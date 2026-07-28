namespace NeftaCustomAdapter
{
    public class NeftaSdk
    {
        private const string IntegrationVersion = "1.2.0";
        
        private static bool _isInitialized;
        
        public static InterstitialLogic Interstitial = new InterstitialLogic();
        public static RewardedLogic Rewarded = new RewardedLogic();

        public static void Initialize()
        {
            if (!_isInitialized)
            {
                NeftaAdapterEvents.SetExtraParameter("nefta-sdk-integration-version", IntegrationVersion);
                NeftaAdapterEvents.AddNewSessionCallback(OnNewSession);
                _isInitialized = true;
            }
        }

        private static void OnNewSession()
        {
            if (Interstitial.IsDualTrackInitialized)
            {
                Interstitial.OnNewSession();
            }
            if (Rewarded.IsDualTrackInitialized)
            {
                Rewarded.OnNewSession();
            }
        }
        
        public static void LoadInterstitial(string adUnitId=null)
        {
            if (!Interstitial.IsDualTrackInitialized)
            {
                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, adUnitId);
                MaxSdk.LoadInterstitial(adUnitId);
            }
            else
            {
                Interstitial.LoadInterstitialAd();   
            }
        }

        public static bool IsInterstitialReady(string adUnitId=null)
        {
            if (!Interstitial.IsDualTrackInitialized)
            {
                return MaxSdk.IsInterstitialReady(adUnitId);
            }
            return Interstitial.IsInterstitialReady();
        }

        public static void ShowInterstitial(string adUnitId=null, string placement=null, string customData=null)
        {
            if (!Interstitial.IsDualTrackInitialized)
            {
                MaxSdk.ShowInterstitial(adUnitId, placement, customData);
            }
            else
            {
                Interstitial.ShowAd(placement, customData);   
            }
        }

        public static void LoadRewardedAd(string adUnitId=null)
        {
            if (!Rewarded.IsDualTrackInitialized)
            {
                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, adUnitId);
                MaxSdk.LoadRewardedAd(adUnitId);
            }
            else
            {
                Rewarded.LoadRewardedAd();   
            }
        }

        public static bool IsRewardedAdReady(string adUnitId=null)
        {
            if (!Rewarded.IsDualTrackInitialized)
            {
                return MaxSdk.IsRewardedAdReady(adUnitId);
            }
            return Rewarded.IsRewardedAdReady();
        }

        public static void ShowRewardedAd(string adUnitId=null, string placement=null, string customData=null)
        {
            if (!Rewarded.IsDualTrackInitialized)
            {
                MaxSdk.ShowRewardedAd(adUnitId, placement, customData);
            }
            else
            {
                Rewarded.ShowAd(placement, customData);   
            }
        }
    }
}