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
        private const string NeftaId = "5661184053215232";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "6d318f954e2630a8",
            "37146915dc4c7740",
            "e5dc3548d4a0913f",
            // rewarded
            "918acf84edf9c034",
            "37163b1a07c4aaa0",
            "e0b0d20088d60ec5"
        };
        
        [DllImport("__Internal")]
        private static extern void CheckTrackingPermission();
#else // UNITY_ANDROID
        private const string NeftaId = "5643649824063488";

        private readonly string[] _adUnits = new string[] {
            // interstitials
            "7267e7f4187b95b2",
            "00b665eda2658439",
            "87f1b4837da231e5",
            // rewarded
            "72458470d47ee781",
            "5305c7824f0b5e0a",
            "a4b93fe91b278c75"
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
            NeftaAdapterEvents.SetContentRating(NeftaAdapterEvents.ContentRating.MatureAudience);
            
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
