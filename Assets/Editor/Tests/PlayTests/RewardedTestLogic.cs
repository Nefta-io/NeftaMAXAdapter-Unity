using System.Collections.Generic;
using AdDemo;
using NeftaCustomAdapter;

namespace Editor.Tests.PlayTests
{
    public class RewardedTestLogic : RewardedLogic
    {
        public class LoadRequest
        {
            public string AdUnitId;
            public bool DisableAutoRetries;
            public string BidFloor;

            public LoadRequest(string adUnitId, bool disableAutoRetries, string bidFloor)
            {
                AdUnitId = adUnitId;
                DisableAutoRetries = disableAutoRetries;
                BidFloor = bidFloor;
            }
        }
        
        protected override string LogTag => "NeftaRewarded";
        protected override NeftaAdapterEvents.AdType AdType => NeftaAdapterEvents.AdType.Rewarded;
        protected override int InsightType => Insights.Rewarded;
        
        public List<LoadRequest> LoadRequests;

        public void Initialize(bool isOptimized, string adUnitIdA, string adUnitIdB)
        {
            _trackA = new Track(adUnitIdA);
            _trackB = new Track(adUnitIdB);
            NeftaSdk.Initialize();

            IsOptimized = true;

            LoadRequests = new List<LoadRequest>();
        }

        protected override void LoadInternal(string adUnitId, bool disableAutoRetries, string bidFloor)
        {
            LoadRequests.Add(new LoadRequest(adUnitId, disableAutoRetries, bidFloor));
        }
        
        protected override bool TryShow(Track track)
        {
            track.State = State.Idle;
            if (track.AdInfo != null)
            {
                track.AdInfo = null;
                return true;
            }
            return false;
        }
        
        public void InvokeLoad(string adUnitId, double revenue)
        {
            OnAdLoadedCallback(adUnitId, SimulatorUi.GetAdInfo(adUnitId, "REWARDED", revenue));
        }
    }
}