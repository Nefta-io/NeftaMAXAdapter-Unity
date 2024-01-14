using UnityEngine;

namespace NeftaCustomAdapter.Editor
{
    public class NeftaConfiguration : ScriptableObject
    {
        [Tooltip("If you're are in framework mode (use_frameworks!) and link them dynamically")]
        public bool _neftaSDKDependencyInRoot;

        [Tooltip("Set to use debug version of NeftaPlugin")]
        public bool _isLoggingEnabled;
    }
}