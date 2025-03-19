using System;
using System.Collections.Generic;
using NeftaCustomAdapter;

namespace AdDemo
{
    public class Banner : Ad
    {
        private readonly Action<string> _setStatus;
        private string _createdBannerId;
        
        public Banner(string adInsightName, List<AdConfig> adUnits, Action requestNewInsight, Action<string> setStatus)
            : base(NeftaAdapterEvents.AdType.Banner, adInsightName, adUnits, requestNewInsight) {
            
            _setStatus = setStatus;
            
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        public override void Load()
        {
            base.Load();
            
            if (_selectedAdUnit.Id != _createdBannerId)
            {
                if (_createdBannerId != null)
                {
                    MaxSdk.DestroyBanner(_createdBannerId);
                }
                _createdBannerId = _selectedAdUnit.Id;
                MaxSdk.CreateBanner(_createdBannerId, MaxSdkBase.BannerPosition.TopCenter);
            }
            MaxSdk.ShowBanner(_createdBannerId);
        }

        public void Hide()
        {
            MaxSdk.HideBanner(_createdBannerId);
        }
        
        protected override void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            base.OnAdFailedEvent(adUnitId, errorInfo);
            
            _setStatus("Load failed");
        }
        
        protected override void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            base.OnAdLoadedEvent(adUnitId, adInfo);
            
            _setStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");
        }

        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus("Clicked");
        }

        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _setStatus($"Paid {adInfo.Revenue}");
        }
    }
}