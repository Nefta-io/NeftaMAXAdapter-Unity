using System.Collections.Generic;
using Nefta.Core.Events;
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
#else // UNITY_ANDROID
        private const string BannerAdUnitId = "6345b3fa80c73572";
        private const string InterstitialAdUnitId = "60bbc7cc56dfa329";
        private const string RewardedAdUnitId = "3082ee9199cf59f0";
#endif
        
        private bool _isBannerShown;

        [SerializeField] private BannerController _banner;
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;
        
        private void Awake()
        {
#if UNITY_IOS
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.Init("5661184053215232");
#else
            NeftaAdapterEvents.Init("5643649824063488");
#endif
            NeftaAdapterEvents.Record(new ProgressionEvent(Type.Achievement, Status.Start)
            {
                _source = Source.OptionalContent,
                _name = "area-69",
                _value = 23,
                _customString = "abc"
            });
        
            NeftaAdapterEvents.Record(new ReceiveEvent(ResourceCategory.CoreItem)
            {
                _method = ReceiveMethod.Reward,
                _value = 1,
                _name = "RewardedAds",
                _customString = "RV"
            });
            
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                // AppLovin SDK is initialized, configure and start loading ads.
                Debug.Log("MAX SDK Initialized");
            };
        
            MaxSdk.SetSdkKey(MaxSdkKey);
            MaxSdk.InitializeSdk();

            _banner.Init(BannerAdUnitId);
            _interstitial.Init(InterstitialAdUnitId);
            _rewarded.Init(RewardedAdUnitId);
        }
    }
}
