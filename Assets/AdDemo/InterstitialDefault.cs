using System;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class InterstitialDefault : IInterstitial
    {
        private InterstitialUi _ui;
        private int _consecutiveAdFails;
        
        public void Init(InterstitialUi ui)
        {
            _ui = ui;
            
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnAdClickedEvent;
        }

        public void Load()
        {
            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, InterstitialUi.AdUnitIdA);
            
            MaxSdk.LoadInterstitial(InterstitialUi.AdUnitIdA);
        }
        
        public void Show()
        {
            if (MaxSdk.IsInterstitialReady(InterstitialUi.AdUnitIdA))
            {
                MaxSdk.ShowInterstitial(InterstitialUi.AdUnitIdA);
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