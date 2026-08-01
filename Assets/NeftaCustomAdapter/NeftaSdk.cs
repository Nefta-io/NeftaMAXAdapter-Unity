using UnityEngine;

namespace NeftaCustomAdapter
{
    public class NeftaSdk
    {
        private const string IntegrationVersion = "1.3.2";
        
        private static bool _isInitialized;
        private static bool _isNeftaInitialized;
        private static bool _isInterstitialLoadScheduled;
        private static bool _isRewardedLoadScheduled;
        
        public static InterstitialLogic Interstitial = new InterstitialLogic();
        public static RewardedLogic Rewarded = new RewardedLogic();

        public static void Initialize()
        {
            if (!_isInitialized)
            {
                NeftaAdapterEvents.SetExtraParameter("nefta-sdk-integration-version", IntegrationVersion);
                _isInitialized = true;
            }
        }
        
        public static void LoadInterstitial(string adUnitId=null)
        {
            if (!Interstitial.IsOptimized)
            {
                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, adUnitId);
                MaxSdk.LoadInterstitial(adUnitId);
            }
            else
            {
                if (_isNeftaInitialized)
                {
                    Interstitial.LoadInterstitialAd();
                }
                else
                {
                    Debug.Log("[NeftaPlugin] Delaying load request until Nefta Initialized");
                    _isInterstitialLoadScheduled = true;
                }
            }
        }

        public static bool IsInterstitialReady(string adUnitId=null)
        {
            if (!Interstitial.IsOptimized)
            {
                var isReady = MaxSdk.IsInterstitialReady(adUnitId);
                if (!isReady && Interstitial._adInfo != null && Interstitial._adInfo.AdUnitIdentifier == adUnitId)
                {
                    Interstitial._adInfo = null;
                }
                return isReady;
            }
            return Interstitial.IsInterstitialReady();
        }
        
        // Returns tracked adInfo to be shown
        // It's tracked from OnAdLoadedEvent till consumption of it (ShowInterstitial | OnAdDisplayedEvent | OnAdDisplayedFailedEvent | false IsInterstitialReady)
        // It can happen that ad invalidates on native, so if you need exact status call IsInterstitialReady(GetInterstitialAdReady()?.AdUnitIdentifier)
        public static MaxSdkBase.AdInfo GetInterstitialAdReady()
        {
            return Interstitial.GetAdReady();
        }

        public static void ShowInterstitial(string adUnitId=null, string placement=null, string customData=null)
        {
            if (!Interstitial.IsOptimized)
            {
                Interstitial._adInfo = null;
                MaxSdk.ShowInterstitial(adUnitId, placement, customData);
            }
            else
            {
                Interstitial.ShowAd(placement, customData);   
            }
        }

        public static void LoadRewardedAd(string adUnitId=null)
        {
            if (!Rewarded.IsOptimized)
            {
                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, adUnitId);
                MaxSdk.LoadRewardedAd(adUnitId);
            }
            else
            {
                if (_isNeftaInitialized)
                {
                    Rewarded.LoadRewardedAd();
                }
                else
                {
                    Debug.Log("[NeftaPlugin] Delaying load request until Nefta Initialized");
                    _isRewardedLoadScheduled = true;
                }
            }
        }

        public static bool IsRewardedAdReady(string adUnitId=null)
        {
            if (!Rewarded.IsOptimized)
            {
                var isReady = MaxSdk.IsRewardedAdReady(adUnitId);
                if (!isReady && Rewarded._adInfo != null && Rewarded._adInfo.AdUnitIdentifier == adUnitId)
                {
                    Rewarded._adInfo = null;
                }
                return isReady;
            }
            return Rewarded.IsRewardedAdReady();
        }

        // Returns tracked adInfo to be shown
        // It's tracked from OnAdLoadedEvent till consumption of it (ShowRewardedAd | OnAdDisplayedEvent | OnAdDisplayedFailedEvent | false IsRewardAdReady)
        // It can happen that ad invalidates on native, so if you need exact status call IRewardedAdReady(GetRewardedAdReady()?.AdUnitIdentifier)
        public static MaxSdkBase.AdInfo GetRewardedAdReady()
        {
            return Rewarded.GetAdReady();
        }

        public static void ShowRewardedAd(string adUnitId=null, string placement=null, string customData=null)
        {
            if (!Rewarded.IsOptimized)
            {
                Rewarded._adInfo = null;
                MaxSdk.ShowRewardedAd(adUnitId,  placement, customData);
            }
            else
            {
                Rewarded.ShowAd(placement, customData);   
            }
        }

        internal static void OnInit()
        {
            _isNeftaInitialized = true;
            if (_isInterstitialLoadScheduled)
            {
                _isInterstitialLoadScheduled = false;
                LoadInterstitial();
            }
            if (_isRewardedLoadScheduled)
            {
                _isRewardedLoadScheduled = true;
                LoadRewardedAd();
            }
        }
    }
}