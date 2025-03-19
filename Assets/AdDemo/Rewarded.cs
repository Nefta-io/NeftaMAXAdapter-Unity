using System;
using System.Collections.Generic;
using NeftaCustomAdapter;

namespace AdDemo
{
    public class Rewarded : Ad
    {
        private readonly Action<string> _setStatus;
        private readonly Action _onLoad;

        private string _loadedAdUnitId;

        public Rewarded(string adInsightName, List<AdConfig> adUnits, Action requestNewInsight, Action<string> setStatus, Action onLoad)
            : base(NeftaAdapterEvents.AdType.Interstitial, adInsightName, adUnits, requestNewInsight)
        {
            _setStatus = setStatus;
            _onLoad = onLoad;
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHideEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        public override void Load()
        {
            base.Load();
            
            _loadedAdUnitId = _selectedAdUnit.Id;
            MaxSdk.LoadRewardedAd(_loadedAdUnitId);
        }
        
        public void Show()
        {
            if (MaxSdk.IsRewardedAdReady(_loadedAdUnitId))
            {
                _setStatus("Showing");
                MaxSdk.ShowRewardedAd(_loadedAdUnitId);
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
        
        protected override void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            base.OnAdFailedEvent(adUnitId, errorInfo);
            
            _setStatus("Load failed");
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Display failed");
        }
        
        private void OnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Displayed");
        }
        
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Rewarded ad clicked");
        }
        
        private void OnAdHideEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Hidden");
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Rewarded ad received reward");
        }
        
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Revenue paid");
        }
    }
}