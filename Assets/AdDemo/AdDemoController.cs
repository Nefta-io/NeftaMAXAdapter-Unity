using NeftaCustomAdapter;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
#if UNITY_IOS
        private const string _neftaAppId = "5763106043068416";
        
        private readonly string[] _adUnits = new string[] {
            // interstitials
            "78b66d4cd80ca1e7",
            "c9acf50602329bfe",
            // rewarded
            "7c6097e4101586b0",
            "08304643cb16df3b"
        };
#else // UNITY_ANDROID
        private const string _neftaAppId = "5693275310653440";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "850bcc93f949090c",
            "60bbc7cc56dfa329",
            // rewarded
            "c164298ebdd0c008",
            "3082ee9199cf59f0"
        };
#endif
        
        private void Awake()
        {
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.InitWithAppId(_neftaAppId, (InitConfiguration config) =>
            {
                Debug.Log($"[NeftaPluginMAX] Should skip Nefta optimization: {config._skipOptimization} for: {config._nuid}");
                
                MaxSdk.SetVerboseLogging(true);
                MaxSdk.SetTestDeviceAdvertisingIdentifiers(new string[]
                {
                    "6AE31431-72EA-44BD-9732-8159D827E21C",
                    "B656BE16-9A12-4A0E-B160-DBEDFEC7F4C6",
                    "97ec28e2-e65a-4fac-b11e-3975391f7cb7",
                    "dca773a6-3445-4776-b361-4d950a0e212f"
                });
                if (!config._skipOptimization)
                {
                    MaxSdk.SetExtraParameter("disable_b2b_ad_unit_ids", string.Join(",", _adUnits));   
                }
                    
                MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
                {
                    Debug.Log("MAX SDK Initialized");
                };
                MaxSdk.InitializeSdk();
            });
        }
    }
}
