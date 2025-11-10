using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        private const string AdUnitIdA = "78b66d4cd80ca1e7";
        private const string AdUnitIdB = "c9acf50602329bfe";
#else // UNITY_ANDROID
        private const string AdUnitIdA = "850bcc93f949090c";
        private const string AdUnitIdB = "60bbc7cc56dfa329";
#endif
        private const int TimeoutInSeconds = 5;

        private enum State
        {
            Idle,
            LoadingWithInsights,
            Loading,
            Ready
        }
        
        private class AdRequest
        {
            public readonly string AdUnitId;
            public State State;
            public AdInsight Insight;
            public double Revenue;
            public int ConsecutiveAdFails;

            public AdRequest(string adUnitId)
            {
                AdUnitId = adUnitId;
            }
        }
        
        private AdRequest _adRequestA;
        private AdRequest _adRequestB;
        private bool _isFirstResponseReceived;
        
        private Queue<string> _statusQueue;

        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private void StartLoading()
        {
            Load(_adRequestA, _adRequestB.State);
            Load(_adRequestB, _adRequestA.State);
        }

        private void Load(AdRequest request, State otherState)
        {
            if (request.State == State.Idle)
            {
                if (otherState != State.LoadingWithInsights)
                {
                    GetInsightsAndLoad(request); 
                }
                else if (_isFirstResponseReceived)
                {
                    LoadDefault(request);
                }
            }
        }
        
        private void GetInsightsAndLoad(AdRequest adRequest)
        {
            adRequest.State = State.LoadingWithInsights;
            
            NeftaAdapterEvents.GetInsights(Insights.Interstitial, adRequest.Insight, (Insights insights) => {
                var insight = insights._interstitial;
                SetStatus($"Load with Insights: {insight}");
                if (insight != null && _load.isOn)
                {
                    adRequest.Insight = insight;
                    var bidFloor = insight._floorPrice.ToString(CultureInfo.InvariantCulture);
                    MaxSdk.SetInterstitialExtraParameter(adRequest.AdUnitId, "disable_auto_retries", "true");
                    MaxSdk.SetInterstitialExtraParameter(adRequest.AdUnitId, "jC7Fp", bidFloor);
                    
                    NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, adRequest.AdUnitId, insight);
                    
                    SetStatus($"Loading {adRequest.AdUnitId} as Optimized with floor: {bidFloor}");
                    MaxSdk.LoadInterstitial(adRequest.AdUnitId);
                }
            }, TimeoutInSeconds);
        }
        
        private void LoadDefault(AdRequest adRequest)
        {
            adRequest.State = State.Loading;
            
            SetStatus($"Loading {adRequest.AdUnitId} as Default");
            
            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, adRequest.AdUnitId);
            
            MaxSdk.LoadInterstitial(adRequest.AdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            
            var adRequest = adUnitId == _adRequestA.AdUnitId ? _adRequestA : _adRequestB;
            SetStatus($"Load Failed {adRequest.AdUnitId}: {errorInfo}");

            _isFirstResponseReceived = true;
            adRequest.ConsecutiveAdFails++;
            
            StartLoading();
            
            StartCoroutine(RetryGetInsightsAndLoad(adRequest));
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);

            var adRequest = adUnitId == _adRequestA.AdUnitId ? _adRequestA : _adRequestB;
            SetStatus($"Loaded {adRequest.AdUnitId} at: {adInfo.Revenue}");

            _isFirstResponseReceived = true;
            adRequest.Insight = null;
            adRequest.ConsecutiveAdFails = 0;
            adRequest.Revenue = adInfo.Revenue;
            adRequest.State = State.Ready;

            UpdateShowButton();
            
            StartLoading();
        }
        
        private IEnumerator RetryGetInsightsAndLoad(AdRequest adRequest)
        {
            yield return new WaitForSeconds(GetMinWaitTime(adRequest.ConsecutiveAdFails));

            adRequest.State = State.Idle;
            if (_load.isOn)
            {
                StartLoading();
            }
        }

        private float GetMinWaitTime(int numberOfConsecutiveFails)
        {
            // As per MAX recommendations, retry with exponentially higher delays up to 64s
            // In case you would like to customize fill rate / revenue please contact our customer support
            return new [] { 0, 2, 4, 8, 16, 32, 64 }[Math.Min(numberOfConsecutiveFails, 6)];
        }
        
        private void Awake()
        {
            _adRequestA = new AdRequest(AdUnitIdA);
            _adRequestB = new AdRequest(AdUnitIdB);
            
            _statusQueue = new Queue<string>();
            
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnAdClickEvent;
            
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.onClick.AddListener(OnShowClick);
            
            _show.interactable = false;
        }
        
        private void OnLoadChanged(bool isOn)
        {
            if (isOn)
            {
                StartLoading();   
            }
            else
            {
                if (_adRequestA.State != State.Ready)
                {
                    _adRequestA.State = State.Idle;
                }

                if (_adRequestB.State != State.Ready)
                {
                    _adRequestB.State = State.Idle;
                }
            }
            
            AddDemoGameEventExample();
        }
        
        private void OnShowClick()
        {
            var isShown = false;
            if (_adRequestA.State == State.Ready)
            {
                if (_adRequestB.State == State.Ready && _adRequestB.Revenue > _adRequestA.Revenue)
                {
                    isShown = TryShow(_adRequestB);
                }
                if (!isShown)
                {
                    isShown = TryShow(_adRequestA);
                }
            }
            if (!isShown && _adRequestB.State == State.Ready)
            {
                TryShow(_adRequestB);
            }
            
            UpdateShowButton();
        }
        
        private bool TryShow(AdRequest adRequest)
        {
            adRequest.State = State.Idle;
            adRequest.Revenue = 0;
            
            if (MaxSdk.IsInterstitialReady(adRequest.AdUnitId))
            {
                SetStatus($"Showing {adRequest.AdUnitId}");
                MaxSdk.ShowInterstitial(adRequest.AdUnitId);
                return true;
            }
            return false;
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayFailedEvent");
        }
        
        private void OnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdDisplayedEvent");
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdHiddenEvent");
            
            // start new load cycle
            if (_load.isOn)
            {
                StartLoading();   
            }
        }
        
        private void OnRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"OnRevenuePaidEvent {adInfo.Revenue}");
        }
        
        private void OnAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"On Ad clicked {adUnitId}");
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
                    Debug.Log($"NeftaPluginMAX Interstitial: {status}");
                }
            }
        }
        
        private void UpdateShowButton()
        {
            _show.interactable = _adRequestA.State == State.Ready || _adRequestB.State == State.Ready;
        }

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) Random.Range(0, 9);
            var method = (ReceiveMethod)Random.Range(0, 8);
            var value = Random.Range(0, 101);
            new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value }.Record();
        }
    }
}