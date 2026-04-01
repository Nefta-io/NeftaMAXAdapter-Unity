using System;

namespace NeftaCustomAdapter
{
    public class RewardedLogic : AdLogic
    {
        protected override string LogTag => "NeftaRewarded";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Rewarded;
        protected override int InsightType => Insights.Rewarded;
        
        public Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> OnAdReceivedRewardEvent;

        public override void InitializeDualTrack(string adUnitIdA, string adUnitIdB)
        {
            base.InitializeDualTrack(adUnitIdA, adUnitIdB);
            
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

        protected override void LoadInternal(string adUnitId, string bidFloor)
        {
            MaxSdk.SetRewardedAdExtraParameter(adUnitId, "disable_auto_retries", bidFloor != null ? "true" : "false");
            MaxSdk.SetRewardedAdExtraParameter(adUnitId, "jC7Fp", bidFloor ?? "");
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        public bool IsRewardedAdReady()
        {
            return IsAdReady();
        }
        
        protected override bool TryShow(Track adRequest)
        {
            adRequest.AdInfo = null;
            if (MaxSdk.IsRewardedAdReady(adRequest.AdUnitId))
            {
                adRequest.State = State.Shown;
                Log($"Showing {adRequest.AdUnitId}");
                MaxSdk.ShowRewardedAd(adRequest.AdUnitId);
                return true;
            }
            adRequest.State = State.Idle;
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