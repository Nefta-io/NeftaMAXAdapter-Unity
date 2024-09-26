namespace Nefta.Core.Events
{
    public enum ReceiveMethod
    {
        Undefined = 0,
        LevelEnd = 1,
        Reward = 2,
        Loot = 3,
        Shop = 4,
        IAP = 5,
        Create = 6,
        Other = 7
    }
    
    /// <summary>
    /// Event for recording player receiving resources
    /// </summary>
    public class ReceiveEvent : ResourceEvent
    {
        /// <summary>
        /// The method how or where the player received resources
        /// </summary>
        public ReceiveMethod _method;

        internal override int _eventType => 2;
        
        internal override int _subCategory => (int) _method;

        
        public ReceiveEvent(ResourceCategory category) : base(category)
        {
        }
    }
}