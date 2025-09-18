#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
#if UNITY_IOS
        private const string NeftaId = "5763106043068416";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "78b66d4cd80ca1e7",
            // rewarded
            "7c6097e4101586b0"
        };
        
        [DllImport("__Internal")]
        private static extern void CheckTrackingPermission();
#else // UNITY_ANDROID
        private const string NeftaId = "5693275310653440";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "850bcc93f949090c",
            // rewarded
            "c164298ebdd0c008"
        };
#endif
        
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;

        private float _stateTime;
        private bool _permissionChecked;
        
        private void Awake()
        {
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.SetExtraParameter(NeftaAdapterEvents.ExtParams.TestGroup, "split-unity-max");
            NeftaAdapterEvents.SetExtraParameter("param1", "arg2");
            NeftaAdapterEvents.Init(NeftaId);
            
            _interstitial.Init();
            _rewarded.Init();
            
#if UNITY_EDITOR || !UNITY_IOS
            _stateTime = 1f; // skip IDFA check
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
        
        private void Update()
        {
            if (!_permissionChecked)
            {
                _stateTime += Time.deltaTime;
                if (_stateTime > 1f)
                {
                    _permissionChecked = true;
#if !UNITY_EDITOR && UNITY_IOS
                    CheckTrackingPermission();
#endif
                    InitAds();
                }
            }
        }
    }
}
