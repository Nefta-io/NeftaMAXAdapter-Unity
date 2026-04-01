namespace NeftaCustomAdapter
{
    public class NeftaSdk
    {
        private const string IntegrationVersion = "1.0.0";
        
        private static bool _isInitialized;
        
        public static InterstitialLogic Interstitial = new InterstitialLogic();
        public static RewardedLogic Rewarded = new RewardedLogic();

        private static bool Passthrough => IsNeftaDisabled || (NeftaAdapterEvents.InitConfiguration?._skipOptimization ?? false);
        
        public static bool IsNeftaDisabled;

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
            if (!Passthrough)
            {
                Interstitial?.OnNewSession();
                Rewarded?.OnNewSession();   
            }
        }
        
        public static void LoadInterstitial(string adUnitId=null)
        {
            if (Passthrough || !Interstitial.IsDualTrackInitialized)
            {
                MaxSdk.LoadInterstitial(adUnitId);
            }
            else
            {
                Interstitial.LoadInterstitialAd();   
            }
        }

        public static bool IsInterstitialReady(string adUnitId=null)
        {
            if (Passthrough || !Interstitial.IsDualTrackInitialized)
            {
                return MaxSdk.IsInterstitialReady(adUnitId);
            }
            return Interstitial.IsInterstitialReady();
        }

        public static void ShowInterstitial(string adUnitId=null)
        {
            if (Passthrough || !Interstitial.IsDualTrackInitialized)
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
            if (Passthrough || !Rewarded.IsDualTrackInitialized)
            {
                MaxSdk.LoadRewardedAd(adUnitId);
            }
            else
            {
                Rewarded.LoadRewardedAd();   
            }
        }

        public static bool IsRewardedAdReady(string adUnitId=null)
        {
            if (Passthrough || !Rewarded.IsDualTrackInitialized)
            {
                return MaxSdk.IsRewardedAdReady(adUnitId);
            }
            return Rewarded.IsRewardedAdReady();
        }

        public static void ShowRewardedAd(string adUnitId=null)
        {
            if (Passthrough || !Rewarded.IsDualTrackInitialized)
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