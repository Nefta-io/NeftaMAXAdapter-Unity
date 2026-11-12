namespace NeftaCustomAdapter
{
    public class InterstitialLogic : AdLogic
    {
        protected override string LogTag => "NeftaInterstitial";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Interstitial;
        protected override int InsightType => Insights.Interstitial;
        
        public void Initialize(bool isOptimized, string defaultAdUnit, string optimizedAdUnitA, string optimizedAdUnitB)
        {
            NeftaSdk.Initialize();
            if (isOptimized)
            {
                InitializeDualTrack(defaultAdUnit, optimizedAdUnitA, optimizedAdUnitB);
            }

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedCallback;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedCallback;
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
        
        protected override void LoadAsOptimized(string adUnitId, string bidFloor)
        {
            MaxSdk.SetInterstitialExtraParameter(adUnitId, "disable_auto_retries", "true");
            MaxSdk.SetInterstitialExtraParameter(adUnitId, "jC7Fp", bidFloor);
            MaxSdk.LoadInterstitial(adUnitId);
        }

        protected override void LoadAsDefault(string adUnitId)
        {
            MaxSdk.LoadInterstitial(adUnitId);
        }

        public override bool TryShow(Track track, string placement, string customData, bool triggerRewardOnHide)
        {
            track.AdInfo = null;
            if (MaxSdk.IsInterstitialReady(track.AdUnitId))
            {
                track.State = State.Shown;
                if (triggerRewardOnHide)
                {
                    track.IsInterstitialAsRewarded = true;
                }
                Log($"Showing {track.AdUnitId}");
                MaxSdk.ShowInterstitial(track.AdUnitId, placement, customData);
                return true;
            }
            track.State = State.Idle;
            return false;
        }

        public override void RefreshAvailability()
        {
            base.RefreshAvailability();

            if (NeftaSdk.Rewarded.IsOptimized && NeftaAdapterEvents.IsSwapInterToRewarded)
            {
                if (NeftaSdk.Rewarded.OnAdAvailabilityChanged != null)
                {
                    var bestTrack = NeftaSdk.Rewarded.GetReadyTrack();
                    var interstitialTrack = GetReadyTrack();
                    if (interstitialTrack != null && (bestTrack == null || interstitialTrack.AdInfo.Revenue > bestTrack.AdInfo.Revenue))
                    {
                        bestTrack = interstitialTrack;
                    }
                    NeftaSdk.Rewarded.OnAdAvailabilityChanged(bestTrack?.AdInfo);
                }
            }
        }
    }
}