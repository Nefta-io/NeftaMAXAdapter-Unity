using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace NeftaCustomAdapter
{
    public abstract class AdLogic
    {
        protected enum State
        {
            Idle,
            Loading,
            Ready,
            Shown,
            Delay
        }
                
        protected class Track
        {
            public readonly string AdUnitId;
            public readonly bool IsOptimized;
            public State State;
            public AdInsight Insight;
            public MaxSdkBase.AdInfo AdInfo;
            public bool IsAdLoadCallbackAvailable;

            public Track(string adUnitId, bool isOptimized)
            {
                AdUnitId = adUnitId;
                IsOptimized = isOptimized;
            }
        }
        
        protected abstract string LogTag { get; }
        protected abstract NeftaAdapterEvents.AdType AdType { get; }
        protected abstract int InsightType { get; }
        protected abstract void LoadAsOptimized(string adUnitId, string bidFloor);
        protected abstract void LoadAsDefault(string adUnitId);
        protected abstract bool TryShow(Track adRequest, string placement, string customData);
        
        protected Track[] _tracks;
        protected bool _isFirstRequest = true;
        protected bool _isFirstOptimizedResponseReceived;
        protected int _isGlobalCooldown;
        protected bool _isAdRequested;
        internal MaxSdkBase.AdInfo _adInfo;
        
        public Action<string> OnAdRequestedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent;
        public Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent;
        public Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent;
        public Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent;
        
        public bool IsOptimized { get; protected set; }

        protected void InitializeDualTrack(string defaultAdUnit, string optimizedAdUnitA, string optimizedAdUnitB)
        {
            if (optimizedAdUnitB == null)
            {
                _tracks = new []{ new Track(defaultAdUnit, false), new Track(optimizedAdUnitA, true)};
            }
            else
            {
                _tracks = new []{ new Track(defaultAdUnit, false), new Track(optimizedAdUnitA, true),  new Track(optimizedAdUnitB, true)};
            }

            IsOptimized = true;
        }

        /// <summary>
        /// When using wrapper OnAdLoadEvent callback is forwarded only after Load request.
        /// Since Wrapper might already have an ad ready because of dual track loading.
        /// </summary>
        public void LoadOrTriggerAlreadyLoaded()
        {
            if (_isFirstRequest)
            {
                _isAdRequested = true;
                
                Load(_tracks[1]);
            
                var stopWaitingForFirstResponseAfter = AdType == NeftaAdapterEvents.AdType.Interstitial ? NeftaAdapterEvents.FirstResponseTimeoutInterstitial : NeftaAdapterEvents.FirstResponseTimeoutRewarded;
                if (_tracks.Length > 2 && stopWaitingForFirstResponseAfter > 0)
                {
                    _ = ForceStartSecondTrack(stopWaitingForFirstResponseAfter);
                }
 
                var backFillTrackDelay = NeftaAdapterEvents.BackFillTrackDelay;
                if (backFillTrackDelay == 0)
                {
                    Load(_tracks[0]);
                }
                else if (backFillTrackDelay > 0)
                {
                    _= WaitDelay(_tracks[0], backFillTrackDelay);
                }
                
                _isFirstRequest = false;
                return;
            }
            
            Track trackWithLoadCallbackAvailable = null;
            foreach (var track in _tracks)
            {
                if (track.State == State.Ready && track.IsAdLoadCallbackAvailable)
                {
                    if (trackWithLoadCallbackAvailable == null || track.AdInfo.Revenue > trackWithLoadCallbackAvailable.AdInfo.Revenue)
                    {
                        trackWithLoadCallbackAvailable = track;
                    }
                }
            }

            if (trackWithLoadCallbackAvailable != null)
            {
                trackWithLoadCallbackAvailable.IsAdLoadCallbackAvailable = false;
                OnAdLoadedEvent(trackWithLoadCallbackAvailable.AdUnitId, trackWithLoadCallbackAvailable.AdInfo);
            }
            else
            {
                _isAdRequested = true;
            }
        }
        
        protected void Load(Track track)
        {
            track.State = State.Loading;

            if (track.IsOptimized)
            {
                NeftaAdapterEvents.GetInsights(InsightType, track.Insight, (Insights insights) =>
                {
                    Log($"Load with Insights: {insights}");
                    if (insights.Insight != null)
                    {
                        track.Insight = insights.Insight;
                        NeftaAdapterEvents.OnExternalMediationRequest(AdType, track.AdUnitId, track.Insight);
                        var bidFloor = "";
                        if (track.Insight._floorPrice >= 0)
                        {
                            bidFloor = track.Insight._floorPrice.ToString(CultureInfo.InvariantCulture);
                        }
                        Log($"Loading {track.AdUnitId} as Optimized with floor: {bidFloor}");
                        
                        LoadAsOptimized(track.AdUnitId, bidFloor);
                        OnAdRequestedEvent?.Invoke(track.AdUnitId);
                    }
                    else
                    {
                        _ = RetryOptimizedWithDelay(track);
                    }
                });
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequest(AdType, track.AdUnitId);
                
                Log($"Loading {track.AdUnitId} as Default");
                LoadAsDefault(track.AdUnitId);
            
                OnAdRequestedEvent?.Invoke(track.AdUnitId);
            }
        }
        
        public bool IsAdReady()
        {
            foreach (var track in _tracks)
            {
                if (track.State == State.Ready)
                {
                    return true;
                }
            }
            return false;
        }

        public MaxSdkBase.AdInfo GetAdReady()
        {
            if (!IsOptimized)
            {
                return _adInfo;
            }
            return GetReadyTrack()?.AdInfo;
        }
        
        public void ShowAd(string placement, string customData)
        {
            var isShown = false;
            var readyTrack = GetReadyTrack();
            if (readyTrack != null)
            {
                isShown = TryShow(readyTrack, placement, customData);
            }
            if (!isShown)
            {
                ContinueLoading();
            }
        }
        
        protected void OnAdFailedCallback(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            if (!IsOptimized)
            {
                if (OnAdLoadFailedEvent != null)
                {
                    OnAdLoadFailedEvent(adUnitId, errorInfo);
                }
                return;
            }
            
            Log($"Load Failed {adUnitId}: {errorInfo}");

            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
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

        private void ContinueLoading()
        {
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
        
        protected void OnAdLoadedCallback(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);
            if (!IsOptimized)
            {
                _adInfo = adInfo;
                if (OnAdLoadedEvent != null)
                {
                    OnAdLoadedEvent(adUnitId, adInfo);
                }
                return;
            }
            
            Log($"Loaded {adUnitId} at: {adInfo.Revenue}");

            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    track.Insight = null;
                    track.AdInfo = adInfo;
                    track.State = State.Ready;
            
                    if (_isAdRequested)
                    {
                        _isAdRequested = false;
                
                        if (OnAdLoadedEvent != null)
                        {
                            OnAdLoadedEvent(adUnitId, adInfo);   
                        }
                    }
                    else
                    {
                        track.IsAdLoadCallbackAvailable = true;
                    }

                    if (track.IsOptimized)
                    {
                        _ = ContinueOptimized();
                    }
                    return;
                }
            }
        }
        
        protected void OnAdDisplayFailedCallback(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (!IsOptimized)
            {
                _adInfo = null;
                if (OnAdDisplayFailedEvent != null)
                {
                    OnAdDisplayFailedEvent(adUnitId, errorInfo, adInfo);
                }
                return;
            }

            Log($"OnAdDisplayFailedEvent {adUnitId}");
            
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    track.State = State.Idle;
            
                    if (OnAdDisplayFailedEvent != null)
                    {
                        OnAdDisplayFailedEvent(adUnitId, errorInfo, adInfo);
                    }

                    ContinueLoading();
                    return;
                }
            }
        }
        
        protected void OnAdDisplayedCallback(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _adInfo = null;
            if (OnAdDisplayedEvent != null)
            {
                OnAdDisplayedEvent(adUnitId, adInfo);
            }
        }
        
        protected void OnAdRevenuePaidCallback(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationImpression(adUnitId, adInfo);
            if (OnAdRevenuePaidEvent != null)
            {
                OnAdRevenuePaidEvent(adUnitId, adInfo);
            }
        }
        
        protected void OnAdClickedCallback(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo);
            if (OnAdClickedEvent != null)
            {
                OnAdClickedEvent(adUnitId, adInfo);
            }
        }
        
        protected void OnAdHiddenCallback(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (!IsOptimized)
            {
                if (OnAdHiddenEvent != null)
                {
                    OnAdHiddenEvent(adUnitId, adInfo);
                }
                return;
            }

            Log($"OnAdHiddenEvent {adUnitId}");
            
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    track.State = State.Idle;
            
                    if (OnAdHiddenEvent != null)
                    {
                        OnAdHiddenEvent(adUnitId, adInfo);
                    }

                    ContinueLoading();
                    return;
                }
            }
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
        
        protected Track GetReadyTrack()
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
        
        private async Task ForceStartSecondTrack(float stopWaitingForFirstResponseAfter)
        {
            await Task.Delay((int)(stopWaitingForFirstResponseAfter * 1000));
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
                if (NeftaAdapterEvents.BackFillTrackDelay < 0 && _tracks[0].State == State.Idle)
                {
                    Load(_tracks[0]);
                }
            }
        }
        
        protected void Log(string log)
        {
            if (NeftaAdapterEvents.IsLoggingEnabled)
            {
                Debug.Log($"{LogTag}: {log}");
            }
        }
    }
}