using System.Collections.Generic;
using NeftaCustomAdapter;

namespace Editor.Tests.PlayTests
{
    public class LoadRequest
    {
        public readonly string AdUnitId;
        public readonly bool DisableAutoRetries;
        public readonly string BidFloor;

        public LoadRequest(string adUnitId, bool disableAutoRetries, string bidFloor)
        {
            AdUnitId = adUnitId;
            DisableAutoRetries = disableAutoRetries;
            BidFloor = bidFloor;
        }
    }

    /// <summary>
    /// Interstitial scheduler with MAX replaced by a recorded load. Show and close are driven by the test.
    /// </summary>
    public class InterstitialTestLogic : InterstitialLogic
    {
        protected override string LogTag => "TestInterstitial";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Interstitial;
        protected override int InsightType => Insights.Interstitial;

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
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnAdLoadedCallback;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnAdFailedCallback;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnAdDisplayFailedCallback;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnAdDisplayedCallback;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnAdClickedCallback;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnAdHiddenCallback;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaidCallback;
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

        public bool HasInsight(string adUnitId)
        {
            foreach (var track in _tracks)
            {
                if (track.AdUnitId == adUnitId)
                {
                    return track.Insight != null;
                }
            }

            return false;
        }

        public void InvokeLoad(string adUnitId, double revenue)
        {
            OnAdLoadedCallback(adUnitId, AdDemo.SimulatorUi.GetAdInfo(adUnitId, "INTER", revenue));
        }

        public void InvokeNoFill(string adUnitId)
        {
            OnAdFailedCallback(adUnitId, new MaxSdkBase.ErrorInfo(new Dictionary<string, object>
            {
                { "errorCode", 204 },
                { "errorMessage", "no fill" },
                { "waterfallInfo", AdDemo.SimulatorUi.GetWaterfallDictionary(new[] { MaxSdkBase.MaxAdLoadState.FailedToLoad }) }
            }));
        }

        public void CloseShown(string adUnitId)
        {
            OnAdHiddenCallback(adUnitId, AdDemo.SimulatorUi.GetAdInfo(adUnitId, "INTER", 0));
        }
    }
}
