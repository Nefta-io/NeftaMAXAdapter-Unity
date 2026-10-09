using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using AdDemo;
using NeftaCustomAdapter;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Editor.Tests.PlayTests
{
    public class PlayTests
    {
        private const string InterstitialDefault = "interstitial-default";
        private const string InterstitialA = "interstitial-a";
        private const string InterstitialB = "interstitial-b";
        private const string RewardedDefault = "rewarded-default";
        private const string RewardedA = "rewarded-a";
        private const string RewardedB = "rewarded-b";

        private const string InitJson =
            "{\"nuid\":\"playtest\",\"isSessionOptimized\":true,\"delays\":[0.15]," +
            "\"firstResponseTimeoutInterstitialInMs\":0,\"firstResponseTimeoutRewardedInMs\":0," +
            "\"globalTrackCooldownInMs\":50,\"backfillTrackDelayInMs\":-1000}";

        private InterstitialTestLogic _interstitial;
        private RewardedTestLogic _rewarded;
        private readonly List<string> _interstitialLoaded = new List<string>();
        private readonly List<string> _rewardedLoaded = new List<string>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EnsureNefta();
            ApplyScheduler(0f, 0.05f, -1f, 0.15f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_interstitial != null)
            {
                _interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
                _interstitial.Release();
                _interstitial = null;
            }

            if (_rewarded != null)
            {
                _rewarded.OnAdLoadedEvent -= OnRewardedLoaded;
                _rewarded.Release();
                _rewarded = null;
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator OptimizedRequestCarriesBidFloor()
        {
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, 5d);

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialA) == 1, 2f, "optimized A did not load");

            var load = Single(_interstitial.LoadRequests, InterstitialA);
            Assert.IsTrue(load.DisableAutoRetries, Dump(_interstitial.LoadRequests));
            Assert.AreEqual("5", load.BidFloor, Dump(_interstitial.LoadRequests));
            Assert.IsTrue(_interstitial.HasInsight(InterstitialA));
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialA), States());
            Assert.AreEqual("Idle", _interstitial.TrackState(InterstitialB), States());
            Assert.AreEqual("Idle", _interstitial.TrackState(InterstitialDefault), States());
            Assert.AreEqual(0, _rewarded.LoadRequests.Count, Dump(_rewarded.LoadRequests));
            Assert.IsFalse(NeftaSdk.IsInterstitialReady(InterstitialDefault));
            Assert.AreEqual(0, _interstitialLoaded.Count);
        }

        [UnityTest]
        public IEnumerator BackfillHasNoFloorAndAlternateTrackWaitsForCooldown()
        {
            ApplyScheduler(0f, 0.25f, -1f, 0.15f);
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, 5d, 7d);

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialA) == 1, 2f, "optimized A did not load");

            _interstitial.InvokeLoad(InterstitialA, 0.002);

            Assert.AreEqual("Ready", _interstitial.TrackState(InterstitialA), States());
            Assert.IsFalse(_interstitial.HasInsight(InterstitialA));
            Assert.AreEqual(1, _interstitialLoaded.Count);
            Assert.AreEqual(InterstitialA, _interstitialLoaded[0]);
            AssertOptimized(_interstitial.LoadRequests, InterstitialA, "5");
            AssertDefault(_interstitial.LoadRequests, InterstitialDefault);
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));

            yield return new WaitForSecondsRealtime(0.08f);
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), "B started before the global cooldown. " + Dump(_interstitial.LoadRequests));

            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialB) == 1, 2f, "optimized B did not start after cooldown");
            AssertOptimized(_interstitial.LoadRequests, InterstitialB, "7");
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialB), States());
            Assert.AreEqual(1, Count(_interstitial.LoadRequests, InterstitialDefault), Dump(_interstitial.LoadRequests));
        }

        [UnityTest]
        public IEnumerator ShowSelectsHighestRevenue()
        {
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, 5d, 7d);

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialA) == 1, 2f, "optimized A did not load");
            _interstitial.InvokeLoad(InterstitialA, 0.002);

            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialB) == 1, 2f, "optimized B did not load");
            _interstitial.InvokeLoad(InterstitialB, 0.003);
            _interstitial.InvokeLoad(InterstitialDefault, 0.008);

            Assert.AreEqual(1, _interstitialLoaded.Count, "extra fills must stay buffered until the next Load");
            Assert.IsTrue(NeftaSdk.IsInterstitialReady(InterstitialDefault));
            Assert.AreEqual(InterstitialDefault, NeftaSdk.GetInterstitialAdReady().AdUnitIdentifier);
            Assert.AreEqual(0.008d, NeftaSdk.GetInterstitialAdReady().Revenue, 0.0000001d);

            var loadsBeforeShow = _interstitial.LoadRequests.Count;
            NeftaSdk.ShowInterstitial(InterstitialDefault);
            Assert.AreEqual(InterstitialDefault, _interstitial.LastShownAdUnit);
            Assert.AreEqual("Shown", _interstitial.TrackState(InterstitialDefault), States());
            Assert.AreEqual(InterstitialB, NeftaSdk.GetInterstitialAdReady().AdUnitIdentifier);
            Assert.AreEqual(loadsBeforeShow, _interstitial.LoadRequests.Count);
            Assert.AreEqual(1, _interstitialLoaded.Count);

            _interstitial.CloseShown(InterstitialDefault);
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialDefault), States());
            Assert.AreEqual(2, Count(_interstitial.LoadRequests, InterstitialDefault), "backfill should reload once it is idle. " + Dump(_interstitial.LoadRequests));
            var reloaded = _interstitial.LoadRequests[_interstitial.LoadRequests.Count - 1];
            Assert.AreEqual(InterstitialDefault, reloaded.AdUnitId);
            Assert.IsFalse(reloaded.DisableAutoRetries);
            Assert.IsNull(reloaded.BidFloor);
            Assert.AreEqual(1, Count(_interstitial.LoadRequests, InterstitialA), Dump(_interstitial.LoadRequests));
            Assert.AreEqual(1, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            Assert.AreEqual(2, _interstitialLoaded.Count);
            Assert.AreEqual(InterstitialB, _interstitialLoaded[1]);
        }

        [UnityTest]
        public IEnumerator FirstResponseTimeoutOpensSecondOptimizedTrack()
        {
            ApplyScheduler(0.18f, 0.05f, -1f, 0.15f);
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, 5d, 9d);

            var started = Time.realtimeSinceStartup;
            NeftaSdk.LoadInterstitial(InterstitialDefault);
            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialA) == 1, 2f, "optimized A did not load");
            Assert.AreEqual(1, _interstitial.LoadRequests.Count, Dump(_interstitial.LoadRequests));

            yield return WaitUntil(() =>
                Count(_interstitial.LoadRequests, InterstitialB) == 1 &&
                Count(_interstitial.LoadRequests, InterstitialDefault) == 1, 2f, "timeout did not open B and the backfill");

            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, 0.1f);
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialA), States());
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialB), States());
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialDefault), States());
            AssertOptimized(_interstitial.LoadRequests, InterstitialA, "5");
            AssertOptimized(_interstitial.LoadRequests, InterstitialB, "9");
            AssertDefault(_interstitial.LoadRequests, InterstitialDefault);
        }

        [UnityTest]
        public IEnumerator FirstResponsePreventsTimeoutFromOpeningSecondTrack()
        {
            ApplyScheduler(0.2f, 1f, -1f, 0.15f);
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, 5d, 9d);

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialA) == 1, 2f, "optimized A did not load");
            _interstitial.InvokeLoad(InterstitialA, 0.004);

            AssertDefault(_interstitial.LoadRequests, InterstitialDefault);
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));

            yield return new WaitForSecondsRealtime(0.45f);
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), "B started without waiting out the cooldown. " + Dump(_interstitial.LoadRequests));
            Assert.AreEqual("Ready", _interstitial.TrackState(InterstitialA), States());
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialDefault), States());

            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialB) == 1, 2f, "B did not start after the global cooldown");
            AssertOptimized(_interstitial.LoadRequests, InterstitialB, "9");
        }

        [UnityTest]
        public IEnumerator MissingInsightDoesNotRequestAnAd()
        {
            ApplyScheduler(0f, 0.05f, -1f, 0.2f);
            CreateLogics();
            var nextId = PeekInsightId();
            QueueInsight(nextId + 1, Insights.Interstitial, 5d, 0f);

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            for (var frame = 0; frame < 3; frame++)
            {
                yield return null;
            }

            Assert.AreEqual(0, _interstitial.LoadRequests.Count, Dump(_interstitial.LoadRequests));
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialA), States());

            yield return WaitUntil(() => _interstitial.LoadRequests.Count >= 2, 2f, "retry did not load the backfill and optimized A");
            AssertDefault(_interstitial.LoadRequests, InterstitialDefault);
            AssertOptimized(_interstitial.LoadRequests, InterstitialA, "5");
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));
        }

        [UnityTest]
        public IEnumerator NoFillRestsTrackUntilTheOtherCanStart()
        {
            ApplyScheduler(0f, 0.15f, -1f, 0.15f);
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, (5d, 0.6f), (7d, 0f));

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialA) == 1, 2f, "optimized A did not load");

            _interstitial.InvokeNoFill(InterstitialA);
            Assert.AreEqual("Delay", _interstitial.TrackState(InterstitialA), States());
            AssertDefault(_interstitial.LoadRequests, InterstitialDefault);
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));

            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialB) == 1, 2f, "optimized B did not start while A rests");
            Assert.AreEqual("Delay", _interstitial.TrackState(InterstitialA), States());
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialB), States());
            AssertOptimized(_interstitial.LoadRequests, InterstitialB, "7");
            Assert.AreEqual(1, Count(_interstitial.LoadRequests, InterstitialA), Dump(_interstitial.LoadRequests));
        }

        [UnityTest]
        public IEnumerator ZeroBackfillDelayLoadsDefaultImmediately()
        {
            ApplyScheduler(0f, 0.05f, 0f, 0.15f);
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, 5d);

            NeftaSdk.LoadInterstitial(InterstitialDefault);
            Assert.AreEqual(1, _interstitial.LoadRequests.Count, Dump(_interstitial.LoadRequests));
            AssertDefault(_interstitial.LoadRequests, InterstitialDefault);
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialDefault), States());
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));

            yield return WaitUntil(() => Count(_interstitial.LoadRequests, InterstitialA) == 1, 2f, "optimized A did not load");
            AssertOptimized(_interstitial.LoadRequests, InterstitialA, "5");
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));
        }

        [UnityTest]
        public IEnumerator PositiveBackfillDelayWaits()
        {
            ApplyScheduler(0f, 0.05f, 0.2f, 0.15f);
            CreateLogics();
            QueueUpcomingInsights(Insights.Interstitial, 5d);

            var started = Time.realtimeSinceStartup;
            NeftaSdk.LoadInterstitial(InterstitialDefault);
            Assert.AreEqual(0, _interstitial.LoadRequests.Count, Dump(_interstitial.LoadRequests));
            Assert.AreEqual("Delay", _interstitial.TrackState(InterstitialDefault), States());

            yield return WaitUntil(() =>
                Count(_interstitial.LoadRequests, InterstitialA) == 1 &&
                Count(_interstitial.LoadRequests, InterstitialDefault) == 1, 2f, "backfill did not load after its delay");

            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, 0.12f);
            Assert.AreEqual("Loading", _interstitial.TrackState(InterstitialA), States());
            AssertDefault(_interstitial.LoadRequests, InterstitialDefault);
            AssertOptimized(_interstitial.LoadRequests, InterstitialA, "5");
            Assert.AreEqual(0, Count(_interstitial.LoadRequests, InterstitialB), Dump(_interstitial.LoadRequests));
        }

        [UnityTest]
        public IEnumerator RewardedTracksStayIndependent()
        {
            ApplyScheduler(0f, 0.05f, -1f, 0.15f);
            NeftaAdapterEvents.FirstResponseTimeoutRewarded = 0.2f;
            NeftaAdapterEvents.FirstResponseTimeoutInterstitial = 0f;
            CreateLogics();
            QueueUpcomingInsights(Insights.Rewarded, 6d, 8d);

            var started = Time.realtimeSinceStartup;
            NeftaSdk.LoadRewardedAd(RewardedDefault);
            yield return WaitUntil(() => Count(_rewarded.LoadRequests, RewardedA) == 1, 2f, "rewarded A did not load");

            AssertOptimized(_rewarded.LoadRequests, RewardedA, "6");
            Assert.AreEqual("Loading", _rewarded.TrackState(RewardedA));
            Assert.AreEqual("Idle", _rewarded.TrackState(RewardedB));
            Assert.AreEqual("Idle", _rewarded.TrackState(RewardedDefault));
            Assert.AreEqual(0, _interstitial.LoadRequests.Count, Dump(_interstitial.LoadRequests));
            Assert.AreEqual("Idle", _interstitial.TrackState(InterstitialA), States());

            yield return WaitUntil(() =>
                Count(_rewarded.LoadRequests, RewardedB) == 1 &&
                Count(_rewarded.LoadRequests, RewardedDefault) == 1, 2f, "rewarded timeout did not open B and the backfill");

            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, 0.1f);
            AssertOptimized(_rewarded.LoadRequests, RewardedB, "8");
            AssertDefault(_rewarded.LoadRequests, RewardedDefault);
            Assert.AreEqual("Loading", _rewarded.TrackState(RewardedA));
            Assert.AreEqual(0, _interstitial.LoadRequests.Count, Dump(_interstitial.LoadRequests));
            Assert.AreEqual(0, _rewardedLoaded.Count);
        }

        [UnityTest]
        public IEnumerator SimulatorLoadsOptimizedFloorThenFloorlessBackfill()
        {
            ApplyScheduler(0f, 0.08f, -1f, 0.15f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("AdDemoScene");
            yield return null;
            yield return null;

            SimulatorUi interstitialUi = null;
            SimulatorUi rewardedUi = null;
            var simulators = UnityEngine.Object.FindObjectsByType<SimulatorUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var simulator in simulators)
            {
                if (simulator.name == "SimulatorInterstitial")
                {
                    interstitialUi = simulator;
                }
                else if (simulator.name == "SimulatorRewarded")
                {
                    rewardedUi = simulator;
                }
            }

            Assert.IsNotNull(interstitialUi);
            Assert.IsNotNull(rewardedUi);
            interstitialUi.Init(true);
            rewardedUi.Init(true);

            var interstitial = (InterstitialSimulator)interstitialUi.AdLogic;
            var rewarded = (RewardedSimulator)rewardedUi.AdLogic;
            QueueUpcomingInsights(Insights.Interstitial, 5d, 7d, 7d);

            SetLoad(interstitialUi, true);
            yield return WaitUntil(() =>
                interstitial.GetTrack(1).GetState == InterstitialSimulator.TrackStatus.State.Loading &&
                interstitial.GetTrack(1).GetInsight != null &&
                Math.Abs(interstitial.GetTrack(1).GetInsight._floorPrice - 5d) < 0.0001d, 2f, "simulator track A did not load with floor 5");

            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Idle, interstitial.GetTrack(2).GetState);
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Idle, interstitial.GetTrack(0).GetState);
            Assert.IsTrue(Status(interstitialUi, "a").Contains("as Optimized"), Status(interstitialUi, "a"));
            Assert.AreEqual(RewardedSimulator.TrackStatus.State.Idle, rewarded.GetTrack(0).GetState);
            Assert.AreEqual(RewardedSimulator.TrackStatus.State.Idle, rewarded.GetTrack(1).GetState);
            Assert.AreEqual(RewardedSimulator.TrackStatus.State.Idle, rewarded.GetTrack(2).GetState);

            ClickResponse(interstitialUi, "a", "Fill2");
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Ready, interstitial.GetTrack(1).GetState);
            Assert.IsNull(interstitial.GetTrack(1).GetInsight);
            yield return WaitUntil(() => interstitial.GetTrack(0).GetState == InterstitialSimulator.TrackStatus.State.Loading, 2f, "backfill did not start");
            Assert.IsTrue(Status(interstitialUi, "c").Contains("as Default"), Status(interstitialUi, "c"));
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Idle, interstitial.GetTrack(2).GetState);

            yield return WaitUntil(() =>
                interstitial.GetTrack(2).GetState == InterstitialSimulator.TrackStatus.State.Loading &&
                interstitial.GetTrack(2).GetInsight != null &&
                Math.Abs(interstitial.GetTrack(2).GetInsight._floorPrice - 7d) < 0.0001d, 2f, "simulator track B did not load with floor 7");

            ClickResponse(interstitialUi, "b", "Fill3");
            ClickResponse(interstitialUi, "c", "Fill1");
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Ready, interstitial.GetTrack(0).GetState);
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Ready, interstitial.GetTrack(1).GetState);
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Ready, interstitial.GetTrack(2).GetState);

            ClickShow(interstitialUi);
            Assert.IsTrue(Status(interstitialUi, "b").Contains("Showing"), Status(interstitialUi, "b"));

            yield return WaitUntil(() => interstitial.GetTrack(2).GetState == InterstitialSimulator.TrackStatus.State.Loading, 2f, "shown track did not reload");
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Ready, interstitial.GetTrack(0).GetState);
            Assert.AreEqual(InterstitialSimulator.TrackStatus.State.Ready, interstitial.GetTrack(1).GetState);
            Assert.AreEqual(RewardedSimulator.TrackStatus.State.Idle, rewarded.GetTrack(1).GetState);
        }

        private void CreateLogics()
        {
            _interstitial = new InterstitialTestLogic();
            _rewarded = new RewardedTestLogic();
            NeftaSdk.Interstitial = _interstitial;
            NeftaSdk.Rewarded = _rewarded;
            _interstitial.Begin(InterstitialDefault, InterstitialA, InterstitialB);
            _rewarded.Begin(RewardedDefault, RewardedA, RewardedB);
            _interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            _rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            _interstitialLoaded.Clear();
            _rewardedLoaded.Clear();
        }

        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _interstitialLoaded.Add(adUnitId);
        }

        private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _rewardedLoaded.Add(adUnitId);
        }

        private static IEnumerator EnsureNefta()
        {
            if (NeftaAdapterEvents.InitConfiguration != null)
            {
                yield break;
            }

            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.UnitTestOverrideOnReady(InitJson);
            NeftaAdapterEvents.InitWithAppId("play-test-app", NeftaAdapterEvents.OptimizationType.Optimized, _ => { });

            var started = Time.realtimeSinceStartup;
            while (NeftaAdapterEvents.InitConfiguration == null)
            {
                if (Time.realtimeSinceStartup - started > 5f)
                {
                    Assert.Fail("Nefta init override did not arrive within 5s");
                }

                yield return null;
            }
        }

        private static void ApplyScheduler(float firstResponseTimeout, float cooldown, float backfillDelay, float retryDelay)
        {
            NeftaAdapterEvents.FirstResponseTimeoutInterstitial = firstResponseTimeout;
            NeftaAdapterEvents.FirstResponseTimeoutRewarded = firstResponseTimeout;
            NeftaAdapterEvents.GlobalTrackCooldown = cooldown;
            NeftaAdapterEvents.BackFillTrackDelay = backfillDelay;

            var delaysField = typeof(NeftaAdapterEvents).GetField("_delays", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(delaysField);
            var delays = (List<float>)delaysField.GetValue(null);
            if (delays == null)
            {
                delays = new List<float>();
                delaysField.SetValue(null, delays);
            }

            delays.Clear();
            delays.Add(retryDelay);

            var failsField = typeof(NeftaAdapterEvents).GetField("ConsecutiveAdFails", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(failsField);
            ((Dictionary<string, int>)failsField.GetValue(null)).Clear();
        }

        private static int PeekInsightId()
        {
            var field = typeof(NeftaAdapterEvents).GetField("_insightId", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field);
            return (int)field.GetValue(null);
        }

        private static void QueueUpcomingInsights(int insightType, params double[] floors)
        {
            var id = PeekInsightId();
            for (var i = 0; i < floors.Length; i++)
            {
                QueueInsight(id + i, insightType, floors[i], 0f);
            }
        }

        private static void QueueUpcomingInsights(int insightType, params (double floor, float delay)[] insights)
        {
            var id = PeekInsightId();
            for (var i = 0; i < insights.Length; i++)
            {
                QueueInsight(id + i, insightType, insights[i].floor, insights[i].delay);
            }
        }

        private static void QueueInsight(int id, int insightType, double floor, float delay)
        {
            RemoveQueuedInsight(id);
            var json = "{\"floor_price\":" + floor.ToString(CultureInfo.InvariantCulture) +
                       ",\"delay\":" + delay.ToString(CultureInfo.InvariantCulture) + "}";
            NeftaAdapterEvents.UnitTestOverrideOnInsight(id, insightType, json);
        }

        private static void RemoveQueuedInsight(int id)
        {
            Type pluginType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                pluginType = assembly.GetType("Nefta.Editor.NeftaPlugin");
                if (pluginType != null)
                {
                    break;
                }
            }

            Assert.IsNotNull(pluginType, "NeftaPlugin");
            var field = pluginType.GetField("_overridenInsights", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field);
            var dictionary = field.GetValue(null);
            var remove = dictionary.GetType().GetMethod("Remove", new[] { typeof(int) });
            Assert.IsNotNull(remove);
            remove.Invoke(dictionary, new object[] { id });
        }

        private IEnumerator WaitUntil(Func<bool> condition, float timeout, string message)
        {
            var started = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - started > timeout)
                {
                    Assert.Fail(message + " after " + timeout.ToString("0.0", CultureInfo.InvariantCulture) + "s. " + Dump(_interstitial) + " | " + Dump(_rewarded) + " | " + States());
                }

                yield return null;
            }
        }

        private string States()
        {
            if (_interstitial == null)
            {
                return "";
            }

            return "D=" + _interstitial.TrackState(InterstitialDefault) +
                   " A=" + _interstitial.TrackState(InterstitialA) +
                   " B=" + _interstitial.TrackState(InterstitialB);
        }

        private static string Dump(InterstitialTestLogic logic)
        {
            return logic == null ? "(no interstitial)" : Dump(logic.LoadRequests);
        }

        private static string Dump(RewardedTestLogic logic)
        {
            return logic == null ? "(no rewarded)" : Dump(logic.LoadRequests);
        }

        private static string Dump(List<LoadRequest> loads)
        {
            if (loads == null || loads.Count == 0)
            {
                return "(no loads)";
            }

            var parts = new string[loads.Count];
            for (var i = 0; i < loads.Count; i++)
            {
                var load = loads[i];
                parts[i] = load.DisableAutoRetries ? load.AdUnitId + " floor=" + load.BidFloor : load.AdUnitId + " default";
            }

            return string.Join("; ", parts);
        }

        private static int Count(List<LoadRequest> loads, string adUnitId)
        {
            var count = 0;
            for (var i = 0; i < loads.Count; i++)
            {
                if (loads[i].AdUnitId == adUnitId)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertOptimized(List<LoadRequest> loads, string adUnitId, string floor)
        {
            var load = Single(loads, adUnitId);
            Assert.IsTrue(load.DisableAutoRetries, Dump(loads));
            Assert.AreEqual(floor, load.BidFloor, Dump(loads));
        }

        private static void AssertDefault(List<LoadRequest> loads, string adUnitId)
        {
            var load = Single(loads, adUnitId);
            Assert.IsFalse(load.DisableAutoRetries, Dump(loads));
            Assert.IsNull(load.BidFloor, Dump(loads));
        }

        private static LoadRequest Single(List<LoadRequest> loads, string adUnitId)
        {
            LoadRequest found = null;
            var count = 0;
            for (var i = 0; i < loads.Count; i++)
            {
                if (loads[i].AdUnitId != adUnitId)
                {
                    continue;
                }

                count++;
                found = loads[i];
            }

            Assert.AreEqual(1, count, "loads for " + adUnitId + ": " + Dump(loads));
            return found;
        }

        private static void SetLoad(SimulatorUi simulatorUi, bool value)
        {
            var loadField = typeof(SimulatorUi).GetField("_load", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadToggle = (Toggle)loadField.GetValue(simulatorUi);
            loadToggle.SetIsOnWithoutNotify(!value);
            loadToggle.isOn = value;
        }

        private static void ClickResponse(SimulatorUi simulatorUi, string track, string response)
        {
            var responseField = typeof(SimulatorUi).GetField("_" + track + response, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(responseField, track + response);
            ((Button)responseField.GetValue(simulatorUi)).onClick.Invoke();
        }

        private static void ClickShow(SimulatorUi simulatorUi)
        {
            var showField = typeof(SimulatorUi).GetField("_show", BindingFlags.NonPublic | BindingFlags.Instance);
            ((Button)showField.GetValue(simulatorUi)).onClick.Invoke();
        }

        private static string Status(SimulatorUi simulatorUi, string track)
        {
            var statusField = typeof(SimulatorUi).GetField("_" + track + "Status", BindingFlags.NonPublic | BindingFlags.Instance);
            return ((Text)statusField.GetValue(simulatorUi)).text;
        }
    }
}
