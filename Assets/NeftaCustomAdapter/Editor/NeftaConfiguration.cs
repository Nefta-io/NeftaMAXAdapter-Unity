using UnityEngine;

namespace NeftaCustomAdapter.Editor
{
    public class NeftaConfiguration : ScriptableObject
    {
        [Tooltip("Set to use debug version of NeftaPlugin")]
        public bool _isLoggingEnabled;
    }
}