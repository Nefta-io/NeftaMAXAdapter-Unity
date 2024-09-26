namespace Nefta.Core.Events
{
    public enum Type
    {
        Achievement = 0,
        GameplayUnit = 1,
        ItemLevel = 2,
        Unlock = 3,
        PlayerLevel = 4,
        Task = 5,
        Other = 6
    }

    public enum Status
    {
        Start = 0,
        Complete = 1,
        Fail = 2
    }
    
    public enum Source
    {
        Undefined = 0,
        CoreContent = 1,
        OptionalContent = 2,
        Boss = 3,
        Social = 4,
        SpecialEvent = 5,
        Other = 6
    }
    
    /// <summary>
    /// Event for recording player progress
    /// </summary>
    public class ProgressionEvent : GameEvent
    {
        /// <summary>
        /// Type of progression
        /// </summary>
        public Type _type;
        
        /// <summary>
        /// The status of progression (start, complete or failed)
        /// </summary>
        public Status _status;

        /// <summary>
        /// Source content of progression
        /// </summary>
        public Source _source;

        internal override int _eventType => 1;

        internal override int _category => (int)_type * 3 + (int)_status;
        
        internal override int _subCategory => (int)_source;
        
        public ProgressionEvent(Type type, Status status)
        {
            _type = type;
            _status = status;
        }
    }
}