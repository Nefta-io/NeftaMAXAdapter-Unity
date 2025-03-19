using System;
using System.Collections.Generic;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public abstract class Ad
    {
        private NeftaAdapterEvents.AdType _adType;
        private string _adInsightName;
        private Action GetInsights;
        private List<AdConfig> _adUnits;
        
        private Dictionary<string, Insight> _insights;
        protected AdConfig _selectedAdUnit;
        private double _calculatedBidFloor;
        private int _consecutiveAdFail;
        private bool _isLoadPending;

        private void SelectAdUnitFromInsights()
        {
            _selectedAdUnit = _adUnits[0];
            
            if (_insights != null)
            {
                _calculatedBidFloor = _insights[_adInsightName]._float;

                foreach (var adUnit in _adUnits)
                {
                    if (adUnit._cpm > _calculatedBidFloor)
                    {
                        break;
                    }
                    _selectedAdUnit = adUnit;
                }
                Debug.Log($"SelectAdUnitFromInsights for {_adType}: {_selectedAdUnit.Id}/cpm:{_selectedAdUnit._cpm}, calculated bid floor: {_calculatedBidFloor}");
            }
        }

        protected Ad(NeftaAdapterEvents.AdType adType, string adInsightName, List<AdConfig> adUnits, Action getInsights)
        {
            _adType = adType;
            _adInsightName = adInsightName;
            _adUnits = adUnits;
            GetInsights = getInsights;
        }

        public virtual void Load()
        {
            if (_selectedAdUnit == null)
            {
                SelectAdUnitFromInsights();
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

        protected virtual void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
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
        }

        protected virtual void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(_adType, _selectedAdUnit._cpm, _calculatedBidFloor, adInfo);
        }
    }
}