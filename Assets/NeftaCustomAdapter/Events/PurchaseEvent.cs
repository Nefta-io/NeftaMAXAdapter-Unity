using UnityEngine;

namespace Nefta.Core.Events
{
    public class PurchaseEvent : GameEvent
    {
        private int _currency;
        
        internal override int _eventType => 4;
        internal override int _category => 0;
        internal override int _subCategory => _currency;
        
        /// <summary>
        /// Price field, must be non-negative.
        /// </summary>
        public decimal Price {
            get => (decimal)_value / 1000000;
            set => _value = (long)(value * 1000000);
        }

        /// <summary>
        /// Event for recording purchase with real world money.
        /// </summary>
        public PurchaseEvent(string name, decimal price, string currency)
        {
            _name = name;
            Price = price;
            if (currency.Length == 3)
            {
                _currency = currency[0] | (currency[1] << 8) | (currency[2] << 16);
            }
            else
            {
                Debug.LogWarning("Invalid ISO 4217 currency");
            }
        }
    }
}