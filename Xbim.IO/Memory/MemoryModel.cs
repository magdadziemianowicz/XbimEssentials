﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.IO.Esent;
using Xbim.IO.Step21;
using Xbim.IO.Xml;
using PropertyTranformDelegate = Xbim.Common.PropertyTranformDelegate;

namespace Xbim.IO.Memory
{
    public static class MemoryModel
    {
        private static ZipEntry GetZipEntry(Stream fileStream)
        {
            // used because - The ZipInputStream has one major advantage over using ZipFile to read a zip: 
            // it can read from an unseekable input stream - such as a WebClient download
            using (var zipStream = new ZipInputStream(fileStream))
            {
                var entry = zipStream.GetNextEntry();
                while (entry != null)
                {
                    if (entry.IsFile && entry.Name.IfcStorageType() != XbimStorageType.Invalid)
                    {
                        return entry;
                    }
                    entry = zipStream.GetNextEntry(); //get next entry
                }
            }
            return null;
        }

        public static IStepFileHeader GetStepFileHeader(string fileName)
        {
            // need to get the header for each step file storage type
            //if it is a zip file
            var storageType = fileName.IfcStorageType();
            using (var stream = File.OpenRead(fileName))
            {
                
                if(storageType==XbimStorageType.IfcZip|| storageType==XbimStorageType.Step21Zip)
                {

                    var zipEntry = GetZipEntry(stream);
                    if (zipEntry != null)
                    {
                        var zipStorageType = zipEntry.Name.IfcStorageType();
                        if (zipStorageType == XbimStorageType.Ifc || zipStorageType == XbimStorageType.Step21)
                        {
                            using (var zipFile = new ZipFile(fileName))
                            {
                                using (var reader = zipFile.GetInputStream(zipEntry))
                                {
                                    return GetStepFileHeader(reader);
                                }
                            }
                        }
                        else if (zipStorageType == XbimStorageType.IfcXml)
                        {
                            throw new NotImplementedException("XML header reading not implemented");
                        }
                    }
                    return null;
                }
                return GetStepFileHeader(stream);
            }
        }

        private static IStepFileHeader GetStepFileHeader(Stream stream)
        {
            var parser = new XbimP21Parser(stream, null);
            var stepHeader = new StepFileHeader(StepFileHeader.HeaderCreationMode.LeaveEmpty);
            parser.EntityCreate += (string name, long? label, bool header, out int[] ints) =>
            {
                //allow all attributes to be parsed
                ints = null;
                if (header)
                {
                    switch (name)
                    {
                        case "FILE_DESCRIPTION":
                            return stepHeader.FileDescription;
                        case "FILE_NAME":
                            return stepHeader.FileName;
                        case "FILE_SCHEMA":
                            return stepHeader.FileSchema;
                        default:
                            return null;
                    }
                }
                parser.Cancel = true; //done enough
                return null;
            };
            parser.Parse();
            stream.Close();
            return stepHeader;
        }
    }

    public class MemoryModel<TFactory> : IModel, IDisposable where TFactory : IEntityFactory, new()
    {
        public static StepFileHeader GetStepFileHeader(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                var parser = new XbimP21Parser(stream, null);
                var stepHeader = new StepFileHeader(StepFileHeader.HeaderCreationMode.LeaveEmpty);
                parser.EntityCreate += (string name, long? label, bool header, out int[] ints) =>
                {
                    //allow all attributes to be parsed
                    ints = null;
                    if (header)
                    {
                        switch (name)
                        {
                            case "FILE_DESCRIPTION":
                                return stepHeader.FileDescription;
                            case "FILE_NAME":
                                return stepHeader.FileName;
                            case "FILE_SCHEMA":
                                return stepHeader.FileSchema;
                            default:
                                return null;
                        }
                    }
                    parser.Cancel = true; //done enough
                    return null;
                };
                parser.Parse();
                stream.Close();
                return stepHeader;
            }
        }

        private readonly EntityCollection<TFactory> _instances;
        public int UserDefinedId { get; set; }
        public MemoryModel()
        {
            _instances = new EntityCollection<TFactory>(this);
            Header = new StepFileHeader(StepFileHeader.HeaderCreationMode.InitWithXbimDefaults);
            Header.FileSchema.Schemas.AddRange(_instances.Factory.SchemasIds);
            ModelFactors = new XbimModelFactors(Math.PI / 180, 1e-3, 1e-5);
            Metadata = ExpressMetaData.GetMetadata(typeof(TFactory).Module);
            IsTransactional = true;
        }

        /// <summary>
        /// Instance collection of all entities in the model. You can use this collection to create new
        /// entities or to query the model. This is the only way how to create new entities.
        /// </summary>
        public virtual IEntityCollection Instances
        {
            get { return _instances; }
        }

        public virtual bool Activate(IPersistEntity owningEntity, bool write)
        {
            return true;
        }

        /// <summary>
        /// This will delete the entity from model dictionary and also from any references in the model.
        /// Be carefull as this might take a while to check for all occurances of the object. Also make sure 
        /// you don't use this object anymore yourself because it won't get disposed until than. This operation
        /// doesn't guarantee that model is compliant with any kind of schema but it leaves it consistent. So if you
        /// serialize the model there won't be any references to the object which wouldn't be there.
        /// </summary>
        /// <param name="entity">Entity to be deleted</param>
        public virtual void Delete(IPersistEntity entity)
        {
            //remove from entity collection
            var removed = _instances.RemoveReversible(entity);
            if (!removed) return;

            var entityType = entity.GetType();
            List<DeleteCandidateType> candidateTypes;
            if (!_deteteCandidatesCache.TryGetValue(entityType, out candidateTypes))
            {
                candidateTypes = new List<DeleteCandidateType>();
                _deteteCandidatesCache.Add(entityType, candidateTypes);

                //find all potential references and delete from there
                var types = Metadata.Types().Where(t => typeof(IInstantiableEntity).IsAssignableFrom(t.Type));
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var type in types)
                {
                    var toNullify = type.Properties.Values.Where(p =>
                        p.EntityAttribute != null && p.EntityAttribute.Order > 0 &&
                        p.PropertyInfo.PropertyType.IsAssignableFrom(entityType)).ToList();
                    var toRemove =
                        type.Properties.Values.Where(p =>
                            p.EntityAttribute != null && p.EntityAttribute.Order > 0 &&
                            p.PropertyInfo.PropertyType.IsGenericType &&
                            p.PropertyInfo.PropertyType.GenericTypeArgumentIsAssignableFrom(entityType)).ToList();
                    if (!toNullify.Any() && !toRemove.Any()) continue;

                    candidateTypes.Add(new DeleteCandidateType { Type = type, ToNullify = toNullify, ToRemove = toRemove });
                }
            }

            foreach (var candidateType in candidateTypes)
                DeleteReferences(entity, candidateType);
        }

        /// <summary>
        /// Deletes references to specified entity from all entities in the model where entity is
        /// a references as an object or as a member of a collection.
        /// </summary>
        /// <param name="entity">Entity to be removed from references</param>
        /// <param name="candidateType">Candidate type containing reference to the type of entity</param>
        protected virtual void DeleteReferences(IPersistEntity entity, DeleteCandidateType candidateType)
        {
            //get all instances of this type and nullify and remove the entity
            var entitiesToCheck = _instances.OfType(candidateType.Type.Type);
            foreach (var toCheck in entitiesToCheck)
            {
                //check properties
                foreach (var pInfo in candidateType.ToNullify.Select(p => p.PropertyInfo))
                {
                    var pVal = pInfo.GetValue(toCheck);
                    if (pVal == null) continue;
                    //it is enough to compare references
                    if (!ReferenceEquals(pVal, entity)) continue;
                    pInfo.SetValue(toCheck, null);
                }

                foreach (var pInfo in candidateType.ToRemove.Select(p => p.PropertyInfo))
                {
                    var pVal = pInfo.GetValue(toCheck);
                    if (pVal == null) continue;

                    //it might be uninitialized optional item set
                    var optSet = pVal as IOptionalItemSet;
                    if (optSet != null && !optSet.Initialized) continue;

                    //or it is non-optional item set implementind IList
                    var itemSet = pVal as IList;
                    if (itemSet != null)
                    {
                        if (itemSet.Contains(entity))
                            itemSet.Remove(entity);
                        continue;
                    }

                    //fall back operating on common list functions using reflection (this is slow)
                    var contMethod = pInfo.PropertyType.GetMethod("Contains");
                    if (contMethod == null) continue;
                    var contains = (bool)contMethod.Invoke(pVal, new object[] { entity });
                    if (!contains) continue;
                    var removeMethod = pInfo.PropertyType.GetMethod("Remove");
                    if (removeMethod == null) continue;
                    removeMethod.Invoke(pVal, new object[] { entity });
                }
            }
        }

        /// <summary>
        /// Helper structure to hold information for reference removal. If multiple objects of the same type are to
        /// be removed this will cache the information about where to have a look for the references.
        /// </summary>
        protected struct DeleteCandidateType
        {
            public ExpressType Type;
            public List<ExpressMetaProperty> ToNullify;
            public List<ExpressMetaProperty> ToRemove;
        }

        private readonly Dictionary<Type, List<DeleteCandidateType>> _deteteCandidatesCache = new Dictionary<Type, List<DeleteCandidateType>>();

        public virtual ITransaction BeginTransaction(string name)
        {
            if (CurrentTransaction != null)
                throw new Exception("Transaction is opened already.");

            var txn = new Transaction<TFactory>(this);
            CurrentTransaction = txn;
            return txn;
        }

        public IStepFileHeader Header { get; private set; }

        public virtual bool IsTransactional { get; private set; }

        /// <summary>
        /// Weak reference allows garbage collector to collect transaction once it goes out of the scope
        /// even if it is still referenced from model. This is important for the cases where the transaction
        /// is both not commited and not rolled back either.
        /// </summary>
        private WeakReference _transactionReference;

        private InMemoryGeometryStore _geometryStore;

        public virtual ITransaction CurrentTransaction
        {
            get
            {
                if (_transactionReference == null || !_transactionReference.IsAlive)
                    return null;
                return _transactionReference.Target as ITransaction;
            }
            internal set
            {
                if (value == null)
                {
                    _transactionReference = null;
                    return;
                }
                if (_transactionReference == null)
                    _transactionReference = new WeakReference(value);
                else
                    _transactionReference.Target = value;
            }
        }

        public virtual IModelFactors ModelFactors { get; private set; }
        public ExpressMetaData Metadata { get; private set; }

        public virtual void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body) where TSource : IPersistEntity
        {
            foreach (var entity in source)
            {
                body(entity);
            }
        }

        /// <summary>
        /// This event is fired every time new entity is created.
        /// </summary>
        public event NewEntityHandler EntityNew;

        /// <summary>
        /// This event is fired every time any entity is modified. If your model is not
        /// transactional it might not be called at all as the central point for all
        /// modifications is a transaction.
        /// </summary>
        public event ModifiedEntityHandler EntityModified;

        /// <summary>
        /// This event is fired every time when entity gets deleted from model.
        /// </summary>
        public event DeletedEntityHandler EntityDeleted;

        internal void HandleEntityChange(ChangeType changeType, IPersistEntity entity)
        {
            switch (changeType)
            {
                case ChangeType.New:
                    if (EntityNew != null)
                        EntityNew(entity);
                    break;
                case ChangeType.Deleted:
                    if (EntityDeleted != null)
                        EntityDeleted(entity);
                    break;
                case ChangeType.Modified:
                    if (EntityModified != null)
                        EntityModified(entity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("changeType", changeType, null);
            }
        }

        public virtual void OpenXml(string path, ReportProgressDelegate progDelegate = null)
        {
            using (var file = File.OpenRead(path))
            {
                OpenXml(file, progDelegate);
            }
        }

        public virtual void OpenXml(Stream stream, ReportProgressDelegate progDelegate = null)
        {
            using (var reader = XmlReader.Create(stream))
            {
                var read = new Dictionary<int, IPersistEntity>();
                var xmlReader = new XbimXmlReader4(
                    (label, type) =>
                    {
                        IPersistEntity exist;
                        if (read.TryGetValue(label, out exist))
                            return exist;

                        var ent = _instances.Factory.New(this, type, label, true);
                        _instances.InternalAdd(ent);
                        read.Add(label, ent);
                        return ent;
                    },
                    entity => { },
                    Metadata);
                var header = xmlReader.Read(reader);
                Header = header;
            }
        }

        /// <summary>
        /// Opens the model from STEP21 file. 
        /// </summary>
        /// <param name="stream">Path to the file</param>
        /// <param name="progDelegate"></param>
        /// <returns>Number of errors in parsing. Always check this to be null or the model might be incomplete.</returns>
        public virtual int Open(Stream stream, ReportProgressDelegate progDelegate = null)
        {
            var parser = new XbimP21Parser(stream, Metadata);
            if (progDelegate != null) parser.ProgressStatus += progDelegate;
            var first = true;
            Header = new StepFileHeader(StepFileHeader.HeaderCreationMode.LeaveEmpty);
            parser.EntityCreate += (string name, long? label, bool header, out int[] ints) =>
            {
                //allow all attributes to be parsed
                ints = null;

                if (header)
                {
                    switch (name)
                    {
                        case "FILE_DESCRIPTION":
                            return Header.FileDescription;
                        case "FILE_NAME":
                            return Header.FileName;
                        case "FILE_SCHEMA":
                            return Header.FileSchema;
                        default:
                            return null;
                    }
                }

                if (label == null)
                    return _instances.Factory.New(name);
                //if this is a first non-header entity header is read completely by now. 
                //So we should check if the schema declared in the file is the one declared in EntityFactory
                if (first)
                {
                    first = false;
                    if (!Header.FileSchema.Schemas.All(s => _instances.Factory.SchemasIds.Contains(s)))
                        throw new Exception("Mismatch between schema defined in the file and schemas available in the data model.");
                }

                var typeId = Metadata.ExpressTypeId(name);
                var ent = _instances.Factory.New(this, typeId, (int)label, true);
                _instances.InternalAdd(ent);

                //make sure that new added entities will have higher labels to avoid any clashes
                if (label >= _instances.NextLabel)
                    _instances.NextLabel = (int)label + 1;
                return ent;
            };
            parser.Parse();
            if (progDelegate != null) parser.ProgressStatus -= progDelegate;
            return parser.ErrorCount;
        }

        /// <summary>
        /// Opens the model from STEP21 file. 
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <param name="progDelegate"></param>
        /// <returns>Number of errors in parsing. Always check this to be null or the model might be incomplete.</returns>
        public virtual int Open(string file, ReportProgressDelegate progDelegate = null)
        {
            using (var stream = File.OpenRead(file))
            {
                var result = Open(stream, progDelegate);
                stream.Close();
                return result;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="storageType"></param>
        /// <param name="progress"></param>
        public virtual void SaveAs(string path, XbimStorageType? storageType = null, ReportProgressDelegate progress = null)
        {
            using (var file = File.Create(path))
            {
                SaveAs(file, storageType, progress);
                file.Close();
            }
        }

        /// <summary>
        /// Saves the model as PART21 file
        /// </summary>
        /// <param name="stream">Output stream. Steam will be closed at the end.</param>
        /// <param name="storageType"></param>
        /// <param name="progress"></param>
        public virtual void SaveAs(Stream stream, XbimStorageType? storageType = null, ReportProgressDelegate progress = null)
        {
            using (var writer = new StreamWriter(stream))
            {
                SaveAs(writer, storageType, progress);
                writer.Close();
            }
        }

        /// <summary>
        /// Saves the model as PART21 file
        /// </summary>
        /// <param name="writer">Text writer to be used to write the file</param>
        /// <param name="storageType"></param>
        /// <param name="progress"></param>
        public virtual void SaveAs(TextWriter writer, XbimStorageType? storageType = null, ReportProgressDelegate progress = null)
        {
            var part21Writer = new Part21FileWriter();
            part21Writer.Write(this, writer, Metadata, new Dictionary<int, int>());
        }

        public void Dispose()
        {
            _instances.Dispose();
            _deteteCandidatesCache.Clear();
            _transactionReference = null;
        }

        /// <summary>
        /// Inserts deep copy of an object into this model. The entity must originate from the same schema (the same EntityFactory). 
        /// This operation happens within a transaction which you have to handle yourself unless you set the parameter "noTransaction" to true.
        /// Insert will happen outside of transactional behaviour in that case. Resulting model is not guaranteed to be valid according to any
        /// model view definition. However, it is granted to be consistent. You can optionaly bring in all inverse relationships. Be carefull as it
        /// might easily bring in almost full model.
        /// 
        /// </summary>
        /// <typeparam name="T">Type of the copied entity</typeparam>
        /// <param name="toCopy">Entity to be copied</param>
        /// <param name="mappings">Mappings of previous inserts</param>
        /// <param name="includeInverses">Option if to bring in all inverse entities (enumerations in original entity)</param>
        /// <param name="keepLabels">Option if to keep entity labels the same</param>
        /// <param name="propTransform">Optional delegate which you can use to filter the content which will get coppied over.</param>
        /// <param name="noTransaction"></param>
        /// <returns>Copy from this model</returns>
        public T InsertCopy<T>(T toCopy, Dictionary<int, IPersistEntity> mappings, bool includeInverses, bool keepLabels = true, PropertyTranformDelegate propTransform = null, bool noTransaction = false) where T : IPersistEntity
        {
            if (noTransaction)
                IsTransactional = false;
            try
            {
                var toCopyLabel = toCopy.EntityLabel;
                IPersistEntity copy;
                //try to get the value if it was created before
                if (mappings.TryGetValue(toCopyLabel, out copy))
                {
                    return (T)copy;
                }

                var expressType = Metadata.ExpressType(toCopy);
                copy = keepLabels ?
                    _instances.New(toCopy.GetType(), toCopyLabel) :
                    _instances.New(toCopy.GetType());
                //key is the label in original model
                mappings.Add(toCopyLabel, copy);

                var props = expressType.Properties.Values.Where(p => !p.EntityAttribute.IsDerived);
                if (includeInverses)
                    props = props.Union(expressType.Inverses);

                foreach (var prop in props)
                {
                    var value = propTransform != null ?
                        propTransform(prop, toCopy) :
                        prop.PropertyInfo.GetValue(toCopy, null);
                    if (value == null) continue;

                    var isInverse = (prop.EntityAttribute.Order == -1); //don't try and set the values for inverses
                    var theType = value.GetType();
                    //if it is an express type or a value type, set the value
                    if (theType.IsValueType || typeof(ExpressType).IsAssignableFrom(theType))
                    {
                        prop.PropertyInfo.SetValue(copy, value, null);
                    }
                    else if (!isInverse && typeof(IPersistEntity).IsAssignableFrom(theType))
                    {
                        prop.PropertyInfo.SetValue(copy, InsertCopy((IPersistEntity)value, mappings, includeInverses, keepLabels, propTransform, noTransaction), null);
                    }
                    else if (!isInverse && typeof(IList).IsAssignableFrom(theType))
                    {
                        var itemType = theType.GetItemTypeFromGenericType();

                        var copyColl = prop.PropertyInfo.GetValue(copy, null) as IList;
                        if (copyColl == null)
                            throw new Exception(string.Format("Unexpected collection type ({0}) found", itemType.Name));

                        foreach (var item in (IList)value)
                        {
                            var actualItemType = item.GetType();
                            if (actualItemType.IsValueType || typeof(ExpressType).IsAssignableFrom(actualItemType))
                                copyColl.Add(item);
                            else if (typeof(IPersistEntity).IsAssignableFrom(actualItemType))
                            {
                                var cpy = InsertCopy((IPersistEntity)item, mappings, includeInverses, keepLabels, propTransform, noTransaction);
                                copyColl.Add(cpy);
                            }
                            else
                                throw new Exception(string.Format("Unexpected collection item type ({0}) found", itemType.Name));
                        }
                    }
                    else if (isInverse && value is IEnumerable<IPersistEntity>) //just an enumeration of IPersistEntity
                    {
                        foreach (var ent in (IEnumerable<IPersistEntity>)value)
                        {
                            InsertCopy(ent, mappings, includeInverses, keepLabels, propTransform, noTransaction);
                        }
                    }
                    else if (isInverse && value is IPersistEntity) //it is an inverse and has a single value
                    {
                        InsertCopy((IPersistEntity)value, mappings, includeInverses, keepLabels, propTransform, noTransaction);
                    }
                    else
                        throw new Exception(string.Format("Unexpected item type ({0})  found", theType.Name));
                }
                return (T)copy;
            }
            finally
            {
                //make sure model is transactional at the end again
                IsTransactional = true;
            }
        }


        public IGeometryStore GeometryStore
        {
            get { return _geometryStore ?? (_geometryStore = new InMemoryGeometryStore()); }
        }
    }

    /// <summary>
    /// This delegate can be used to implement customized logic in type mapping.
    /// </summary>
    /// <param name="entity">Original entity</param>
    /// <returns>Express type which maps to the type of the original entity</returns>
    public delegate ExpressType TypeResolverDelegate(IPersistEntity entity);

}
