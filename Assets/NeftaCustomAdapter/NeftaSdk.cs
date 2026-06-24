namespace NeftaCustomAdapter
{
    public class NeftaSdk
    {
        private const string IntegrationVersion = "1.3.0";
        
        private static bool _isInitialized;
        
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
                Interstitial.LoadInterstitialAd();   
            }
        }

        public static bool IsInterstitialReady(string adUnitId=null)
        {
            if (!Interstitial.IsOptimized)
            {
                return MaxSdk.IsInterstitialReady(adUnitId);
            }
            return Interstitial.IsInterstitialReady();
        }

        public static void ShowInterstitial(string adUnitId=null)
        {
            if (!Interstitial.IsOptimized)
            {
                MaxSdk.ShowInterstitial(adUnitId);
            }
            else
            {
                Interstitial.ShowAd();   
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
                Rewarded.LoadRewardedAd();   
            }
        }

        public static bool IsRewardedAdReady(string adUnitId=null)
        {
            if (!Rewarded.IsOptimized)
            {
                return MaxSdk.IsRewardedAdReady(adUnitId);
            }
            return Rewarded.IsRewardedAdReady();
        }

        public static void ShowRewardedAd(string adUnitId=null)
        {
            if (!Rewarded.IsOptimized)
            {
                MaxSdk.ShowRewardedAd(adUnitId);
            }
            else
            {
                Rewarded.ShowAd();   
            }
        }
    }
}