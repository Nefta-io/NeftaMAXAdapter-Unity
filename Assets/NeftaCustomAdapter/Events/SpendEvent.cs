using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum SpendMethod
    {
        Undefined,
        Boost,
        Continuity,
        Create,
        Unlock,
        Upgrade,
        Shop,
        Other
    }
    
    /// <summary>
    /// Event for recording player spending resources
    /// </summary>
    public class SpendEvent : ResourceEvent
    {
        private static readonly Dictionary<SpendMethod, string> MethodToString = new Dictionary<SpendMethod, string>()
        {
            { SpendMethod.Undefined, null },
            { SpendMethod.Boost, "boost" },
            { SpendMethod.Continuity, "continuity" },
            { SpendMethod.Create, "create" },
            { SpendMethod.Unlock, "unlock" },
            { SpendMethod.Upgrade, "upgrade" },
            { SpendMethod.Shop, "shop" },
            { SpendMethod.Other, "other" }
        };
        
        /// <summary>
        /// The method how or where the player spend resources
        /// </summary>
        public SpendMethod _method;
        
        internal override string _eventType => "spend";
        
        internal override string _subCategory => MethodToString[_method];
    }
}