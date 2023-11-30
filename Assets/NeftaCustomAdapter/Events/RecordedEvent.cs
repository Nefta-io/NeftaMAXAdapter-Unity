using System;
using System.Runtime.Serialization;

namespace Nefta.Core.Events
{
    [Serializable]
    public class RecordedEvent
    {
        [DataMember(Name = "event_type")] public string _type;
        [DataMember(Name = "event_category")] public string _category;
        [DataMember(Name = "event_sub_category")] public string _subCategory;
        [DataMember(Name = "item_name")] public string _itemName;
        [DataMember(Name = "value")] public long _value;
        [DataMember(Name = "custom_publisher_payload")] public string _customPayload;
    }
}