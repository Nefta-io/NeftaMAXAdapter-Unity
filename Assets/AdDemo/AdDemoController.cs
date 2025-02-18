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
        private const string BannerAdUnitId = "d066ee44f5d29f8b";
        private const string InterstitialAdUnitId = "c9acf50602329bfe";
        private const string RewardedAdUnitId = "08304643cb16df3b";
        
        private const string NeftaId = "5661184053215232";
        
        [DllImport("__Internal")]
        private static extern void CheckTrackingPermission();
#else // UNITY_ANDROID
        private const string BannerAdUnitId = "6345b3fa80c73572";
        private const string InterstitialAdUnitId = "60bbc7cc56dfa329";
        private const string RewardedAdUnitId = "3082ee9199cf59f0";
        
        private const string NeftaId = "5643649824063488";
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

            NeftaAdapterEvents.BehaviourInsightCallback = OnBehaviourInsight;
            NeftaAdapterEvents.GetBehaviourInsight(new string[] { "p_churn_14d"});
            
#if UNITY_EDITOR || !UNITY_IOS
            InitAds();
#endif
        }
        
        private void OnBehaviourInsight(Dictionary<string, Insight> behaviourInsight)
        {
            foreach (var insight in behaviourInsight)
            {
                var insightValue = insight.Value;
                Debug.Log($"BehaviourInsight {insight.Key} status:{insightValue._status} i:{insightValue._int} f:{insightValue._float} s:{insightValue._string}");
            }
        }

        private void InitAds()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("MAX SDK Initialized");
            };
                    
            MaxSdk.SetSdkKey(MaxSdkKey);
            MaxSdk.InitializeSdk();

            _banner.Init(BannerAdUnitId);
            _interstitial.Init(InterstitialAdUnitId);
            _rewarded.Init(RewardedAdUnitId);
        }
        
#if !UNITY_EDITOR && UNITY_IOS
        private void Update()
        {
            if (!_permissionChecked)
            {
                _stateTime += Time.deltaTime;
                if (_stateTime > 1f)
                {
                    _permissionChecked = true;
                    
                    CheckTrackingPermission();
                        
                    InitAds();
                }
            }
        }
#endif
    }
}
