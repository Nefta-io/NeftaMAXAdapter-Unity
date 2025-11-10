using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class PlacementController : MonoBehaviour
    {
        private const int TimeoutInSeconds = 5;
        private readonly Color DefaultColor = new Color(0.6509804f, 0.1490196f, 0.7490196f, 1f);
        private readonly Color FillColor = Color.green;
        private readonly Color NoFillColor = Color.red;

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
        
        [Header("Controls")]
        [SerializeField] private RectTransform _rootRect;
        [SerializeField] private bool _isRewarded;
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        [Header("Track A")]
        [SerializeField] private Text _aStatus;
        [SerializeField] private Image _aFill2Renderer;
        [SerializeField] private Button _aFill2;
        [SerializeField] private Image _aFill1Renderer;
        [SerializeField] private Button _aFill1;
        [SerializeField] private Image _aNoFillRenderer;
        [SerializeField] private Button _aNoFill;
        [SerializeField] private Image _aOtherRenderer;
        [SerializeField] private Button _aOther;
        
        [Header("Track B")]
        [SerializeField] private Text _bStatus;
        [SerializeField] private Image _bFill2Renderer;
        [SerializeField] private Button _bFill2;
        [SerializeField] private Image _bFill1Renderer;
        [SerializeField] private Button _bFill1;
        [SerializeField] private Image _bNoFillRenderer;
        [SerializeField] private Button _bNoFill;
        [SerializeField] private Image _bOtherRenderer;
        [SerializeField] private Button _bOther;
        
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
            
            NeftaAdapterEvents.GetInsights(_isRewarded ? Insights.Rewarded : Insights.Interstitial, adRequest.Insight, (Insights insights) => {
                var insight = _isRewarded ? insights._rewarded : insights._interstitial;
                SetStatus($"Load with Insights: {insight}");
                if (insight != null && _load.isOn)
                {
                    adRequest.Insight = insight;
                    var bidFloor = insight._floorPrice.ToString(CultureInfo.InvariantCulture);
                    SimSetExtraParameter(adRequest.AdUnitId, "disable_auto_retries", "true");
                    SimSetExtraParameter(adRequest.AdUnitId, "jC7Fp", bidFloor);
                    
                    NeftaAdapterEvents.OnExternalMediationRequest(_isRewarded ? NeftaAdapterEvents.AdType.Rewarded : NeftaAdapterEvents.AdType.Interstitial, adRequest.AdUnitId, insight);
                    
                    SetStatus($"Loading {adRequest.AdUnitId} as Optimized with {bidFloor}");
                    SimLoad(adRequest.AdUnitId);
                }
                else
                {
                    adRequest.ConsecutiveAdFails++;
                    StartCoroutine(RetryGetInsightsAndLoad(adRequest));
                }
            }, TimeoutInSeconds);
        }
        
        private void LoadDefault(AdRequest adRequest)
        {
            adRequest.State = State.Loading;
            
            SetStatus($"Loading {adRequest.AdUnitId} as Default");
            
            NeftaAdapterEvents.OnExternalMediationRequest(_isRewarded ? NeftaAdapterEvents.AdType.Rewarded : NeftaAdapterEvents.AdType.Interstitial, adRequest.AdUnitId);
            
            SimLoad(adRequest.AdUnitId);
        }
        
        private void OnAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            
            var adRequest = adUnitId == _adRequestA.AdUnitId ? _adRequestA : _adRequestB;
            SetStatus($"Load Failed {adRequest.AdUnitId}: {errorInfo}");
            
            adRequest.ConsecutiveAdFails++;
            StartCoroutine(RetryGetInsightsAndLoad(adRequest));
            
            _isFirstResponseReceived = true;
            if (_load.isOn)
            {
                StartLoading();
            }
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);

            var adRequest = adUnitId == _adRequestA.AdUnitId ? _adRequestA : _adRequestB;
            SetStatus($"Loaded {adRequest.AdUnitId} at: {adInfo.Revenue}");
            
            adRequest.Insight = null;
            adRequest.ConsecutiveAdFails = 0;
            adRequest.Revenue = adInfo.Revenue;
            adRequest.State = State.Ready;

            UpdateShowButton();

            _isFirstResponseReceived = true;
            if (_load.isOn)
            {
                StartLoading();
            }
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
            _adRequestA = new AdRequest("Track A");
            _adRequestB = new AdRequest("Track B");
            
            ToggleTrackA(false);
            _aFill2.onClick.AddListener(() => { SimOnAdLoadedEvent(_adRequestA, 2); });
            _aFill1.onClick.AddListener(() => { SimOnAdLoadedEvent(_adRequestA, 1); });
            _aNoFill.onClick.AddListener(() => { SimOnAdFailedEvent(_adRequestA, 2); });
            _aOther.onClick.AddListener(() => { SimOnAdFailedEvent(_adRequestA, 0); });
            
            ToggleTrackB(false);
            _bFill2.onClick.AddListener(() => { SimOnAdLoadedEvent(_adRequestB, 2); });
            _bFill1.onClick.AddListener(() => { SimOnAdLoadedEvent(_adRequestB, 1); });
            _bNoFill.onClick.AddListener(() => { SimOnAdFailedEvent(_adRequestB, 2); });
            _bOther.onClick.AddListener(() => { SimOnAdFailedEvent(_adRequestB, 0); });
            
            
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
            
            if (SimIsReady(adRequest.AdUnitId))
            {
                SetStatus($"Showing {adRequest.AdUnitId}");
                SimShow(adRequest.AdUnitId);
                return true;
            }
            return false;
        }

        private void OnShowEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnShowEvent");
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
            NeftaAdapterEvents.OnExternalMediationImpression(adUnitId, adInfo);
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus("OnAdReceivedRewardEvent");
        }
        
        private void OnAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SetStatus($"On Ad clicked {adUnitId}");
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo);
        }
        
        private void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"NeftaPluginMAX Simulator: {status}");
        }
        
        private void UpdateShowButton()
        {
            _show.interactable = _adRequestA.State == State.Ready || _adRequestB.State == State.Ready;
        }
        
        private void SimSetExtraParameter(string adUnitId, string key, string value)
        {
            if (key == "jC7Fp")
            {
                if (adUnitId == _adRequestA.AdUnitId)
                {
                    _simAFloor = double.Parse(value, CultureInfo.InvariantCulture);
                }
                else
                {
                    _simBFloor = double.Parse(value, CultureInfo.InvariantCulture);
                }
            }
        }

        private void SimLoad(string adUnitId)
        {
            if (adUnitId == _adRequestA.AdUnitId)
            {
               ToggleTrackA(true);
               _aStatus.text = $"{adUnitId} loading " + (_simAFloor >= 0 ? "as Optimized": "as Default");
            }
            else
            {
                ToggleTrackB(true);
                _bStatus.text = $"{adUnitId} loading " + (_simBFloor >= 0 ? "as Optimized": "as Default");
            }
        }

        private bool SimIsReady(string adUnitId)
        {
            return true;
        }

        private void SimShow(string adUnitId)
        {
            var adInfo = adUnitId == _adRequestA.AdUnitId ? _simAAdInfo : _simBAdInfo;
            
            OnRevenuePaidEvent(adUnitId, adInfo);

            var simulatorAdPrefab = Resources.Load<SimulatorAd>("SimulatorAd");
            var simAd = Instantiate(simulatorAdPrefab, _rootRect);
            simAd.Init(_isRewarded ? "Rewarded" : "Interstitial",
                () => { OnShowEvent(adUnitId, adInfo); },
                () => { OnAdClickEvent(adUnitId, adInfo); },
                _isRewarded ? () =>
                {
                    var reward = new MaxSdkBase.Reward();
                    reward.Amount = 1;
                    reward.Label = "Sim Rewarded";
                    OnAdReceivedRewardEvent(adUnitId, reward, adInfo);
                } : null,
                () => { OnAdHiddenEvent(adUnitId, adInfo); });
            

            if (adUnitId == _adRequestA.AdUnitId)
            {
                _aStatus.text = "Showing A";
                _simAAdInfo = null;
            }
            else
            {
                _bStatus.text = "Showing B";
                _simBAdInfo = null;
            }
        }

        private double _simAFloor = -1;
        private MaxSdkBase.AdInfo _simAAdInfo;
        private double _simBFloor = -1;
        private MaxSdkBase.AdInfo _simBAdInfo; 
        private void SimOnAdLoadedEvent(AdRequest adRequest, double revenue)
        {
            var adInfo = new MaxSdkBase.AdInfo(new Dictionary<string, object>()
            {
                { "adUnitId", adRequest.AdUnitId },
                { "adFormat", _isRewarded ? "REWARDED" : "INTER" },
                { "networkName", "simulator" },
                { "revenue", revenue },
                { "revenuePrecision", "exact" }
            });
            if (adRequest == _adRequestA)
            {
                _simAAdInfo = adInfo;
                ToggleTrackA(false);
                if (revenue >= 2)
                {
                    _aFill2Renderer.color = FillColor;
                }
                else
                {
                    _aFill1Renderer.color = FillColor;
                }
                _simAFloor = -1;
                _aStatus.text = $"{adRequest.AdUnitId} loaded {revenue}";
            }
            else
            {
                _simBAdInfo = adInfo;
                ToggleTrackB(false);
                if (revenue >= 2)
                {
                    _bFill2Renderer.color = FillColor;
                }
                else
                {
                    _bFill1Renderer.color = FillColor;
                }
                _simBFloor = -1;
                _bStatus.text = $"{adRequest.AdUnitId} loaded {revenue}";
            }
            
            OnAdLoadedEvent(adRequest.AdUnitId, adInfo);
        }

        private void ToggleTrackA(bool isOn)
        {
            _aFill2.interactable = isOn;
            _aFill1.interactable = isOn;
            _aNoFill.interactable = isOn;
            _aOther.interactable = isOn;
            if (isOn)
            {
                _aFill2Renderer.color = DefaultColor;
                _aFill1Renderer.color = DefaultColor;
                _aNoFillRenderer.color = DefaultColor;
                _aOtherRenderer.color = DefaultColor;
            }
        }

        private void ToggleTrackB(bool isOn)
        {
            _bFill2.interactable = isOn;
            _bFill1.interactable = isOn;
            _bNoFill.interactable = isOn;
            _bOther.interactable = isOn;
            if (isOn)
            {
                _bFill2Renderer.color = DefaultColor;
                _bFill1Renderer.color = DefaultColor;
                _bNoFillRenderer.color = DefaultColor;
                _bOtherRenderer.color = DefaultColor;
            }
        }
        
        private void SimOnAdFailedEvent(AdRequest adRequest, int status)
        {
            if (adRequest == _adRequestA)
            {
                if (status == 2)
                {
                    _aNoFillRenderer.color = NoFillColor;
                }
                else
                {
                    _aOtherRenderer.color = NoFillColor;
                }
                _simAFloor = -1;
                ToggleTrackA(false);
            }
            else
            {
                if (status == 2)
                {
                    _bNoFillRenderer.color = NoFillColor;
                }
                else
                {
                    _bOtherRenderer.color = NoFillColor;
                }
                _simAFloor = -1;
                ToggleTrackB(false);
            }
            
            OnAdFailedEvent(adRequest.AdUnitId,
                new MaxSdkBase.ErrorInfo(new Dictionary<string, object>()
                {
                    { "errorCode", status == 2 ? 204 : -1 },
                    { "errorMessage", status == 2 ? "no fill" : "other" }
                })
            ); 
        }
    }
}