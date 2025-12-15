#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using JetBrains.Annotations;
using NeftaCustomAdapter;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
#if UNITY_IOS
        private const string NeftaId = "5763106043068416";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "78b66d4cd80ca1e7",
            "c9acf50602329bfe",
            // rewarded
            "7c6097e4101586b0",
            "08304643cb16df3b"
        };
        
        [DllImport("__Internal")]
        private static extern void CheckTrackingPermission();
#else // UNITY_ANDROID
        private const string NeftaId = "5693275310653440";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "850bcc93f949090c",
            "60bbc7cc56dfa329",
            // rewarded
            "c164298ebdd0c008",
            "3082ee9199cf59f0"
        };
#endif

        private float _stateTime;
        
        private void Awake()
        {
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.SetExtraParameter(NeftaAdapterEvents.ExtParams.TestGroup, "split-unity-max");
                
            NeftaAdapterEvents.OnReady = (InitConfiguration config) =>
            {
                Debug.Log($"[NeftaPluginMAX] Dynamic ad units: {string.Join(", ", config.GetProviderAdUnits())}");
                Debug.Log($"[NeftaPluginMAX] Should bypass Nefta optimization? {config._skipOptimization}");
            };
            NeftaAdapterEvents.Init(NeftaId);
            
#if !UNITY_EDITOR && UNITY_IOS
            CheckIdfaAndInitAds();
#else
            InitAds();
#endif
        }

        private void InitAds()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("MAX SDK Initialized");
            };
            
            MaxSdk.SetVerboseLogging(true);
            MaxSdk.SetExtraParameter("disable_b2b_ad_unit_ids", string.Join(",", _adUnits));
            MaxSdk.InitializeSdk();
        }

        [UsedImplicitly]
        private void CheckIdfaAndInitAds()
        {
            _stateTime = 1f;
        }
        
        private void Update()
        {
            if (_stateTime > 0f)
            {
                _stateTime -= Time.deltaTime;
                if (_stateTime <= 0f)
                {
#if !UNITY_EDITOR && UNITY_IOS
                    CheckTrackingPermission();
#endif
                    InitAds();
                }
            }
        }
    }
}
