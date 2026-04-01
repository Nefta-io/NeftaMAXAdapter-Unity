using System.Collections.Generic;
using System.Globalization;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class SimulatorInterstitialLogic : InterstitialLogic
    {
        private readonly Color DefaultColor = new Color(0.6509804f, 0.1490196f, 0.7490196f, 1f);
        private readonly Color FillColor = Color.green;
        private readonly Color NoFillColor = Color.red;
        
        protected override string LogTag => "SimInterstitial";

        private readonly Image _rendererFill2A;
        private readonly Button _fill2A;
        private readonly Image _rendererFill1A;
        private readonly Button _fill1A;
        private readonly Image _rendererNoFillA;
        private readonly Button _noFillA;
        private readonly Image _rendererOtherA;
        private readonly Button _otherA;
        private readonly Text _statusA;
        private readonly Image _rendererFill2B;
        private readonly Button _fill2B;
        private readonly Image _rendererFill1B;
        private readonly Button _fill1B;
        private readonly Image _rendererNoFillB;
        private readonly Button _noFillB;
        private readonly Image _rendererOtherB;
        private readonly Button _otherB;
        private readonly Text _statusB;
        
        private double _simAFloor = -1;
        private MaxSdkBase.AdInfo _simAAdInfo;
        private double _simBFloor = -1;
        private MaxSdkBase.AdInfo _simBAdInfo; 

        public SimulatorInterstitialLogic(Image rendererFill2A, Button fill2A,
            Image rendererFill1A, Button fill1A,
            Image rendererNoFillA, Button noFillA,
            Image rendererOtherA, Button otherA, Text statusA,
            Image rendererFill2B, Button fill2B,
            Image rendererFill1B, Button fill1B,
            Image rendererNoFillB, Button noFillB,
            Image rendererOtherB, Button otherB, Text statusB)
        {
            _rendererFill2A = rendererFill2A;
            _fill2A = fill2A;
            _rendererFill1A = rendererFill1A;
            _fill1A = fill1A;
            _rendererNoFillA = rendererNoFillA;
            _noFillA = noFillA;
            _rendererOtherA = rendererOtherA;
            _otherA = otherA;
            _statusA = statusA;
            
            _rendererFill2B = rendererFill2B;
            _fill2B = fill2B;
            _rendererFill1B = rendererFill1B;
            _fill1B = fill1B;
            _rendererNoFillB = rendererNoFillB;
            _noFillB = noFillB;
            _rendererOtherB = rendererOtherB;
            _otherB = otherB;
            _statusB = statusB;
            
            ToggleTrackA(false);
            fill2A.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackA, true); });
            fill1A.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackA, false); });
            noFillA.onClick.AddListener(() => { SimOnAdFailedEvent(_trackA, 2); });
            otherA.onClick.AddListener(() => { SimOnAdFailedEvent(_trackA, 0); });
        
            ToggleTrackB(false);
            fill2B.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackB, true); });
            fill1B.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackB, false); });
            noFillB.onClick.AddListener(() => { SimOnAdFailedEvent(_trackB, 2); });
            otherB.onClick.AddListener(() => { SimOnAdFailedEvent(_trackB, 0); });
            
            _trackA = new Track("Interstitial A");
            _trackB = new Track("Interstitial B");
            NeftaSdk.Initialize();
            IsDualTrackInitialized = true;
        }

        public override void OnNewSession()
        {
            _simAFloor = -1;
            _simAAdInfo = null;
            ToggleTrackA(true);
            ToggleTrackA(false);
            _statusA.text = "";
            
            _simBFloor = -1;
            _simBAdInfo = null;
            ToggleTrackB(true);
            ToggleTrackB(false);
            _statusB.text = "";
            
            base.OnNewSession();
        }

        protected override void LoadInternal(string adUnitId, string bidFloor)
        {
            if (adUnitId == _trackA.AdUnitId)
            {
                _simAFloor = string.IsNullOrEmpty(bidFloor) ? -1 : double.Parse(bidFloor, CultureInfo.InvariantCulture);
            }
            else
            {
                _simBFloor = string.IsNullOrEmpty(bidFloor) ? -1 : double.Parse(bidFloor, CultureInfo.InvariantCulture);
            }
            SimLoad(adUnitId);
        }

        protected override bool TryShow(Track track)
        {
            var adInfo = track.AdInfo;
            track.AdInfo = null;
            
            if (track.AdUnitId == _trackA.AdUnitId)
            {
                ToggleTrackA(true);
                ToggleTrackA(false);
                
                if (_simAAdInfo == null)
                {
                    track.State = State.Idle;
                    _statusA.text = "Failed to show A";
                    return false;
                }
                _statusA.text = "Showing A";
                _simAAdInfo = null;
            }
            else
            {
                ToggleTrackB(true);
                ToggleTrackB(false);
                
                if (_simBAdInfo == null)
                {
                    track.State = State.Idle;
                    _statusB.text = "Failed to show B";
                    return false;
                }
                _statusB.text = "Showing B";
                _simBAdInfo = null;
            }
            
            track.State = State.Shown;
            SimulatorController.ShowAd(
                "Interstitial",
                () =>
                {
                    OnAdDisplayedCallback(track.AdUnitId, adInfo);
                    OnAdRevenuePaidCallback(track.AdUnitId, adInfo);
                },
                () => { OnAdClickedCallback(track.AdUnitId, adInfo); },
                () => { },
                () => { OnAdHiddenCallback(track.AdUnitId, adInfo); });
            
            return true;
        }
        
        private void SimLoad(string adUnitId)
        {
            if (adUnitId == _trackA.AdUnitId)
            {
                ToggleTrackA(true);
                _statusA.text = $"{adUnitId} loading " + (_simAFloor >= 0 ? "as Optimized": "as Default");
            }
            else
            {
                ToggleTrackB(true);
                _statusB.text = $"{adUnitId} loading " + (_simBFloor >= 0 ? "as Optimized": "as Default");
            }
        }
        
        private void ToggleTrackA(bool isOn)
        {
            _fill2A.interactable = isOn;
            _fill1A.interactable = isOn;
            _noFillA.interactable = isOn;
            _otherA.interactable = isOn;
            if (isOn)
            {
                _rendererFill2A.color = DefaultColor;
                _rendererFill1A.color = DefaultColor;
                _rendererNoFillA.color = DefaultColor;
                _rendererOtherA.color = DefaultColor;
            }
        }

        private void ToggleTrackB(bool isOn)
        {
            _fill2B.interactable = isOn;
            _fill1B.interactable = isOn;
            _noFillB.interactable = isOn;
            _otherB.interactable = isOn;
            if (isOn)
            {
                _rendererFill2B.color = DefaultColor;
                _rendererFill1B.color = DefaultColor;
                _rendererNoFillB.color = DefaultColor;
                _rendererOtherB.color = DefaultColor;
            }
        }
        
        private void SimOnAdLoadedEvent(Track track, bool high)
        {
            var revenue = high ? 0.002 : 0.001;
            if (track == _trackA && _simAAdInfo != null)
            {
                _simAAdInfo = null;
                if (high)
                {
                    _rendererFill2A.color = DefaultColor;
                    _fill2A.interactable = false;
                }
                else
                {
                    _rendererFill1A.color = DefaultColor;
                    _fill1A.interactable = false;
                }
                return;
            }
            if (track == _trackB && _simBAdInfo != null)
            {
                _simBAdInfo = null;
                if (high)
                {
                    _rendererFill2B.color = DefaultColor;
                    _fill2B.interactable = false;
                }
                else
                {
                    _rendererFill1B.color = DefaultColor;
                    _fill1B.interactable = false;
                }
                return;
            }
            
            var adInfo = new MaxSdkBase.AdInfo(new Dictionary<string, object>()
            {
                { "adUnitId", track.AdUnitId },
                { "adFormat", "INTER" },
                { "networkName", "simulator network" },
                { "creativeId", "simulator creative"+ track.AdUnitId },
                { "revenue", revenue },
                { "revenuePrecision", "exact" },
                { "waterfallInfo", GetWaterfallDictionary(new [] { MaxSdkBase.MaxAdLoadState.AdLoaded, MaxSdkBase.MaxAdLoadState.AdLoadNotAttempted })}
            });
            if (track == _trackA)
            {
                _simAAdInfo = adInfo;
                ToggleTrackA(false);
                if (high)
                {
                    _rendererFill2A.color = FillColor;
                    _fill2A.interactable = true;
                }
                else
                {
                    _rendererFill1A.color = FillColor;
                    _fill1A.interactable = true;
                }
                _simAFloor = -1;
                _statusA.text = $"{track.AdUnitId} loaded {revenue}";
            }
            else
            {
                _simBAdInfo = adInfo;
                ToggleTrackB(false);
                if (high)
                {
                    _rendererFill2B.color = FillColor;
                    _fill2B.interactable = true;
                }
                else
                {
                    _rendererFill1B.color = FillColor;
                    _fill1B.interactable = true;
                }
                _simBFloor = -1;
                _statusB.text = $"{track.AdUnitId} loaded {revenue}";
            }
            
            OnAdLoadedCallback(track.AdUnitId, adInfo);
        }
        
        private void SimOnAdFailedEvent(Track track, int status)
        {
            if (track == _trackA)
            {
                if (status == 2)
                {
                    _rendererNoFillA.color = NoFillColor;
                }
                else
                {
                    _rendererOtherA.color = NoFillColor;
                }
                _simAFloor = -1;
                _statusA.text = $"{track.AdUnitId} failed";
                ToggleTrackA(false);
            }
            else
            {
                if (status == 2)
                {
                    _rendererNoFillB.color = NoFillColor;
                }
                else
                {
                    _rendererOtherB.color = NoFillColor;
                }
                _simBFloor = -1;
                _statusB.text = $"{track.AdUnitId} failed";
                ToggleTrackB(false);
            }
            
            OnAdFailedCallback(track.AdUnitId,
                new MaxSdkBase.ErrorInfo(new Dictionary<string, object>()
                {
                    { "errorCode", status == 2 ? 204 : -1 },
                    { "errorMessage", status == 2 ? "no fill" : "other" },
                    { "waterfallInfo", GetWaterfallDictionary(new [] { MaxSdkBase.MaxAdLoadState.FailedToLoad , MaxSdkBase.MaxAdLoadState.FailedToLoad }) }
                })
            ); 
        }

        private Dictionary<string, object> GetWaterfallDictionary(MaxSdkBase.MaxAdLoadState[] loadStates)
        {
            var responses = new List<object>();
            for (var i = 0; i < loadStates.Length; i++)
            {
                Dictionary<string, object> error = null;
                if (loadStates[i] == MaxSdkBase.MaxAdLoadState.FailedToLoad)
                {
                    error = new Dictionary<string, object>()
                    {
                        { "errorCode", "-1" },
                        { "errorMessage", "simulator error message" },
                        { "latencyMillis", "45" }
                    };
                }

                responses.Add(new Dictionary<string, object>()
                {
                    { "adLoadState", ((int)loadStates[i]).ToString() },
                    {
                        "mediatedNetwork", new Dictionary<string, object>
                        {
                            { "name", $"simulator network {i}" },
                            { "adapterClassName", "simulator adapter" },
                            { "adapterVersion", "1.0.0" },
                            { "sdkVersion", "13.0.0" }
                        }
                    },
                    { "credentials", new Dictionary<string, object>() },
                    { "isBidding", "true" },
                    { "latencyMillis", UnityEngine.Random.Range(0, 200).ToString() },
                    { "error", error }
                });
            }

            return new Dictionary<string, object>()
            {
                { "name", "simulator waterfall" },
                { "testName", "waterfall test name" },
                { "networkResponses", responses },
                { "latencyMillis", UnityEngine.Random.Range(0, 200).ToString() }
            };
        }
    }
}