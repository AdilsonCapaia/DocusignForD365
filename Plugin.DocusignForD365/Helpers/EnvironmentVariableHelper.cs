// Author: Adilson Capaia
// Blog: The Rational Developer
// Year: 2026

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Plugin.DocusignForD365.Helpers
{
    /// <summary>
    /// Simple helper to read Dataverse Environment Variables.
    /// </summary>
    public static class EnvironmentVariableHelper
    {
        public static string GetValue(IOrganizationService service, string schemaName)
        {
            // First try to get the current value from environmentvariablevalue
            var valueQuery = new QueryExpression("environmentvariablevalue")
            {
                ColumnSet = new ColumnSet("value"),
                Criteria = new FilterExpression()
            };

            var definitionLink = valueQuery.AddLink(
                "environmentvariabledefinition",
                "environmentvariabledefinitionid",
                "environmentvariabledefinitionid");

            definitionLink.LinkCriteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);

            var valueResults = service.RetrieveMultiple(valueQuery);

            if (valueResults.Entities.Count > 0)
            {
                var value = valueResults.Entities[0].GetAttributeValue<string>("value");
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            // Fall back to the default value on the definition
            var defQuery = new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("defaultvalue"),
                Criteria = new FilterExpression()
            };
            defQuery.Criteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);

            var defResults = service.RetrieveMultiple(defQuery);

            if (defResults.Entities.Count > 0)
            {
                return defResults.Entities[0].GetAttributeValue<string>("defaultvalue");
            }

            return null;
        }
    }
}
