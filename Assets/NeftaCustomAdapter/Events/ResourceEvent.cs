using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum Category
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
        private static readonly Dictionary<Category, string> CategoryToString = new Dictionary<Category, string>()
        {
            { Category.Undefined, null },
            { Category.SoftCurrency, "soft-currency" },
            { Category.PremiumCurrency, "premium-currency" },
            { Category.Resource, "resource" },
            { Category.CoreItem, "core-item" },
            { Category.CosmeticItem, "cosmetic-item" },
            { Category.Consumable, "consumable" },
            { Category.Experience, "experience" },
            { Category.Chest, "chest" },
            { Category.Other, "other" },
        };

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string _name;
            
        /// <summary>
        /// The category of the resource
        /// </summary>
        public Category _category;
        
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
                _category = CategoryToString[_category],
                _itemName = _name,
                _value = _quantity,
                _customPayload = _customString,
            };
        }
    }
}