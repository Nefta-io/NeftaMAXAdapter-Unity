using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum ResourceCategory
    {
        Other,
        SoftCurrency,
        PremiumCurrency,
        Resource,
        CoreItem,
        CosmeticItem,
        Consumable,
        Experience,
        Chest
    }

    public abstract class ResourceEvent : GameEvent
    {
        private static readonly Dictionary<ResourceCategory, string> CategoryToString = new Dictionary<ResourceCategory, string>()
        {
            { ResourceCategory.Other, "other" },
            { ResourceCategory.SoftCurrency, "soft_currency" },
            { ResourceCategory.PremiumCurrency, "premium_currency" },
            { ResourceCategory.Resource, "resource" },
            { ResourceCategory.CoreItem, "core_item" },
            { ResourceCategory.CosmeticItem, "cosmetic_item" },
            { ResourceCategory.Consumable, "consumable" },
            { ResourceCategory.Experience, "experience" },
            { ResourceCategory.Chest, "chest" }
        };
            
        /// <summary>
        /// The category of the resource
        /// </summary>
        public ResourceCategory _resourceCategory;
        
        public long _quantity { get { return _value; } set { _value = value; } }

        internal override string _category => CategoryToString[_resourceCategory];
        
        protected ResourceEvent(ResourceCategory category)
        {
            _resourceCategory = category;
        }
    }
}