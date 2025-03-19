using System;
using System.Collections.Generic;
using NeftaCustomAdapter;

namespace AdDemo
{
    public class Interstitial : Ad
    {
        private readonly Action<string> _setStatus;
        private Action _onLoad;
        private string _loadedAdUnitId;
        
        public Interstitial(string adInsightName, List<AdConfig> adUnits, Action requestNewInsight, Action<string> setStatus, Action onLoad)
            : base(NeftaAdapterEvents.AdType.Interstitial, adInsightName, adUnits, requestNewInsight)
        {
            _setStatus = setStatus;
            _onLoad = onLoad;
                        
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnShowEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayeFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnRevenuePaidEvent;
        }

        public override void Load()
        {
            base.Load();
            _loadedAdUnitId = _selectedAdUnit.Id;
            MaxSdk.LoadInterstitial(_loadedAdUnitId);
        }
        
        public void Show()
        {
            if (MaxSdk.IsInterstitialReady(_loadedAdUnitId))
            { 
                _setStatus("Showing");
                MaxSdk.ShowInterstitial(_loadedAdUnitId);
            }
            else
            {
                _setStatus("Ad not ready");
            }
        }
        
        protected override void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            base.OnAdLoadedEvent(adUnitId, adInfo);
            
            _setStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");

            _onLoad();
        }

        protected override void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            base.OnAdFailedEvent(adUnitId, error);
            
            _setStatus($"Load failed: {error.Message}");
        }

        private void OnShowEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Show");
        }
        
        private void OnAdDisplayeFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Display failed");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Hidden");
        }
        
        private void OnRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus($"Paid {adInfo.Revenue}");
        }
    }
}