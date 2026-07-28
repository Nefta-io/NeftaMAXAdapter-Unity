namespace NeftaCustomAdapter
{
    public class InterstitialLogic : AdLogic
    {
        protected override string LogTag => "NeftaInterstitial";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Interstitial;
        protected override int InsightType => Insights.Interstitial;
        
        public void Initialize(bool isOptimized, string adUnitIdA, string adUnitIdB)
        {
            NeftaSdk.Initialize();
            NeftaAdapterEvents.SetInterstitialLogic(isOptimized);
            if (isOptimized)
            {
                InitializeDualTrack(adUnitIdA, adUnitIdB);
            }

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedCallback;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdFailedCallback;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedCallback;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnAdDisplayedCallback;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnAdClickedCallback;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenCallback;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidCallback;
        }
        
        public void LoadInterstitialAd()
        {
            LoadOrTriggerAlreadyLoaded();
        }
        
        protected override void LoadInternal(string adUnitId, bool disableAutoRetries, string bidFloor)
        {
            MaxSdk.SetInterstitialExtraParameter(adUnitId, "disable_auto_retries", disableAutoRetries ? "true" : "false");
            MaxSdk.SetInterstitialExtraParameter(adUnitId, "jC7Fp", bidFloor);
            MaxSdk.LoadInterstitial(adUnitId);
        }

        public bool IsInterstitialReady()
        {
            return IsAdReady();
        }

        protected override bool TryShow(Track track, string placement, string customData)
        {
            track.AdInfo = null;
            if (MaxSdk.IsInterstitialReady(track.AdUnitId))
            {
                track.State = State.Shown;
                Log($"Showing {track.AdUnitId}");
                MaxSdk.ShowInterstitial(track.AdUnitId, placement, customData);
                return true;
            }
            track.State = State.Idle;
            return false;
        }
    }
}