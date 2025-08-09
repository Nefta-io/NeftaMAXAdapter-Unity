#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
        private const string MaxSdkKey = "IAhBswbDpMg9GhQ8NEKffzNrXQP1H4ABNFvUA7ePIz2xmarVFcy_VB8UfGnC9IPMOgpQ3p8G5hBMebJiTHv3P9";

#if UNITY_IOS
        private const string NeftaId = "5763106043068416";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "c9acf50602329bfe",
            "e5dc3548d4a0913f",
            // rewarded
            "08304643cb16df3b",
            "7c6097e4101586b0",
        };
        
        [DllImport("__Internal")]
        private static extern void CheckTrackingPermission();
#else // UNITY_ANDROID
        private const string NeftaId = "5693275310653440";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "60bbc7cc56dfa329",
            "850bcc93f949090c",
            // rewarded
            "3082ee9199cf59f",
            "c164298ebdd0c008",
        };
#endif
        private bool _isBannerShown;

        [SerializeField] private BannerController _banner;
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;

        private float _stateTime;
        private bool _permissionChecked;
        
        private void Awake()
        {
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.Init(NeftaId);

            _banner.Init();
            _interstitial.Init(OnFullScreenAdDisplay);
            _rewarded.Init(OnFullScreenAdDisplay);
            
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

        private void OnFullScreenAdDisplay(bool displayed)
        {
            _banner.SetAutoRefresh(!displayed);
        }
    }
}
