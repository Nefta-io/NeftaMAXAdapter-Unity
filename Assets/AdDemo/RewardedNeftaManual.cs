using System.Globalization;
using System.Threading.Tasks;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class RewardedNeftaManual : IRewarded
    {
        private enum State
        {
            Idle,
            Loading,
            Ready,
            Shown,
            Delay
        }
                
        private class Track
        {
            public readonly string AdUnitId;
            public readonly bool IsOptimized;
            public State State;
            public AdInsight Insight;
            public MaxSdkBase.AdInfo AdInfo;

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

        private RewardedUi _ui;
        
        public void Init(RewardedUi ui)
        {
            _ui = ui;

            _tracks = new Track[]
            {
                new Track(RewardedUi.DefaultAdUnitId, false),
                new Track(RewardedUi.OptimizedAdUnitIdA, true),
                new Track(RewardedUi.OptimizedAdUnitIdB, true)
            };
            
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnAdClickedEvent;
        }
        
        public void Load()
        {
            if (_isFirstRequest)
            {
                Load(_tracks[1]);
                
                var stopWaitingForFirstResponseAfter = NeftaAdapterEvents.FirstResponseTimeoutRewarded;
                if (stopWaitingForFirstResponseAfter > 0)
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

                NeftaAdapterEvents.GetInsights(Insights.Rewarded, track.Insight, (Insights insights) =>
                {
                    _ui.SetStatus($"Load with Insights: {insights}");
                    if (insights.Insight != null)
                    {
                        track.Insight = insights.Insight;
                        MaxSdk.SetRewardedAdExtraParameter(track.AdUnitId, "disable_auto_retries", "true");
                        var bidFloor = "";
                        if (track.Insight._floorPrice >= 0)
                        {
                            bidFloor = track.Insight._floorPrice.ToString(CultureInfo.InvariantCulture);
                        }

                        MaxSdk.SetRewardedAdExtraParameter(track.AdUnitId, "jC7Fp", bidFloor);


                        NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, track.AdUnitId, track.Insight);

                        _ui.SetStatus($"Loading {track.AdUnitId} as Optimized with floor: {bidFloor}");
                        MaxSdk.LoadRewardedAd(track.AdUnitId);
                    }
                    else
                    {
                        _ = RetryOptimizedWithDelay(track);
                    }
                });
            }
            else
            {
                NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, track.AdUnitId);
            
                _ui.SetStatus($"Loading {track.AdUnitId} as Default");
                MaxSdk.LoadRewardedAd(track.AdUnitId);
            }
        }
        
        public void Show()
        {
            var isShown = false;
            var readyTrack = GetReadyTrack();
            if (readyTrack != null)
            {
                isShown = TryShow(readyTrack);
            }
            if (!isShown)
            {
                ContinueLoading();
            }
            readyTrack = GetReadyTrack();
            _ui.SetAvailability(readyTrack != null);
        }
        
        private bool TryShow(Track track)
        {
            track.AdInfo = null;
            if (MaxSdk.IsRewardedAdReady(track.AdUnitId))
            {
                track.State = State.Shown;
                _ui.SetStatus($"Showing {track.AdUnitId}");
                MaxSdk.ShowRewardedAd(track.AdUnitId);
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

        private Track GetReadyTrack()
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

        private void ContinueLoading()
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
                    ContinueLoading();
                    return;
                }
            }
        }
        
        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdReceivedRewardEvent");
        }
        
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationImpression(adUnitId, adInfo);
            
            Debug.Log($"OnAdRevenuePaidEvent: {adInfo.Revenue}");
        }


        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationClick(adUnitId, adInfo);
            
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
                if (NeftaAdapterEvents.BackFillTrackDelay < 0 && _tracks[0].State == State.Idle)
                {
                    Load(_tracks[0]);
                }
            }
        }
    }
}