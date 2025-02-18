using NeftaCustomAdapter;

namespace Nefta.Core.Events
{
    public abstract class GameEvent
    {
        internal abstract int _eventType { get; }
        internal abstract int _category { get; }
        internal abstract int _subCategory { get; }
        
        public string _name;
        /// <summary>
        /// Value field, must be non-negative.
        /// </summary>
        public long _value;
        public string _customString;
        
        public void Record()
        {
            string name = null;
            if (_name != null)
            {
                name = NeftaAdapterEvents.JavaScriptStringEncode(_name);
            }
            string customPayload = null;
            if (_customString != null)
            {
                customPayload = NeftaAdapterEvents.JavaScriptStringEncode(_customString);
            }
            NeftaAdapterEvents.Record(_eventType, _category, _subCategory, name, _value, customPayload);
        }
    }
}