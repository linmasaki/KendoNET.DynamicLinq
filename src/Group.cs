using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KendoNET.DynamicLinq
{
    public class Group : Sort
    {
        [DataMember(Name = "aggregates")]
        public IEnumerable<Aggregator> Aggregates { get; set; }
    }
}
