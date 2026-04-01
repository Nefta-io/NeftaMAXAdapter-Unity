using UnityEngine;
#if UNITY_EDITOR
using Nefta.Editor;
#endif

namespace NeftaCustomAdapter
{
    public class NeftaListener : AndroidJavaProxy, IPluginListener
    {
        public NeftaListener() : base("com.nefta.sdk.AdapterCallback")
        {
        }
        
        public void IOnReady(string initConfig)
        {
            NeftaAdapterEvents.IOnReady(initConfig);
        }

        public void IOnInsights(int id, int adapterResponseType, string adapterResponse)
        {
            NeftaAdapterEvents.IOnInsights(id, adapterResponseType, adapterResponse);
        }

        public void IOnNewSessionCallback()
        {
            NeftaAdapterEvents.IOnNewSessionCallback();
        }
    }
}