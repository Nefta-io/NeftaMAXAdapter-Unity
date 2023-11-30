using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum Source
    {
        Undefined,
        CoreContent,
        OptionalContent,
        Boss,
        Social,
        SpecialEvent,
        Other
    }
    
    public enum Type
    {
        Undefined,
        GameplayUnit,
        Task,
        Achievement,
        PlayerLevel,
        ItemLevel,
        Other
    }

    public enum Status
    {
        Undefined,
        Start,
        Completed,
        Fail
    }
    
    /// <summary>
    /// Event for recording player progress
    /// </summary>
    public class ProgressionEvent : GameEvent
    {
        private static readonly Dictionary<Status, string> ProgressionToString = new Dictionary<Status, string>()
        {
            { Status.Undefined, null },
            { Status.Start, "progression-start" },
            { Status.Completed, "progression-completed" },
            { Status.Fail, "progression-fail" }
        };

        private static readonly Dictionary<Type, string> ProgressionTypeToString = new Dictionary<Type, string>()
        {
            { Type.Undefined, null },
            { Type.GameplayUnit, "gameplay-unit" },
            { Type.Task, "task" },
            { Type.Achievement, "achievement" },
            { Type.PlayerLevel, "player-level" },
            { Type.ItemLevel, "item-level" },
            { Type.Other, "other" },
        };

        private static readonly Dictionary<Source, string> ProgressionSourceToString = new Dictionary<Source, string>()
        {
            { Source.Undefined, null },
            { Source.CoreContent, "core-content" },
            { Source.OptionalContent, "optional-content" },
            { Source.Boss, "boss" },
            { Source.Social, "social" },
            { Source.SpecialEvent, "special-event" },
            { Source.Other, "other" },
        };

        /// <summary>
        /// The name of the progression in which the player progressed
        /// </summary>
        public string _name;

        /// <summary>
        /// The status of progression (start, complete or failed)
        /// </summary>
        public Status _status;

        /// <summary>
        /// Type of progression
        /// </summary>
        public Type _type;

        /// <summary>
        /// Source content of progression
        /// </summary>
        public Source _source;

        /// <summary>
        /// Quantifiable progress number
        /// </summary>
        public int _value;

        /// <summary>
        /// Any custom data you might want to record
        /// </summary>
        public string _customString;

        public override RecordedEvent GetRecordedEvent()
        {
            return new RecordedEvent()
            {
                _type = ProgressionToString[_status],
                _category = ProgressionTypeToString[_type],
                _subCategory = ProgressionSourceToString[_source],
                _itemName = _name,
                _value = _value,
                _customPayload = _customString,
            };
        }
    }
}