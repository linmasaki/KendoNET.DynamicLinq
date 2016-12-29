using System;

namespace Kendo.DynamicLinqCore
{
    public class GroupSelector<TElement>
    {
        public Func<TElement, object> Selector { get; set; }
        public string Field { get; set; }
    }
}
