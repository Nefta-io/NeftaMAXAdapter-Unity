#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using System.Collections.Generic;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
        private const string MaxSdkKey = "IAhBswbDpMg9GhQ8NEKffzNrXQP1H4ABNFvUA7ePIz2xmarVFcy_VB8UfGnC9IPMOgpQ3p8G5hBMebJiTHv3P9";

#if UNITY_IOS
        private const string NeftaId = "5661184053215232";
        
        [DllImport("__Internal")]
        private static extern void CheckTrackingPermission();
#else // UNITY_ANDROID
        private const string NeftaId = "5643649824063488";
#endif
        
        private List<AdConfig> _bannerAdUnits = new List<AdConfig>()
        {
            new AdConfig("d066ee44f5d29f8b", "6345b3fa80c73572", 100),
        };
        
        private List<AdConfig> _interstitialAdUnits = new List<AdConfig>
        {
            new AdConfig("c9acf50602329bfe", "60bbc7cc56dfa329", 100),
        };
        
        private List<AdConfig> _rewardedAdUnits = new List<AdConfig>
        {
            new AdConfig("08304643cb16df3b", "3082ee9199cf59f0", 100),
        };
        
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

            NeftaAdapterEvents.BehaviourInsightCallback = OnBehaviourInsight;
            GetBehaviourInsight();
            
            _banner.Init(_bannerAdUnits, GetBehaviourInsight);
            _interstitial.Init(_interstitialAdUnits, GetBehaviourInsight);
            _rewarded.Init(_rewardedAdUnits, GetBehaviourInsight);
            
#if UNITY_EDITOR || !UNITY_IOS
            _stateTime = 1f; // skip IDFA check
#endif
        }

        private void GetBehaviourInsight()
        {
            NeftaAdapterEvents.GetBehaviourInsight(new string[]
            {
                "calculated_user_floor_price_banner",
                "calculated_user_floor_price_interstitial",
                "calculated_user_floor_price_rewarded"
            });
        }
        
        private void OnBehaviourInsight(Dictionary<string, Insight> behaviourInsight)
        {
            foreach (var insight in behaviourInsight)
            {
                var insightValue = insight.Value;
                Debug.Log($"BehaviourInsight {insight.Key} status:{insightValue._status} i:{insightValue._int} f:{insightValue._float} s:{insightValue._string}");
            }
            
            _banner.OnBehaviourInsight(behaviourInsight);
            _interstitial.OnBehaviourInsight(behaviourInsight);
            _rewarded.OnBehaviourInsight(behaviourInsight);
        }

        private void InitAds()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("MAX SDK Initialized");
            };
                    
            MaxSdk.SetSdkKey(MaxSdkKey);
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
