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
using Xbim.Ifc4.PresentationAppearanceResource;
using Xbim.Ifc4.DateTimeResource;

namespace Xbim.Ifc4.MeasureResource
{
	[ExpressType("IFCRATIOMEASURE", 87)]
    // ReSharper disable once PartialTypeWithSinglePart
	public partial struct IfcRatioMeasure : IfcMeasureValue, IfcSizeSelect, IfcTimeOrRatioSelect, IExpressValueType, System.IEquatable<double>
	{ 
		private double _value;
        
		public object Value
        {
            get { return _value; }
        }

		public override string ToString()
        {
            return _value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        public IfcRatioMeasure(double val)
        {
            _value = val;
        }

		public IfcRatioMeasure(string val)
        {
			_value = System.Convert.ToDouble(val, System.Globalization.CultureInfo.CreateSpecificCulture("en-US"));
        }

        public static implicit operator IfcRatioMeasure(double value)
        {
            return new IfcRatioMeasure(value);
        }

        public static implicit operator double(IfcRatioMeasure obj)
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

            return ((IfcRatioMeasure) obj)._value == _value;
        }

		public bool Equals(double other)
	    {
	        return this == other;
	    }

        public static bool operator ==(IfcRatioMeasure obj1, IfcRatioMeasure obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(IfcRatioMeasure obj1, IfcRatioMeasure obj2)
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
            _value = value.RealVal;
            
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
				return typeof(double);
			}
		}
		#endregion


	}
}
