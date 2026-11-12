using System.Globalization;
using NeftaCustomAdapter;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace AdDemo
{
    public class InterstitialNeftaManual : IInterstitial
    {
        public enum State
        {
            Idle,
            Loading,
            Ready,
            Shown,
            Delay
        }
                
        public class Track
        {
            public readonly string AdUnitId;
            public readonly bool IsOptimized;
            public State State;
            public AdInsight Insight;
            public MaxSdkBase.AdInfo AdInfo;
            public bool IsInterstitialAsRewarded;

            public Track(string adUnitId, bool isOptimized)
            {
                AdUnitId = adUnitId;
                IsOptimized = isOptimized;
            }
        }

        private Track[] _tracks;
        private bool _isFirstRequest = true;
        private bool _isFirstOptimizedResponseReceived;
        private int _isGlobalCooldown;

        private InterstitialUi _ui;
        
        public RewardedNeftaManual RewardedLogic;
        
        public void Init(InterstitialUi ui)
        {
            _ui = ui;

            _tracks = new Track[]
            {
                new Track(InterstitialUi.DefaultAdUnitId, false),
                new Track(InterstitialUi.OptimizedAdUnitIdA, true),
                new Track(InterstitialUi.OptimizedAdUnitIdB, true)
            };
            
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnAdClickedEvent;
        }

        public void Load()
        {
            if (_isFirstRequest)
            {
                Load(_tracks[1]);
                
                var stopWaitingForFirstResponseAfter = NeftaAdapterEvents.FirstResponseTimeoutInterstitial;
                if (stopWaitingForFirstResponseAfter > 0)
                {
                    _ = ForceStartSecondTrack(stopWaitingForFirstResponseAfter);
                }
            
                var backFillTrackDelay = NeftaAdapterEvents.BackfillInterstitialDelay;
                if (backFillTrackDelay == 0)
                {
                    Load(_tracks[0]);
                }
                else if (backFillTrackDelay > 0)
                {
                    _= WaitDelay(_tracks[0], backFillTrackDelay);
                }
            
                _isFirstRequest = false;
            }
            else
            {
                ContinueLoading();
            }
        }
        
        private void Load(Track track)
        {
            track.State = State.Loading;

            if (track.IsOptimized)
            {
                NeftaAdapterEvents.GetInsights(Insights.Interstitial, track.Insight, (Insights insights) =>
                {
                    _ui.SetStatus($"Load with Insights: {insights}");
                    if (insights.Insight != null)
                    {
                        LoadOptimizedTrack(track, insights.Insight);
                        
                        if (track == _tracks[1] && _tracks.Length > 2 && _tracks[2].State == State.Idle && insights.Insight._insightB != null) {
                            _tracks[2].State = State.Loading;
                            LoadOptimizedTrack(_tracks[2], insights.Insight._insightB);
                        }
                    }
                    else
                    {
                        _ = RetryOptimizedWithDelay(track);
                    }
                });   
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, track.AdUnitId);
            
                _ui.SetStatus($"Loading {track.AdUnitId} as Default");
                MaxSdk.LoadInterstitial(track.AdUnitId);
            }
        }

        private void LoadOptimizedTrack(Track track, AdInsight insight)
        {
            track.Insight = insight;
            MaxSdk.SetInterstitialExtraParameter(track.AdUnitId, "disable_auto_retries", "true");
            var bidFloor = "";
            if (track.Insight._floorPrice >= 0)
            {
                bidFloor = track.Insight._floorPrice.ToString(CultureInfo.InvariantCulture);
            }
            MaxSdk.SetInterstitialExtraParameter(track.AdUnitId, "jC7Fp", bidFloor);
                    
            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Interstitial, track.AdUnitId, track.Insight);
                    
            _ui.SetStatus($"Loading {track.AdUnitId} as Optimized with floor: {bidFloor}");
            MaxSdk.LoadInterstitial(track.AdUnitId);
        }

        public void Show()
        {
            var isShown = false;
            var readyTrack = GetReadyTrack();
            while (readyTrack != null)
            {
                if (TryShow(readyTrack, false))
                {
                    isShown = true;
                    readyTrack = null;
                }
                else
                {
                    readyTrack = GetReadyTrack();
                }
            }
            readyTrack = GetReadyTrack();
            var isAvailable = readyTrack != null;
            _ui.SetAvailability(isAvailable);
            if (NeftaAdapterEvents.IsSwapInterToRewarded && RewardedLogic != null)
            {
                RewardedLogic.OnInterstitialAvailabilityChanged(isAvailable);
            }
            if (!isShown)
            {
                ContinueLoading();   
            }
        }

        public bool TryShow(Track track, bool triggerRewardOnHide)
        {
            track.AdInfo = null;
            if (MaxSdk.IsInterstitialReady(track.AdUnitId))
            {
                track.State = State.Shown;
                if (triggerRewardOnHide)
                {
                    track.IsInterstitialAsRewarded = true;
                }
                _ui.SetStatus($"Showing {track.AdUnitId}");
                MaxSdk.ShowInterstitial(track.AdUnitId);
                return true;
            }
            track.State = State.Idle;
            return false;
        }
        
        private async Task RetryOptimizedWithDelay(Track track)
        {
            var delay = NeftaAdapterEvents.GetRetryDelayInSeconds(track.Insight, track.AdUnitId);
            await Task.Delay((int)(delay * 1000));
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            track.State = State.Idle;
            ContinueLoading();
        }

        public Track GetReadyTrack()
        {
            Track bestTrack = null;
            foreach (var t in _tracks)
            {
                if (t.State == State.Ready && (bestTrack == null || t.AdInfo.Revenue > bestTrack.AdInfo.Revenue))
                {
                    bestTrack = t;
                }
            }
            return bestTrack;
        }
        
        private void OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            
            _ui.SetStatus($"Load failed {adUnitId} with: {errorInfo}");

            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    _ = WaitDelay(track, NeftaAdapterEvents.GetRetryDelayInSeconds(track.Insight, track.AdUnitId));
                    if (track.IsOptimized)
                    {
                        _ = ContinueOptimized();
                    }
                    return;
                }
            }
        }

        private async Task ContinueOptimized()
        {
            _isFirstOptimizedResponseReceived = true;
            if (_tracks[0].State == State.Idle)
            {
                Load(_tracks[0]);
            }
            
            _isGlobalCooldown++;
            await Task.Delay((int)(NeftaAdapterEvents.GlobalTrackCooldown * 1000));
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            _isGlobalCooldown--;
            if (_isGlobalCooldown <= 0)
            {
                _isGlobalCooldown = 0;
                ContinueLoading();   
            }
        }

        public void ContinueLoading()
        {
            if (!_ui.IsAutoLoad)
            {
                return;
            }
            
            if (_tracks[0].State == State.Idle)
            {
                Load(_tracks[0]);
            }
            if (_isGlobalCooldown > 0)
            {
                return;
            }
            for (var i = 1; i < _tracks.Length; i++)
            {
                if (_tracks[i].State == State.Loading)
                {
                    return;
                }
            }
            for (var i = 1; i < _tracks.Length; i++)
            {
                if (_tracks[i].State == State.Idle)
                {
                    Load(_tracks[i]);
                    return;
                }
            }
        }

        public void UpdateAdAvailability(bool isAvailable)
        {
            _ui.SetAvailability(isAvailable);
        }
        
        private async Task WaitDelay(Track track, float delay)
        {
            track.State = State.Delay;
            await Task.Delay((int)(delay * 1000));
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            track.State = State.Idle;
            ContinueLoading();
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);
            
            _ui.SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");

            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    track.Insight = null;
                    track.AdInfo = adInfo;
                    track.State = State.Ready;
                    
                    _ui.SetAvailability(true);
                    if (NeftaAdapterEvents.IsSwapInterToRewarded && RewardedLogic != null)
                    {
                        RewardedLogic.OnInterstitialAvailabilityChanged(true);
                    }

                    if (track.IsOptimized)
                    {
                        _ = ContinueOptimized();
                    }
                    return;
                }
            }
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdDisplayFailedEvent");
            
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    track.State = State.Idle;
                    track.IsInterstitialAsRewarded = false;
                    ContinueLoading();
                    return;
                }
            }
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdHideEvent");
            
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    track.State = State.Idle;
                    
                    if (track.IsInterstitialAsRewarded)
                    {
                        if (RewardedLogic != null)
                        {
                            RewardedLogic.OnAdReceivedRewardEvent(adUnitId, new MaxSdkBase.Reward()
                            {
                                Amount = 1,
                                Label = "Interstitial reward"
                            }, adInfo);
                        }
                        track.IsInterstitialAsRewarded = false;
                    }
                    
                    ContinueLoading();
                    return;
                }
            }
        }
        
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var interAsRewarded = false;
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    interAsRewarded = track.IsInterstitialAsRewarded;
                    break;
                }
            }
            NeftaAdapterEvents.OnExternalMediationImpression(adUnitId, adInfo, interAsRewarded);
            
            Debug.Log($"OnAdRevenuePaidEvent: {adInfo.Revenue}");
        }


        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var interAsRewarded = false;
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    interAsRewarded = track.IsInterstitialAsRewarded;
                    break;
                }
            }
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo, interAsRewarded);
            
            Debug.Log("OnAdClickedEvent");
        }
        
        private async Task ForceStartSecondTrack(float stopWaitingForFirstResponseAfter)
        {
            await Task.Delay((int)(stopWaitingForFirstResponseAfter * 1000f));
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (!_isFirstOptimizedResponseReceived)
            {
                _isFirstOptimizedResponseReceived = true;
                if (_tracks.Length > 2 && _tracks[2].State == State.Idle)
                {
                    Load(_tracks[2]);
                }
                if (NeftaAdapterEvents.BackfillInterstitialDelay < 0 && _tracks[0].State == State.Idle)
                {
                    Load(_tracks[0]);
                }
            }
        }
    }
}