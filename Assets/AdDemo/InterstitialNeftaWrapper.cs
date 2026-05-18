using System;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class InterstitialNeftaWrapper : IInterstitial
    {
        private InterstitialUi _ui;
        private int _consecutiveAdFails;
        
        public InterstitialNeftaWrapper(bool isOptimized)
        {
            if (isOptimized)
            {
                NeftaSdk.Interstitial.InitializeDualTrack(InterstitialUi.AdUnitIdA, InterstitialUi.AdUnitIdB);   
            }
        }
        
        public void Init(InterstitialUi ui)
        {
            _ui = ui;
            
            NeftaSdk.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            NeftaSdk.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            NeftaSdk.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            NeftaSdk.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
        }
        
        public void Load()
        {
            NeftaSdk.LoadInterstitial(InterstitialUi.AdUnitIdA);
        }
        
        public void Show()
        {
            if (NeftaSdk.IsInterstitialReady(InterstitialUi.AdUnitIdA))
            {
                NeftaSdk.ShowInterstitial(InterstitialUi.AdUnitIdA);
            }
            else
            {
                Load();
            }
            _ui.SetAvailability(NeftaSdk.IsInterstitialReady(InterstitialUi.AdUnitIdA));
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
    }
}