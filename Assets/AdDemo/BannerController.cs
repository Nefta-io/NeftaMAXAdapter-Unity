using System.Collections;
using System.Collections.Generic;
using Nefta.Core.Events;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Type = Nefta.Core.Events.Type;

namespace AdDemo
{
    public class BannerController : MonoBehaviour
    {
#if UNITY_IOS
        private const string DefaultAdUnitId = "d066ee44f5d29f8b";
#else // UNITY_ANDROID
        private const string DefaultAdUnitId = "6345b3fa80c73572";
#endif
        private const string AdUnitIdInsightName = "recommended_banner_ad_unit_id";
        private const string FloorPriceInsightName = "calculated_user_floor_price_banner";
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _show;
        [SerializeField] private Button _hide;
        [SerializeField] private Text _status;

        private string _recommendedAdUnitId;
        private double _calculatedBidFloor;
        private bool _isLoadRequested;
        private string _currentBannerId;

        private void GetInsightsAndLoad()
        {
            _isLoadRequested = true;
            
            NeftaAdapterEvents.GetBehaviourInsight(new string[] { FloorPriceInsightName }, OnBehaviourInsight);
            
            StartCoroutine(LoadFallback());
        }
        
        private void OnBehaviourInsight(Dictionary<string, Insight> insights)
        {
            _recommendedAdUnitId = null;
            _calculatedBidFloor = 0;
            if (insights.TryGetValue(AdUnitIdInsightName, out var insight))
            {
                _recommendedAdUnitId = insight._string;
            }
            if (insights.TryGetValue(FloorPriceInsightName, out insight))
            {
                _calculatedBidFloor = insight._float;
            }
            
            Debug.Log($"OnBehaviourInsight for Banner calculated bid floor: {_calculatedBidFloor}");
            
            if (_isLoadRequested)
            {
                Load();
            }
        }
        
        private void Load()
        {
            _isLoadRequested = false;
            
            _currentBannerId = DefaultAdUnitId;
            if (!string.IsNullOrEmpty(_recommendedAdUnitId))
            {
                _currentBannerId = _recommendedAdUnitId;
            }
            MaxSdk.CreateBanner(_currentBannerId, MaxSdkBase.BannerPosition.TopCenter);
            MaxSdk.SetBannerExtraParameter(_currentBannerId, "adaptive_banner", "false");
            MaxSdk.SetBannerExtraParameter(_currentBannerId, "banner_width", "320");
            MaxSdk.ShowBanner(_currentBannerId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Banner, _recommendedAdUnitId, _calculatedBidFloor, adUnitId, errorInfo);
            
            _show.interactable = true;
            _hide.interactable = false;
            SetStatus($"Load failed {adUnitId}: {errorInfo}");

            StartCoroutine(ReTryLoad());
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Banner, _recommendedAdUnitId, _calculatedBidFloor, adInfo);
            
            SetStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");
        }

        public void Init()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

            _title.text = "Banner";
            _show.onClick.AddListener(OnShowClick);
            _hide.onClick.AddListener(OnHideClick);
            _hide.interactable = false;
        }
        
        private void OnShowClick()
        {
            GetInsightsAndLoad();
            
            SetStatus("Loading...");
            
            _show.interactable = false;
            _hide.interactable = true;

            AddDemoGameEventExample();
        }
        
        private void OnHideClick()
        {
            MaxSdk.HideBanner(_currentBannerId);
            _show.interactable = true;
            _hide.interactable = false;
        }
        
        private IEnumerator LoadFallback()
        {
            yield return new WaitForSeconds(5f);

            if (_isLoadRequested)
            {
                _recommendedAdUnitId = null;
                _calculatedBidFloor = 0f;
                Load();
            }
        }
        
        private IEnumerator ReTryLoad()
        {
            yield return new WaitForSeconds(5f);
            
            GetInsightsAndLoad();
        }

        public void SetAutoRefresh(bool refresh)
        {
            if (_currentBannerId != null)
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
        }
        
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("Clicked");
        }

        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"Paid {adInfo.Revenue}");
        }
        
        private void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"Banner: {status}");
        }

        private void AddDemoGameEventExample()
        {
            var type = (Type) Random.Range(0, 7);
            var status = (Status)Random.Range(0, 3);
            var source = (Source)Random.Range(0, 7);
            var value = Random.Range(0, 101);
            NeftaAdapterEvents.Record(new ProgressionEvent(type, status)
                { _source = source, _name = $"progression_{type}_{status} {source} {value}", _value = value });
        }
    }
}