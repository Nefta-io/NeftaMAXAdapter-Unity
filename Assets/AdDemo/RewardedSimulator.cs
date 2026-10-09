using System.Collections.Generic;
using System.Globalization;
using NeftaCustomAdapter;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class RewardedSimulator : RewardedLogic
    {
        private readonly Color DefaultColor = new Color(0.6509804f, 0.1490196f, 0.7490196f, 1f);
        private readonly Color FillColor = Color.green;
        private readonly Color NoFillColor = Color.red;
        
        protected override string LogTag => "SimRewarded";

        private readonly Image[] _rendererFill3;
        private readonly Button[] _fill3;
        private readonly Image[] _rendererFill2;
        private readonly Button[] _fill2;
        private readonly Image[] _rendererFill1;
        private readonly Button[] _fill1;
        private readonly Image[] _rendererNoFill;
        private readonly Button[] _noFill;
        private readonly Image[] _rendererOther;
        private readonly Button[] _other;
        private readonly Text[] _status;
        
        private double[] _simFloors = new double[] { -1f, -1f, -1f };
        private MaxSdkBase.AdInfo[] _simAdInfos = new MaxSdkBase.AdInfo[3];

        public RewardedSimulator(bool isOptimized,
            Image rendererFill3A, Button fill3A,
            Image rendererFill2A, Button fill2A,
            Image rendererFill1A, Button fill1A,
            Image rendererNoFillA, Button noFillA,
            Image rendererOtherA, Button otherA, Text statusA,
            Image rendererFill3B, Button fill3B,
            Image rendererFill2B, Button fill2B,
            Image rendererFill1B, Button fill1B,
            Image rendererNoFillB, Button noFillB,
            Image rendererOtherB, Button otherB, Text statusB,
            Image rendererFill3C, Button fill3C,
            Image rendererFill2C, Button fill2C,
            Image rendererFill1C, Button fill1C,
            Image rendererNoFillC, Button noFillC,
            Image rendererOtherC, Button otherC, Text statusC)
        {
            _rendererFill3 = new[] { rendererFill3C, rendererFill3A, rendererFill3B };
            _fill3 = new[] { fill3C, fill3A, fill3B };
            
            _rendererFill2 = new[] { rendererFill2C, rendererFill2A, rendererFill2B };
            _fill2 = new[] { fill2C, fill2A, fill2B };
            
            _rendererFill1 = new[] { rendererFill1C, rendererFill1A, rendererFill1B };
            _fill1 = new[] { fill1C, fill1A, fill1B };
            
            _rendererNoFill = new[] { rendererNoFillC, rendererNoFillA, rendererNoFillB };
            _noFill = new[] { noFillC, noFillA, noFillB };
            
            _rendererOther = new[] { rendererOtherC, rendererOtherA, rendererOtherB };
            _other = new[] { otherC, otherA, otherB };
            
            _status = new[] { statusC, statusA, statusB };

            _tracks = new Track[3];
            for (var t = 0; t < 3; t++)
            {
                var ti = t;
                ToggleTrack(t, false);
                _fill3[t].onClick.AddListener(() => { SimOnAdLoadedEvent(ti, _tracks[ti], 3); });
                _fill2[t].onClick.AddListener(() => { SimOnAdLoadedEvent(ti, _tracks[ti], 2); });
                _fill1[t].onClick.AddListener(() => { SimOnAdLoadedEvent(ti, _tracks[ti], 1); });
                _noFill[t].onClick.AddListener(() => { SimOnAdFailedEvent(_tracks[ti], 2); });
                _other[t].onClick.AddListener(() => { SimOnAdFailedEvent(_tracks[ti], 0); });                
            }
            
            _tracks[0] = new Track("Rewarded D", false);
            _tracks[1] = new Track("Rewarded A", true);
            _tracks[2] = new Track("Rewarded B", true);
            NeftaSdk.Initialize();
            IsOptimized = isOptimized;
        }

        public void UnoptimizedLoad()
        {
            if (_tracks[0].State == State.Idle)
            {
                NeftaAdapterEvents.OnExternalMediationRequest(AdType, _tracks[0].AdUnitId);
                SimLoad(_tracks[0].AdUnitId);
            }
        }

        protected override void LoadAsOptimized(string adUnitId, string bidFloor)
        {
            for (var t = 0; t < _tracks.Length; t++)
            {
                if (adUnitId == _tracks[t].AdUnitId)
                {
                    _simFloors[t] = string.IsNullOrEmpty(bidFloor) ? -1 : double.Parse(bidFloor, CultureInfo.InvariantCulture);
                    break;
                }
            }
            SimLoad(adUnitId);
        }
        
        protected override void LoadAsDefault(string adUnitId)
        {
            SimLoad(adUnitId);
        }

        public bool UnoptimizedShow()
        {
            return TryShow(_tracks[0], null, null);
        }
        
        public void ResetTrack()
        {
            _tracks[0].State = State.Idle;
        }

        protected override bool TryShow(Track track, string placement, string customData)
        {
            MaxSdkBase.AdInfo adInfo = null;
            track.AdInfo = null;
            track.State = State.Shown;
            
            for (var t = 0; t < _tracks.Length; t++)
            {
                if (_tracks[t] == track)
                {
                    ToggleTrack(t, true);
                    ToggleTrack(t, false);
                
                    if (_simAdInfos[t] == null)
                    {
                        track.State = State.Idle;
                        _status[t].text = $"Failed to show {track.AdUnitId}";
                        return false;
                    }
                    _status[t].text = $"Showing {track.AdUnitId}";
                    adInfo = _simAdInfos[t];
                    _simAdInfos[t] = null;
                }
            }
            
            track.State = State.Shown;
            SimulatorUi.ShowAd(
                "Rewarded",
                () =>
                {
                    OnAdDisplayedCallback(track.AdUnitId, adInfo);
                    OnAdRevenuePaidCallback(track.AdUnitId, adInfo);
                },
                () => { OnAdClickedCallback(track.AdUnitId, adInfo); },
                () =>
                {
                    var reward = new MaxSdkBase.Reward
                    {
                        Amount = 1,
                        Label = "Sim Rewarded"
                    };
                    OnAdReceivedRewardCallback(track.AdUnitId, reward, adInfo);
                },
                () =>
                {
                    track.State = State.Idle;
                    OnAdHiddenCallback(track.AdUnitId, adInfo);
                });
            
            return true;
        }
        
        private void SimLoad(string adUnitId)
        {
            for (var t = 0; t < _tracks.Length; t++)
            {
                if (_tracks[t].AdUnitId == adUnitId)
                {
                    var isDynamic = _simFloors[t] >= 0;
                    _tracks[t].State = State.Loading;
                    ToggleTrack(t, true);
                    _status[t].text = $"{adUnitId} loading " + (isDynamic ? "as Optimized": "as Default");
                }
            }
        }
        
        private void ToggleTrack(int i, bool isOn)
        {
            _fill3[i].interactable = isOn;
            _fill2[i].interactable = isOn;
            _fill1[i].interactable = isOn;
            _noFill[i].interactable = isOn;
            _other[i].interactable = isOn;
            if (isOn)
            {
                _rendererFill3[i].color = DefaultColor;
                _rendererFill2[i].color = DefaultColor;
                _rendererFill1[i].color = DefaultColor;
                _rendererNoFill[i].color = DefaultColor;
                _rendererOther[i].color = DefaultColor;
            }
        }

        private void SimOnAdLoadedEvent(int t, Track track, int r)
        {
            var revenue = r == 3 ? 0.003 : r == 2 ? 0.002 : 0.001;
            if (_simAdInfos[t] != null)
            {
                _simAdInfos[t] = null;

                _rendererFill3[t].color = DefaultColor;
                _fill3[t].interactable = false;
                
                _rendererFill2[t].color = DefaultColor;
                _fill2[t].interactable = false;

                _rendererFill1[t].color = DefaultColor;
                _fill1[t].interactable = false;

                return;
            }

            track.State = State.Ready;
            var adInfo = SimulatorUi.GetAdInfo(track.AdUnitId, "REWARDED", revenue);

            _simAdInfos[t] = adInfo;
            ToggleTrack(t, false);
            if (r == 3)
            {
                _rendererFill3[t].color = FillColor;
                _fill3[t].interactable = true;
            }
            else if (r == 2)
            {
                _rendererFill2[t].color = FillColor;
                _fill2[t].interactable = true;
            }
            else
            {
                _rendererFill1[t].color = FillColor;
                _fill1[t].interactable = true;
            }
            _simFloors[t] = -1;
            _status[t].text = $"{track.AdUnitId} loaded {revenue}";
            
            OnAdLoadedCallback(track.AdUnitId, adInfo);
        }
        
        private void SimOnAdFailedEvent(Track track, int status)
        {
            for (var t = 0; t < _tracks.Length; t++)
            {
                if (_tracks[t] == track)
                {
                    if (status == 2)
                    {
                        _rendererNoFill[t].color = NoFillColor;
                    }
                    else
                    {
                        _rendererOther[t].color = NoFillColor;
                    }
                    _simFloors[t] = -1;
                    _status[t].text = $"{track.AdUnitId} failed";
                    ToggleTrack(t, false);
                }
            }
            
            OnAdFailedCallback(track.AdUnitId,
                new MaxSdkBase.ErrorInfo(new Dictionary<string, object>()
                {
                    { "errorCode", status == 2 ? 204 : -1 },
                    { "errorMessage", status == 2 ? "no fill" : "other" },
                    { "waterfallInfo", SimulatorUi.GetWaterfallDictionary(new [] { MaxSdkBase.MaxAdLoadState.FailedToLoad , MaxSdkBase.MaxAdLoadState.FailedToLoad }) }
                })
            ); 
        }
        
        public class TrackStatus
        {
            public enum State
            {
                Idle,
                Loading,
                Ready,
                Shown,
                Delay
            }

            private Track _track;

            public TrackStatus(object track)
            {
                _track = (Track) track;
            }

            public State GetState => (State)(int) _track.State;
            public AdInsight GetInsight => _track.Insight;
        }
        
        public TrackStatus GetTrack(int t)
        {
            return new TrackStatus(_tracks[t]);
        }
    }
}