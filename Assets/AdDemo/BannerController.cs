using System;
using System.Collections;
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
        private const int TimeoutInSeconds = 5;
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _show;
        [SerializeField] private Button _hide;
        [SerializeField] private Text _status;

        private string _selectedAdUnitId;
        private string _currentAdUnitId;
        private AdInsight _usedInsight;
        private int _consecutiveAdFails;
        
        private void GetInsightsAndLoad()
        {
            NeftaAdapterEvents.GetInsights(Insights.Banner, Load, TimeoutInSeconds);
        }
        
        private void Load(Insights insights)
        {
            _selectedAdUnitId = DefaultAdUnitId;
            _usedInsight = insights._banner;
            if (_usedInsight != null && _usedInsight._adUnit != null)
            {
                _selectedAdUnitId = _usedInsight._adUnit;
            }

            if (_currentAdUnitId != null)
            {
                MaxSdk.DestroyBanner(_currentAdUnitId);
                _currentAdUnitId = _selectedAdUnitId;
            }
            
            SetStatus($"Loading {_selectedAdUnitId} insights: {_usedInsight}");
            MaxSdk.CreateBanner(_currentAdUnitId, MaxSdkBase.BannerPosition.TopCenter);
            MaxSdk.ShowBanner(_currentAdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Banner, _usedInsight, adUnitId, errorInfo);
            
            SetStatus($"Load failed {adUnitId}: {errorInfo}");

            _consecutiveAdFails++;
            StartCoroutine(RetryLoadWithDelay());
        }
        
        private IEnumerator RetryLoadWithDelay()
        {
            // As per MAX recommendations, retry with exponentially higher delays up to 64s
            // In case you would like to customize fill rate / revenue please contact our customer support
            yield return new WaitForSeconds(new [] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(_consecutiveAdFails, 6)]);
            GetInsightsAndLoad();
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Banner, _usedInsight, adInfo);

            SetStatus($"Loaded {adInfo.NetworkName} {adInfo.NetworkPlacement}");
            
            _consecutiveAdFails = 0;
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
            SetStatus("Loading...");
            GetInsightsAndLoad();
            
            _show.interactable = false;
            _hide.interactable = true;

            AddDemoGameEventExample();
        }
        
        private void OnHideClick()
        {
            MaxSdk.HideBanner(_currentAdUnitId);
            
            _show.interactable = true;
            _hide.interactable = false;
        }

        public void SetAutoRefresh(bool refresh)
        {
            if (_currentAdUnitId != null)
            {
                if (refresh)
                {
                    MaxSdk.StartBannerAutoRefresh(_currentAdUnitId);
                }
                else
                {
                    MaxSdk.StopBannerAutoRefresh(_currentAdUnitId);
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
            new ProgressionEvent(type, status){ _source = source, _name = $"progression_{type}_{status} {source} {value}", _value = value }.Record();
        }
    }
}