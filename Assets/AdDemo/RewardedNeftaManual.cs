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
            LoadingWithInsights,
            Loading,
            Ready,
            Shown
        }
                
        private class Track
        {
            public readonly string AdUnitId;
            public State State;
            public int LoadingTimeInMs;
            public AdInsight Insight;
            public MaxSdkBase.AdInfo AdInfo;

            public Track(string adUnitId)
            {
                AdUnitId = adUnitId;
            }
        }
        
        private Track _trackA;
        private Track _trackB;
        private bool _isFirstResponseReceived;

        private RewardedUi _ui;
        
        public void Init(RewardedUi ui)
        {
            _ui = ui;
            
            _trackA = new Track(RewardedUi.AdUnitIdA);
            _trackB = new Track(RewardedUi.AdUnitIdB);
            
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
            if (!_ui.IsAutoLoad)
            {
                return;
            }
            
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
        
        private void GetInsightsAndLoad(Track track)
        {
            track.State = State.LoadingWithInsights;
            track.LoadingTimeInMs = 0;
            
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
                    RestartAfterFailedLoad(track);
                }
            });
        }
        
        private void LoadDefault(Track track)
        {
            track.State = State.Loading;
            track.LoadingTimeInMs = 0;
            
            MaxSdk.SetRewardedAdExtraParameter(track.AdUnitId, "disable_auto_retries", "false");
            MaxSdk.SetRewardedAdExtraParameter(track.AdUnitId, "jC7Fp", "");
            
            NeftaAdapterEvents.OnExternalMediationRequest(NeftaAdapterEvents.AdType.Rewarded, track.AdUnitId);
            
            _ui.SetStatus($"Loading {track.AdUnitId} as Default");
            MaxSdk.LoadRewardedAd(track.AdUnitId);
        }
        
        public void Show()
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
                    Load();
                }
            }
            _ui.SetAvailability(_trackA.State == State.Ready || _trackB.State == State.Ready);
        }
        
        public void OnUpdate(float delta)
        {
            if (_trackA.State == State.Loading || _trackB.State == State.LoadingWithInsights)
            {
                _trackA.LoadingTimeInMs += (int)(delta * 1000);
                if ((_trackA.State == State.Loading && NeftaAdapterEvents.NoDefaultResponseRetryInMs > 0 &&
                     _trackA.LoadingTimeInMs >= NeftaAdapterEvents.NoDefaultResponseRetryInMs) ||
                    (_trackA.State == State.LoadingWithInsights && NeftaAdapterEvents.NoDynamicResponseRetryInMs > 0 &&
                     _trackA.LoadingTimeInMs >= NeftaAdapterEvents.NoDynamicResponseRetryInMs))
                {
                    _ui.SetStatus($"Retrying load after no response on {_trackA.AdUnitId}");
                    _trackA.State = State.Idle;
                    Load();
                }
            }
            if (_trackB.State == State.Loading || _trackB.State == State.LoadingWithInsights)
            {
                _trackB.LoadingTimeInMs += (int)(delta * 1000);
                if ((_trackB.State == State.Loading && NeftaAdapterEvents.NoDefaultResponseRetryInMs > 0 &&
                     _trackB.LoadingTimeInMs >= NeftaAdapterEvents.NoDefaultResponseRetryInMs) ||
                    (_trackB.State == State.LoadingWithInsights && NeftaAdapterEvents.NoDynamicResponseRetryInMs > 0 &&
                     _trackA.LoadingTimeInMs >= NeftaAdapterEvents.NoDynamicResponseRetryInMs))
                {
                    _ui.SetStatus($"Retrying load after no response on {_trackB.AdUnitId}");
                    _trackB.State = State.Idle;
                    Load();
                }
            }
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
        
        private void RestartAfterFailedLoad(Track track)
        {
            _ = RetryLoadWithDelay(track);
            
            _isFirstResponseReceived = true;
            Load();
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
            Load();
        }
        
        private void OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestFailed(adUnitId, errorInfo);
            
            _ui.SetStatus($"Load failed {adUnitId} with: {errorInfo}");
            
            var track = adUnitId == _trackA.AdUnitId ? _trackA : _trackB;
            RestartAfterFailedLoad(track);
        }
        
        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            NeftaAdapterEvents.OnExternalMediationRequestLoaded(adInfo);
            
            _ui.SetStatus($"Loaded {adUnitId} at: {adInfo.Revenue}");
            
            var track = adUnitId == _trackA.AdUnitId ? _trackA : _trackB;
            track.Insight = null;
            track.AdInfo = adInfo;
            track.State = State.Ready;
            _isFirstResponseReceived = true;
            
            _ui.SetAvailability(true);
            
            Load();
        }
        
        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdDisplayFailedEvent");
            
            Load();
        }
        
        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _ui.SetStatus("OnAdHideEvent");

            Load();
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
    }
}