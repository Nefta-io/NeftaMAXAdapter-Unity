using System;

namespace NeftaCustomAdapter
{
    public class RewardedLogic : AdLogic
    {
        protected override string LogTag => "NeftaRewarded";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Rewarded;
        protected override int InsightType => Insights.Rewarded;
        
        public Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> OnAdReceivedRewardEvent;
        
        public void Initialize(bool isOptimized, string defaultAdUnit, string optimizedAdUnitA, string optimizedAdUnitB)
        {
            NeftaSdk.Initialize();
            if (isOptimized)
            {
                InitializeDualTrack(defaultAdUnit, optimizedAdUnitA, optimizedAdUnitB);
            }
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedCallback;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdFailedCallback;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedCallback;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayedCallback;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedCallback;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHiddenCallback;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardCallback;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidCallback;
        }
        
        public void LoadRewardedAd()
        {
            LoadOrTriggerAlreadyLoaded();
        }

        protected override void LoadAsOptimized(string adUnitId, string bidFloor)
        {
            MaxSdk.SetRewardedAdExtraParameter(adUnitId, "disable_auto_retries", "true");
            MaxSdk.SetRewardedAdExtraParameter(adUnitId, "jC7Fp", bidFloor);
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        protected override void LoadAsDefault(string adUnitId)
        {
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        public bool IsRewardedAdReady()
        {
            return IsAdReady();
        }
        
        protected override bool TryShow(Track track, string placement, string customData)
        {
            track.AdInfo = null;
            if (MaxSdk.IsRewardedAdReady(track.AdUnitId))
            {
                track.State = State.Shown;
                Log($"Showing {track.AdUnitId}");
                MaxSdk.ShowRewardedAd(track.AdUnitId, placement, customData);
                return true;
            }
            track.State = State.Idle;
            return false;
        }

        protected void OnAdReceivedRewardCallback(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            if (OnAdReceivedRewardEvent != null)
            {
                OnAdReceivedRewardEvent(adUnitId, reward, adInfo);
            }
        }
    }
}