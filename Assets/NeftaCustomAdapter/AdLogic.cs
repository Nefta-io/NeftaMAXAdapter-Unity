using System;
using System.Collections.Generic;
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
            LoadingWithInsights,
            Loading,
            Ready,
            Shown
        }
                
        protected class Track
        {
            public readonly string AdUnitId;
            public State State;
            public AdInsight Insight;
            public MaxSdkBase.AdInfo AdInfo;
            public bool IsAdLoadCallbackAvailable;

            public Track(string adUnitId)
            {
                AdUnitId = adUnitId;
            }
        }
        
        protected abstract string LogTag { get; }
        protected abstract NeftaAdapterEvents.AdType AdType { get; }
        protected abstract int InsightType { get; }
        protected abstract void LoadInternal(string adUnitId, bool disableAutoRetries, string bidFloor);
        protected abstract bool TryShow(Track track);
        
        protected Track _trackA;
        protected Track _trackB;
        protected bool _isFirstResponseReceived;
        protected bool _isAdRequested;
        
        public Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent;
        public Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent;
        public Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent;
        public Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent;
        public Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent;
        
        public bool IsOptimized { get; protected set; }

        protected void InitializeDualTrack(string adUnitIdA, string adUnitIdB)
        {
            _trackA = new Track(adUnitIdA);
            _trackB = new Track(adUnitIdB);

            IsOptimized = true;
        }

        /// <summary>
        /// When using wrapper OnAdLoadEvent callback is forwarded only after Load request.
        /// Since Wrapper might already have an ad ready because of dual track loading.
        /// </summary>
        public void LoadOrTriggerAlreadyLoaded()
        {
            if (_trackA.State == State.Ready && _trackA.IsAdLoadCallbackAvailable)
            {
                _trackA.IsAdLoadCallbackAvailable = false;
                if (OnAdLoadedEvent != null)
                {
                    OnAdLoadedEvent(_trackA.AdUnitId, _trackA.AdInfo);
                    return;
                }
            }
            if (_trackB.State == State.Ready && _trackB.IsAdLoadCallbackAvailable)
            {
                _trackB.IsAdLoadCallbackAvailable = false;
                if (OnAdLoadedEvent != null)
                {
                    OnAdLoadedEvent(_trackB.AdUnitId, _trackB.AdInfo);
                    return;
                }
            }
            
            _isAdRequested = true;
            LoadTracks();
        }
        
        protected void LoadTracks()
        {
            TrackLoad(_trackA, _trackB.State);
            TrackLoad(_trackB, _trackA.State);
        }
        
        private void TrackLoad(Track track, State otherState)
        {
            if (track.State == State.Idle)
            {
                if (otherState == State.LoadingWithInsights || otherState == State.Shown)
                {
                    if (_isFirstResponseReceived)
                    {
                        LoadDefault(track);
                    }
                }
                else
                {
                    GetInsightsAndLoad(track); 
                }
            }
        }
        
        protected void GetInsightsAndLoad(Track track)
        {
            track.State = State.LoadingWithInsights;
            
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
                    
                    LoadInternal(track.AdUnitId, true, bidFloor);

                    if (NeftaAdapterEvents.NoDynamicResponseRetryInMs > 0)
                    {
                        DelayExecutor.ExecuteAfterDelay(track, RetryOnNoResponse, NeftaAdapterEvents.NoDynamicResponseRetryInMs * 0.001f);
                    }
                }
                else
                {
                    RestartAfterFailedLoad(track);
                }
            });
        }
        
        protected void LoadDefault(Track track)
        {
            track.State = State.Loading;
            NeftaAdapterEvents.OnExternalMediationRequest(AdType, track.AdUnitId);
            Log($"Loading {track.AdUnitId} as Default");
            
            LoadInternal(track.AdUnitId, false, "");
            
            if (NeftaAdapterEvents.NoDefaultResponseRetryInMs > 0)
            {
                DelayExecutor.ExecuteAfterDelay(track, RetryOnNoResponse, NeftaAdapterEvents.NoDefaultResponseRetryInMs * 0.001f);
            }
        }
        
        private void RetryOnNoResponse(Track track)
        {
            Log($"Retrying load on {track.AdUnitId}");
            track.State = State.Idle;
            LoadTracks();
        }
        
        private void RestartAfterFailedLoad(Track track)
        {
            _ = RetryLoadWithDelay(track);
            
            _isFirstResponseReceived = true;
            LoadTracks();
        }
        
        private async Task RetryLoadWithDelay(Track track)
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
            LoadTracks();
        }
        
        public bool IsAdReady()
        {
            return _trackA.State == State.Ready || _trackB.State == State.Ready;
        }
        
        public void ShowAd()
        {
            var isShown = false;
            if (_trackA.State == State.Ready)
            {
                if (_trackB.State == State.Ready && _trackB.AdInfo.Revenue > _trackA.AdInfo.Revenue)
                {
                    isShown = TryShow(_trackB);
                }
                if (!isShown)
                {
                    isShown = TryShow(_trackA);
                }
            }
            if (!isShown && _trackB.State == State.Ready)
            {
                if (!TryShow(_trackB))
                {
                    LoadTracks();
                }
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
            
            var track = adUnitId == _trackA.AdUnitId ? _trackA : _trackB;
            DelayExecutor.CancelDelayedAction(track);
            RestartAfterFailedLoad(track);
        }
        
        protected void OnAdLoadedCallback(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);
            if (!IsOptimized)
            {
                if (OnAdLoadedEvent != null)
                {
                    OnAdLoadedEvent(adUnitId, adInfo);
                }
                return;
            }
            
            Log($"Loaded {adUnitId} at: {adInfo.Revenue}");
            
            var track = adUnitId == _trackA.AdUnitId ? _trackA : _trackB;
            DelayExecutor.CancelDelayedAction(track);
            track.Insight = null;
            track.AdInfo = adInfo;
            track.State = State.Ready;
            _isFirstResponseReceived = true;
            
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
            
            LoadTracks();
        }
        
        protected void OnAdDisplayFailedCallback(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (!IsOptimized)
            {
                if (OnAdDisplayFailedEvent != null)
                {
                    OnAdDisplayFailedEvent(adUnitId, errorInfo, adInfo);
                }
                return;
            }

            Log($"OnAdDisplayFailedEvent {adUnitId}");
            
            var track = adUnitId == _trackA.AdUnitId ? _trackA : _trackB;
            track.State = State.Idle;
            
            if (OnAdDisplayFailedEvent != null)
            {
                OnAdDisplayFailedEvent(adUnitId, errorInfo, adInfo);
            }
            
            LoadTracks();
        }
        
        protected void OnAdDisplayedCallback(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
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
            
            var track = adUnitId == _trackA.AdUnitId ? _trackA : _trackB;
            track.State = State.Idle;
            
            if (OnAdHiddenEvent != null)
            {
                OnAdHiddenEvent(adUnitId, adInfo);
            }
            
            LoadTracks();
        }
        
        protected void Log(string log)
        {
            if (NeftaAdapterEvents.IsLoggingEnabled)
            {
                Debug.Log($"{LogTag}: {log}");
            }
        }
        
        protected class DelayExecutor : MonoBehaviour
        {
            private class DelayedAction
            {
                public Track _track;
                public Action<Track> _action;
                public float _delay;

                public DelayedAction(Track track, Action<Track> action, float delay)
                {
                    _track = track;
                    _action = action;
                    _delay = delay;
                }
            }

            private static DelayExecutor _instance;
            private readonly List<DelayedAction> _actions = new List<DelayedAction>();

            private void Update()
            {
                for (var i = _actions.Count - 1; i >= 0; i--)
                {
                    var delayedAction = _actions[i];
                    delayedAction._delay -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                    if (delayedAction._delay <= 0f)
                    {
                        _actions.RemoveAt(i);
                        delayedAction._action(delayedAction._track);
                    }
                }
            }

            public static void ExecuteAfterDelay(Track track, Action<Track> action, float delay)
            {
                if (_instance == null)
                {
                    var executor = new GameObject("NeftaSdkExecutor")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    DontDestroyOnLoad(executor);
                    _instance = executor.AddComponent<DelayExecutor>();
                }
                _instance._actions.Add(new DelayedAction(track, action, delay));
            }

            public static void CancelDelayedAction(Track track)
            {
                if (_instance != null)
                {
                    for (var i = _instance._actions.Count - 1; i >= 0; i--)
                    {
                        if (_instance._actions[i]._track == track)
                        {
                            _instance._actions.RemoveAt(i);
                            return;
                        }
                    }
                }
            }
        }
    }
}