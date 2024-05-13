namespace Nefta.Core.Events
{
    public abstract class GameEvent
    {
        internal abstract string _eventType { get; }
        internal abstract string _category { get; }
        internal abstract string _subCategory { get; }
        
        public string _name;
        public long _value;
        public string _customString;
    }
}