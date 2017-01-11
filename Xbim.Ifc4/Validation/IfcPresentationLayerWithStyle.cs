using System;
using log4net;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Xbim.Common.Enumerations;
using Xbim.Common.ExpressValidation;
using Xbim.Ifc4.Interfaces;
using static Xbim.Ifc4.Functions;
// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace Xbim.Ifc4.PresentationOrganizationResource
{
	public partial class IfcPresentationLayerWithStyle : IExpressValidatable
	{
		public enum IfcPresentationLayerWithStyleClause
		{
			ApplicableOnlyToItems,
		}

		/// <summary>
		/// Tests the express where-clause specified in param 'clause'
		/// </summary>
		/// <param name="clause">The express clause to test</param>
		/// <returns>true if the clause is satisfied.</returns>
		public bool ValidateClause(IfcPresentationLayerWithStyleClause clause) {
			var retVal = false;
			try
			{
				switch (clause)
				{
					case IfcPresentationLayerWithStyleClause.ApplicableOnlyToItems:
						retVal = SIZEOF(AssignedItems.Where(temp => (SIZEOF(TYPEOF(temp) * NewArray("IFC4.IFCGEOMETRICREPRESENTATIONITEM", "IFC4.IFCMAPPEDITEM")) == 1))) == SIZEOF(AssignedItems);
						break;
				}
			} catch (Exception ex) {
				var Log = LogManager.GetLogger("Xbim.Ifc4.PresentationOrganizationResource.IfcPresentationLayerWithStyle");
				Log.Error(string.Format("Exception thrown evaluating where-clause 'IfcPresentationLayerWithStyle.{0}' for #{1}.", clause,EntityLabel), ex);
			}
			return retVal;
		}

		public override IEnumerable<ValidationResult> Validate()
		{
			foreach (var value in base.Validate())
			{
				yield return value;
			}
			if (!ValidateClause(IfcPresentationLayerWithStyleClause.ApplicableOnlyToItems))
				yield return new ValidationResult() { Item = this, IssueSource = "IfcPresentationLayerWithStyle.ApplicableOnlyToItems", IssueType = ValidationFlags.EntityWhereClauses };
		}
	}
}