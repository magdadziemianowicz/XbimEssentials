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
using Xbim.Ifc4.StructuralLoadResource;

namespace Xbim.Ifc4.MeasureResource
{
	[ExpressType("IFCBOOLEAN", 6)]
    // ReSharper disable once PartialTypeWithSinglePart
	public partial struct IfcBoolean : IfcModulusOfRotationalSubgradeReactionSelect, IfcModulusOfSubgradeReactionSelect, IfcModulusOfTranslationalSubgradeReactionSelect, IfcRotationalStiffnessSelect, IfcSimpleValue, IfcTranslationalStiffnessSelect, IfcWarpingStiffnessSelect, IExpressValueType, System.IEquatable<bool>
	{ 
		private bool _value;
        
		public object Value
        {
            get { return _value; }
        }

		public override string ToString()
        {
			return _value ? "true" : "false";
        }
        public IfcBoolean(bool val)
        {
            _value = val;
        }

		public IfcBoolean(string val)
        {
			if (string.Compare(val, "true", System.StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(val, ".T.", System.StringComparison.OrdinalIgnoreCase) == 0)
                _value = true;
            else
                _value = false;
        }

        public static implicit operator IfcBoolean(bool value)
        {
            return new IfcBoolean(value);
        }

        public static implicit operator bool(IfcBoolean obj)
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

            return ((IfcBoolean) obj)._value == _value;
        }

		public bool Equals(bool other)
	    {
	        return this == other;
	    }

        public static bool operator ==(IfcBoolean obj1, IfcBoolean obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(IfcBoolean obj1, IfcBoolean obj2)
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
            _value = value.BooleanVal;
            
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
				return typeof(bool);
			}
		}
		#endregion


	}
}
