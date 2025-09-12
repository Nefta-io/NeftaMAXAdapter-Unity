using UnityEngine;
#if UNITY_EDITOR
using Nefta.Editor;
#endif


namespace NeftaCustomAdapter
{
    public class NeftaAdapterListener : AndroidJavaProxy, IAdapterListener
    {
        public NeftaAdapterListener() : base("com.nefta.sdk.AdapterCallback")
        {
        }
        
        public void IOnReady(string adUnits)
        {
            NeftaAdapterEvents.IOnReady(adUnits);
        }

        public void IOnInsights(int id, int adapterResponseType, string adapterResponse)
        {
            NeftaAdapterEvents.IOnInsights(id, adapterResponseType, adapterResponse);
        }
    }
}