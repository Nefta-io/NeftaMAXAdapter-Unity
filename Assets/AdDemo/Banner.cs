using System;
using System.Collections.Generic;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class Banner
    {
        public static readonly string FloorPrice = "calculated_user_floor_price_banner";
        
        private List<AdConfig> _adUnits = new List<AdConfig>()
        {
            new AdConfig("d066ee44f5d29f8b", "6345b3fa80c73572", 50),
            new AdConfig("c9acf50602329bfe", "60bbc7cc56dfa329", 75),
            new AdConfig("08304643cb16df3b", "3082ee9199cf59f0", 100),
        };
        
        private Action GetInsights;
        private Dictionary<string, Insight> _insights;
        private AdConfig _selectedAdUnit;
        private double _calculatedBidFloor;
        private int _consecutiveAdFail;
        private bool _isLoadPending;

        
        private readonly Action<string> _setStatus;
        private string _currentBannerId;
        
        private void SelectAdUnitFromInsights()
        {
            _selectedAdUnit = _adUnits[0];
            
            if (_insights != null)
            {
                _calculatedBidFloor = _insights[FloorPrice]._float;

                foreach (var adUnit in _adUnits)
                {
                    if (adUnit._cpm > _calculatedBidFloor)
                    {
                        break;
                    }
                    _selectedAdUnit = adUnit;
                }
                Debug.Log($"SelectAdUnitFromInsights for Banner: {_selectedAdUnit.Id}/cpm:{_selectedAdUnit._cpm}, calculated bid floor: {_calculatedBidFloor}");
            }
        }
        
        public void OnBehaviourInsight(Dictionary<string, Insight> insights)
        {
            _insights = insights;
            
            SelectAdUnitFromInsights();

            if (_isLoadPending)
            {
                Load();
            }
        }

        
        public Banner(Action requestNewInsight, Action<string> setStatus)
        {
            GetInsights = requestNewInsight;
            
            SelectAdUnitFromInsights();
            
            _setStatus = setStatus;
            
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        public void Load()
        {
            if (_selectedAdUnit.Id != _currentBannerId)
            {
                if (_currentBannerId != null)
                {
                    MaxSdk.DestroyBanner(_currentBannerId);
                }
                _currentBannerId = _selectedAdUnit.Id;
                MaxSdk.CreateBanner(_currentBannerId, MaxSdkBase.BannerPosition.TopCenter);
                MaxSdk.SetBannerExtraParameter(_currentBannerId, "adaptive_banner", "false");
            }
            MaxSdk.ShowBanner(_currentBannerId);
        }

        public void SetAutoRefresh(bool refresh)
        {
            if (refresh)
            {
                MaxSdk.StartBannerAutoRefresh(_currentBannerId);
            }
            else
            {
                MaxSdk.StopBannerAutoRefresh(_currentBannerId);
            }
        }

        public void Hide()
        {
            MaxSdk.HideBanner(_currentBannerId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Banner, _selectedAdUnit._cpm, _calculatedBidFloor, adUnitId, errorInfo);
            
            if (errorInfo.Code == MaxSdkBase.ErrorCode.NoFill)
            {
                _consecutiveAdFail++;
                if (_consecutiveAdFail > 2)
                {
                    _selectedAdUnit = _adUnits[0];
                    Load();
                }
                else
                {
                    _isLoadPending = true;
                    GetInsights();
                }
            }
            
            _setStatus("Load failed");
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Banner, _selectedAdUnit._cpm, _calculatedBidFloor, adInfo);
            
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