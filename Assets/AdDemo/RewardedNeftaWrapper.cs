using System;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class RewardedNeftaWrapper : IRewarded
    {
        private RewardedUi _ui;
        private int _consecutiveAdFails;

        public RewardedNeftaWrapper(bool isOptimized)
        {
            if (isOptimized)
            {
                NeftaSdk.Rewarded.InitializeDualTrack(RewardedUi.AdUnitIdA, RewardedUi.AdUnitIdB);   
            }
        }
        
        public void Init(RewardedUi ui)
        {
            _ui = ui;
            
            NeftaSdk.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            NeftaSdk.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            NeftaSdk.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            NeftaSdk.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            NeftaSdk.Rewarded.OnAdHiddenEvent += OnAdHiddenEvent;
        }
        
        public void Load()
        {
            NeftaSdk.LoadRewardedAd(RewardedUi.AdUnitIdA);
        }
        
        public void Show()
        {
            if (NeftaSdk.IsRewardedAdReady(RewardedUi.AdUnitIdA))
            {
                NeftaSdk.ShowRewardedAd(RewardedUi.AdUnitIdA);
            }
            else
            {
                Load();
            }
            _ui.SetAvailability(NeftaSdk.IsRewardedAdReady(RewardedUi.AdUnitIdA));
        }
        
        public void OnUpdate(float delta) {}
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");
            
            _consecutiveAdFails = 0;
            
            _ui.SetAvailability(true);
        }
        
        private void OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
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
    }
}