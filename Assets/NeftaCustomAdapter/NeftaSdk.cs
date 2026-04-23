namespace NeftaCustomAdapter
{
    public class NeftaSdk
    {
        private const string IntegrationVersion = "1.1.0";
        
        private static bool _isInitialized;
        
        public static InterstitialLogic Interstitial = new InterstitialLogic();
        public static RewardedLogic Rewarded = new RewardedLogic();

        internal static bool Passthrough => IsNeftaDisabled ||
                                            !Interstitial.IsDualTrackInitialized ||
                                            (NeftaAdapterEvents.InitConfiguration?._skipOptimization ?? false);
        
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
            if (Passthrough)
            {
                if (!IsNeftaDisabled)
                {
                    NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, adUnitId);
                }
                MaxSdk.LoadInterstitial(adUnitId);
            }
            else
            {
                Interstitial.LoadInterstitialAd();   
            }
        }

        public static bool IsInterstitialReady(string adUnitId=null)
        {
            if (Passthrough)
            {
                return MaxSdk.IsInterstitialReady(adUnitId);
            }
            return Interstitial.IsInterstitialReady();
        }

        public static void ShowInterstitial(string adUnitId=null)
        {
            if (Passthrough)
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
            if (Passthrough)
            {
                if (!IsNeftaDisabled)
                {
                    NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, adUnitId);
                }
                MaxSdk.LoadRewardedAd(adUnitId);
            }
            else
            {
                Rewarded.LoadRewardedAd();   
            }
        }

        public static bool IsRewardedAdReady(string adUnitId=null)
        {
            if (Passthrough)
            {
                return MaxSdk.IsRewardedAdReady(adUnitId);
            }
            return Rewarded.IsRewardedAdReady();
        }

        public static void ShowRewardedAd(string adUnitId=null)
        {
            if (Passthrough)
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