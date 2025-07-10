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

        public void IOnInsights(int id, string insights)
        {
            NeftaAdapterEvents.IOnInsights(id, insights);
        }
    }
}