using System.Collections.Generic;
using NeftaCustomAdapter;

namespace Editor.Tests.PlayTests
{
    /// <summary>
    /// Rewarded scheduler with MAX replaced by a recorded load. Show and close are driven by the test.
    /// </summary>
    public class RewardedTestLogic : RewardedLogic
    {
        protected override string LogTag => "TestRewarded";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Rewarded;
        protected override int InsightType => Insights.Rewarded;

        public readonly List<LoadRequest> LoadRequests = new List<LoadRequest>();
        public string LastShownAdUnit;

        private bool _started;

        public void Begin(string defaultAdUnit, string optimizedAdUnitA, string optimizedAdUnitB)
        {
            LoadRequests.Clear();
            LastShownAdUnit = null;
            base.Initialize(true, defaultAdUnit, optimizedAdUnitA, optimizedAdUnitB);
            _started = true;
        }

        public void Release()
        {
            if (!_started)
            {
                return;
            }

            _started = false;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnAdLoadedCallback;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnAdFailedCallback;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnAdDisplayFailedCallback;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnAdDisplayedCallback;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnAdClickedCallback;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnAdHiddenCallback;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnAdReceivedRewardCallback;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdRevenuePaidCallback;
        }

        protected override void LoadAsOptimized(string adUnitId, string bidFloor)
        {
            LoadRequests.Add(new LoadRequest(adUnitId, true, bidFloor));
        }

        protected override void LoadAsDefault(string adUnitId)
        {
            LoadRequests.Add(new LoadRequest(adUnitId, false, null));
        }

        protected override bool TryShow(Track track, string placement, string customData)
        {
            var hadAd = track.AdInfo != null;
            track.AdInfo = null;
            if (!hadAd)
            {
                track.State = State.Idle;
                return false;
            }

            LastShownAdUnit = track.AdUnitId;
            track.State = State.Shown;
            return true;
        }

        public string TrackState(string adUnitId)
        {
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    return track.State.ToString();
                }
            }

            return "missing";
        }

        public void InvokeLoad(string adUnitId, double revenue)
        {
            OnAdLoadedCallback(adUnitId, AdDemo.SimulatorUi.GetAdInfo(adUnitId, "REWARDED", revenue));
        }

        public void CloseShown(string adUnitId)
        {
            OnAdHiddenCallback(adUnitId, AdDemo.SimulatorUi.GetAdInfo(adUnitId, "REWARDED", 0));
        }
    }
}
