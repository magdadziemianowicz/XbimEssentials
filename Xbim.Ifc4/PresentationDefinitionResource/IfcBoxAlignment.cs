// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool Xbim.CodeGeneration 
//  
//     Changes to this file may cause incorrect behaviour and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using Xbim.Common;
using Xbim.Common.Exceptions;
using Xbim.Ifc4.MeasureResource;

namespace Xbim.Ifc4.PresentationDefinitionResource
{
	[ExpressType("IFCBOXALIGNMENT", 7)]
    // ReSharper disable once PartialTypeWithSinglePart
	public partial struct IfcBoxAlignment : IExpressValueType, System.IEquatable<string>
	{ 
		private string _value;
        
		public object Value
        {
            get { return _value; }
        }

		public override string ToString()
        {
			return _value ?? "";
        }
        public IfcBoxAlignment(string val)
        {
            _value = val;
        }


        public static implicit operator IfcBoxAlignment(string value)
        {
            return new IfcBoxAlignment(value);
        }

        public static implicit operator string(IfcBoxAlignment obj)
        {
            return obj._value;

        }


        public override bool Equals(object obj)
        {
			if (obj == null && Value == null)
                return true;

            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            return ((IfcBoxAlignment) obj)._value == _value;
        }

		public bool Equals(string other)
	    {
	        return this == other;
	    }

        public static bool operator ==(IfcBoxAlignment obj1, IfcBoxAlignment obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(IfcBoxAlignment obj1, IfcBoxAlignment obj2)
        {
            return !Equals(obj1, obj2);
        }

        public override int GetHashCode()
        {
            return Value != null ? _value.GetHashCode() : base.GetHashCode();
        }

		#region IPersist implementation
		void IPersist.Parse(int propIndex, IPropertyValue value, int[] nestedIndex)
		{
			if (propIndex != 0)
				throw new XbimParserException(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1, GetType().Name.ToUpper()));
            _value = value.StringVal;
            
		}

		string IPersist.WhereRule()
		{
            throw new System.NotImplementedException();
		}
		#endregion

		#region IExpressValueType implementation
        System.Type IExpressValueType.UnderlyingSystemType { 
			get 
			{
				return typeof(string);
			}
		}
		#endregion


	}
}
