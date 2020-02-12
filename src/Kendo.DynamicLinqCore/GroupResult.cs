using System.Runtime.Serialization;

namespace Kendo.DynamicLinqCore
{
    // The response format of the group schema : https://docs.telerik.com/kendo-ui/api/javascript/data/datasource/configuration/schema#schemagroups
    [DataContract(Name = "groupresult")]
    public class GroupResult
    {
        // Small letter properties are kendo js properties so please excuse the warnings
        [DataMember(Name = "value")]
        public object Value { get; set; }

        public string SelectorField { get; set; }

        [DataMember(Name = "field")]
        public string Field
        {
            get { return string.Format("{0} ({1})", this.SelectorField, this.Count); }
            //get { return SelectorField; }
        }
        public int Count { get; set; }

        [DataMember(Name = "aggregates")]
        public object Aggregates { get; set; }

        [DataMember(Name = "items")]
        public dynamic Items { get; set; }

        [DataMember(Name = "hasSubgroups")]
        public bool HasSubgroups { get; set; } // true if there are subgroups
    }
}
