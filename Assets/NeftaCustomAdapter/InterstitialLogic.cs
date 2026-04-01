namespace NeftaCustomAdapter
{
    public class InterstitialLogic : AdLogic
    {
        protected override string LogTag => "NeftaInterstitial";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Interstitial;
        protected override int InsightType => Insights.Interstitial;

        public override void InitializeDualTrack(string adUnitIdA, string adUnitIdB)
        {
            base.InitializeDualTrack(adUnitIdA, adUnitIdB);
            
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
        
        protected override void LoadInternal(string adUnitId, string bidFloor)
        {
            MaxSdk.SetInterstitialExtraParameter(adUnitId, "disable_auto_retries", bidFloor != null ? "true" : "false");
            MaxSdk.SetInterstitialExtraParameter(adUnitId, "jC7Fp", bidFloor ?? "");
            MaxSdk.LoadInterstitial(adUnitId);
        }

        public bool IsInterstitialReady()
        {
            return IsAdReady();
        }

        protected override bool TryShow(Track track)
        {
            track.AdInfo = null;
            if (MaxSdk.IsInterstitialReady(track.AdUnitId))
            {
                track.State = State.Shown;
                Log($"Showing {track.AdUnitId}");
                MaxSdk.ShowInterstitial(track.AdUnitId);
                return true;
            }
            track.State = State.Idle;
            return false;
        }
    }
}