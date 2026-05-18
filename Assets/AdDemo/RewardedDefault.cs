using System;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class RewardedDefault : IRewarded
    {
        private RewardedUi _ui;
        private int _consecutiveAdFails;

        public void Init(RewardedUi ui)
        {
            _ui = ui;
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
        }

        public void Load()
        {
            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, RewardedUi.AdUnitIdA);

            MaxSdk.LoadRewardedAd(RewardedUi.AdUnitIdA);
        }
        
        public void Show()
        {
            if (MaxSdk.IsRewardedAdReady(RewardedUi.AdUnitIdA))
            {
                MaxSdk.ShowRewardedAd(RewardedUi.AdUnitIdA);
            }
            else if (_ui.IsAutoLoad)
            {
                Load();
            }
            _ui.SetAvailability(false);
        }

        public void OnUpdate(float delta) {}
        
         private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);
            
            _ui.SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");
            
            _consecutiveAdFails = 0;
            
            _ui.SetAvailability(true);
        }
        
        private void OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            
            _ui.SetStatus($"Load failed {adUnitId} with: {errorInfo}");
            
            _consecutiveAdFails++;
            _ = LoadWithDelay();
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdDisplayFailedEvent");
            
            if (_ui.IsAutoLoad)
            {
                Load();
            }
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdHideEvent");

            if (_ui.IsAutoLoad)
            {
                Load();
            }
        }
        
        private async Task LoadWithDelay()
        {
            var delay = new[] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(_consecutiveAdFails, 6)];
            await Task.Delay(delay * 1000);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            Load();
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdReceivedRewardEvent");
        }
        
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationImpression(adUnitId, adInfo);

            Debug.Log($"OnAdRevenuePaidEvent: {adInfo.Revenue}");
        }


        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo);

            Debug.Log("OnAdClickedEvent");
        }
    }
}