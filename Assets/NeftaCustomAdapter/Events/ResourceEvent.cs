using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum ResourceCategory
    {
        Undefined,
        SoftCurrency,
        PremiumCurrency,
        Resource,
        CoreItem,
        CosmeticItem,
        Consumable,
        Experience,
        Chest,
        Other
    }

    public class ResourceEvent : GameEvent
    {
        private static readonly Dictionary<ResourceCategory, string> CategoryToString = new Dictionary<ResourceCategory, string>()
        {
            { ResourceCategory.Undefined, null },
            { ResourceCategory.SoftCurrency, "soft_currency" },
            { ResourceCategory.PremiumCurrency, "premium_currency" },
            { ResourceCategory.Resource, "resource" },
            { ResourceCategory.CoreItem, "core_item" },
            { ResourceCategory.CosmeticItem, "cosmetic_item" },
            { ResourceCategory.Consumable, "consumable" },
            { ResourceCategory.Experience, "experience" },
            { ResourceCategory.Chest, "chest" },
            { ResourceCategory.Other, "other" }
        };

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string _name;
            
        /// <summary>
        /// The category of the resource
        /// </summary>
        public ResourceCategory _resourceCategory;
        
        /// <summary>
        /// Quantity that player received
        /// </summary>
        public int _quantity;
            
        /// <summary>
        /// Any other custom data you want to track
        /// </summary>
        public string _customString;

        protected ResourceEvent()
        {
                
        }

        public override RecordedEvent GetRecordedEvent()
        {
            return new RecordedEvent()
            {
                _category = CategoryToString[_resourceCategory],
                _itemName = _name,
                _value = _quantity,
                _customPayload = _customString,
            };
        }
    }
}