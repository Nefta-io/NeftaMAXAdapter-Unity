namespace Nefta.Core.Events
{
    public enum SpendMethod
    {
        Undefined = 0,
        Boost = 1,
        Continuity = 2,
        Create = 3,
        Unlock = 4,
        Upgrade = 5,
        Shop = 6,
        Other = 7
    }
    
    /// <summary>
    /// Event for recording player spending resources
    /// </summary>
    public class SpendEvent : ResourceEvent
    {
        /// <summary>
        /// The method how or where the player spend resources
        /// </summary>
        public SpendMethod _method;
        
        internal override int _eventType => 3;
        
        internal override int _subCategory => (int) _method;
        
        public SpendEvent(ResourceCategory category) : base(category)
        {
        }
    }
}