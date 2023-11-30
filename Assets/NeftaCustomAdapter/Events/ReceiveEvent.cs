using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum ReceiveMethod
    {
        Undefined,
        LevelEnd,
        Reward,
        Loot,
        Shop,
        IAP,
        Create,
        Other
    }
    
    /// <summary>
    /// Event for recording player receiving resources
    /// </summary>
    public class ReceiveEvent : ResourceEvent
    {
        private static readonly Dictionary<ReceiveMethod, string> MethodToString = new Dictionary<ReceiveMethod, string>()
        {
            { ReceiveMethod.Undefined, null },
            { ReceiveMethod.LevelEnd, "level-end" },
            { ReceiveMethod.Reward, "reward" },
            { ReceiveMethod.Loot, "loot" },
            { ReceiveMethod.Shop, "shop" },
            { ReceiveMethod.IAP, "iap" },
            { ReceiveMethod.Create, "create" },
            { ReceiveMethod.Other, "other" }
        };
        
        /// <summary>
        /// The method how or where the player received resources
        /// </summary>
        public ReceiveMethod _method;
        
        public override RecordedEvent GetRecordedEvent()
        {
            var receiveEvent = base.GetRecordedEvent();
            receiveEvent._type = "receive";
            receiveEvent._subCategory = MethodToString[_method];
            return receiveEvent;
        }
    }
}