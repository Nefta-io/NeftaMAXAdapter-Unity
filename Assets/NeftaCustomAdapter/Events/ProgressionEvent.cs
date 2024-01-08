using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum Type
    {
        Achievement,
        GameplayUnit,
        ItemLevel,
        Unlock,
        PlayerLevel,
        Task,
        Other
    }

    public enum Status
    {
        Start,
        Complete,
        Fail
    }
    
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
    
    /// <summary>
    /// Event for recording player progress
    /// </summary>
    public class ProgressionEvent : GameEvent
    {
        private static readonly Dictionary<Type, Dictionary<Status, string>> ProgressionToString = new Dictionary<Type, Dictionary<Status, string>>()
        {
            { Type.Achievement,
                new Dictionary<Status, string>()
                {
                    { Status.Start, "achievement_start" },
                    { Status.Complete, "achievement_complete" },
                    { Status.Fail, "achievement_fail" },
                }
            },
            { Type.GameplayUnit,
                new Dictionary<Status, string>()
                {
                    { Status.Start, "gameplay_unit_start" },
                    { Status.Complete, "gameplay_unit_complete" },
                    { Status.Fail, "gameplay_unit_fail" },
                }
            },
            { Type.ItemLevel,
                new Dictionary<Status, string>()
                {
                    { Status.Start, "item_level_start" },
                    { Status.Complete, "item_level_complete" },
                    { Status.Fail, "item_level_fail" },
                }
            },
            { Type.Unlock,
                new Dictionary<Status, string>()
                {
                    { Status.Start, "unlock_start" },
                    { Status.Complete, "unlock_complete" },
                    { Status.Fail, "unlock_fail" },
                }
            },
            { Type.PlayerLevel,
                new Dictionary<Status, string>()
                {
                    { Status.Start, "player_level_start" },
                    { Status.Complete, "player_level_complete" },
                    { Status.Fail, "player_level_fail" },
                }
            },
            { Type.Task,
                new Dictionary<Status, string>()
                {
                    { Status.Start, "task_start" },
                    { Status.Complete, "task_complete" },
                    { Status.Fail, "task_fail" },
                }
            },
            { Type.Other,
                new Dictionary<Status, string>()
                {
                    { Status.Start, "other_start" },
                    { Status.Complete, "other_complete" },
                    { Status.Fail, "other_fail" },
                }
            }
        };

        private static readonly Dictionary<Source, string> ProgressionSourceToString = new Dictionary<Source, string>()
        {
            { Source.Undefined, null },
            { Source.CoreContent, "core-content" },
            { Source.OptionalContent, "optional_content" },
            { Source.Boss, "boss" },
            { Source.Social, "social" },
            { Source.SpecialEvent, "special_event" },
            { Source.Other, "other" },
        };
        
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
        
        /// <summary>
        /// The name of the progression in which the player progressed
        /// </summary>
        public string _name;

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
                _type = "progression",
                _category = ProgressionToString[_type][_status],
                _subCategory = ProgressionSourceToString[_source],
                _itemName = _name,
                _value = _value,
                _customPayload = _customString,
            };
        }
    }
}