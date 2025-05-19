using System;
using System.Collections;
using System.Collections.Generic;
using Nefta.Core.Events;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace AdDemo
{
    public class InterstitialController : MonoBehaviour
    {
#if UNITY_IOS
        private const string _defaultAdUnitId = "c9acf50602329bfe";
#else // UNITY_ANDROID
        private const string _defaultAdUnitId = "60bbc7cc56dfa329";
#endif
        
        private const string AdUnitIdInsightName = "recommended_interstitial_ad_unit_id";
        private const string FloorPriceInsightName = "calculated_user_floor_price_interstitial";
        
        private string _selectedAdUnitId;
        private string _recommendedAdUnitId;
        private double _calculatedBidFloor;
        private Coroutine _fallbackCoroutine;
        private Queue<string> _statusQueue;
        private Action<bool> _onFullScreenAdDisplayed;
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;

        private void GetInsightsAndLoad()
        {
            NeftaAdapterEvents.GetBehaviourInsight(new string[] { AdUnitIdInsightName, FloorPriceInsightName }, OnBehaviourInsight);
            
            _fallbackCoroutine = StartCoroutine(LoadFallback());
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

            Debug.Log($"OnBehaviourInsight for Interstitial recommended AdUnit: {_recommendedAdUnitId}, calculated bid floor: {_calculatedBidFloor}");
            
            if (_fallbackCoroutine != null)
            {
                Load();
            }
        }
        
        private void Load()
        {
            if (_fallbackCoroutine != null)
            {
                StopCoroutine(_fallbackCoroutine);
                _fallbackCoroutine = null;
            }
            
            _selectedAdUnitId = _recommendedAdUnitId ?? _defaultAdUnitId;
            MaxSdk.LoadInterstitial(_selectedAdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(NeftaAdapterEvents.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, adUnitId, errorInfo);
            
            SetStatus($"Load failed: {errorInfo.Message}");
            
            // or automatically retry with a delay
            //StartCoroutine(ReTryLoad());
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(NeftaAdapterEvents.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, adInfo);
            
            SetStatus($"Loaded {adUnitId}: {adInfo.NetworkName}");

            _show.interactable = true;
        }
        
        public void Init(Action<bool> onFullScreenAdDisplayed)
        {
            _statusQueue = new Queue<string>();
            _onFullScreenAdDisplayed = onFullScreenAdDisplayed;
            
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnShowEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnRevenuePaidEvent;
            
            _title.text = "Interstitial";
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);
            
            _show.interactable = false;
        }
        
        private void OnLoadClick()
        {
            GetInsightsAndLoad();

            AddDemoGameEventExample();
        }
        
        private void OnShowClick()
        {
            if (MaxSdk.IsInterstitialReady(_selectedAdUnitId))
            {
                SetStatus("Showing");
                MaxSdk.ShowInterstitial(_selectedAdUnitId);
            }
            else
            {
                SetStatus("Ad not ready");
            }
            
            _show.interactable = false;
        }
        
        private IEnumerator LoadFallback()
        {
            yield return new WaitForSeconds(5f);

            _recommendedAdUnitId = null;
            _calculatedBidFloor = 0;
            Load();
        }

        private IEnumerator ReTryLoad()
        {
            yield return new WaitForSeconds(5f);
            
            GetInsightsAndLoad();
        }

        private void OnShowEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnShowEvent");
            _onFullScreenAdDisplayed(true);
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayFailedEvent");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHiddenEvent");
            _onFullScreenAdDisplayed(false);
        }
        
        private void OnRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"OnRevenuePaidEvent {adInfo.Revenue}");
        }
        
        private void SetStatus(string status)
        {
            lock(_statusQueue)
            {
                _statusQueue.Enqueue(status);
            }
        }
        
        private void Update()
        {
            if (_statusQueue == null)
            {
                return;
            }
            
            lock (_statusQueue)
            {
                while (_statusQueue.Count > 0)
                {
                    var status = _statusQueue.Dequeue();
                    
                    _status.text = status;
                    Debug.Log($"Integration Interstitial: {status}");
                }
            }
        }

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (ReceiveMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            NeftaAdapterEvents.Record(new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value });
        }
    }
}