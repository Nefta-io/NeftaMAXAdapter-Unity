namespace Nefta.Core.Events
{
    public abstract class GameEvent
    {
        internal abstract int _eventType { get; }
        internal abstract int _category { get; }
        internal abstract int _subCategory { get; }
        
        public string _name;
        public long _value;
        public string _customString;
    }
}