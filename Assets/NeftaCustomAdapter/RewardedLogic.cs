using System;

namespace NeftaCustomAdapter
{
    public class RewardedLogic : AdLogic
    {
        protected override string LogTag => "NeftaRewarded";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Rewarded;
        protected override int InsightType => Insights.Rewarded;
        
        public Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> OnAdReceivedRewardEvent;
        
        public void Initialize(bool isOptimized, string adUnitIdA, string adUnitIdB)
        {
            NeftaSdk.Initialize();
            NeftaAdapterEvents.SetRewardedLogic(isOptimized);
            if (isOptimized)
            {
                InitializeDualTrack(adUnitIdA, adUnitIdB);
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

        protected override void LoadInternal(string adUnitId, bool disableAutoRetries, string bidFloor)
        {
            MaxSdk.SetRewardedAdExtraParameter(adUnitId, "disable_auto_retries", disableAutoRetries ? "true" : "false");
            MaxSdk.SetRewardedAdExtraParameter(adUnitId, "jC7Fp", bidFloor);
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        public bool IsRewardedAdReady()
        {
            return IsAdReady();
        }
        
        protected override bool TryShow(Track track)
        {
            track.AdInfo = null;
            if (MaxSdk.IsRewardedAdReady(track.AdUnitId))
            {
                track.State = State.Shown;
                Log($"Showing {track.AdUnitId}");
                MaxSdk.ShowRewardedAd(track.AdUnitId);
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