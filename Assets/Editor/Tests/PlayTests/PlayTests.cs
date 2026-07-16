using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AdDemo;
using NeftaCustomAdapter;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Editor.Tests.PlayTests
{
    public class PlayTests
    {
        private SimulatorUi _interstitialUi = null;
        private SimulatorUi _rewardedUi = null;
        
        private List<string> _loadCallbacks = new List<string>();
        
        [UnityTest]
        public IEnumerator BasicFlow()
        {
            yield return StartSimulator();
            
            var trackA = ((InterstitialSimulator)_interstitialUi.AdLogic).GetTrack(true);
            var trackB = ((RewardedSimulator)_rewardedUi.AdLogic).GetTrack(false);

            ToggleLoad(_interstitialUi, true);
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(trackA.GetState == InterstitialSimulator.TrackStatus.State.LoadingWithInsights);
            Assert.IsTrue(trackB.GetState == RewardedSimulator.TrackStatus.State.Idle);
        }

        [UnityTest]
        public IEnumerator WrapperFlow()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Editor/Tests/PlayTests/WrapperScene");
            
            yield return null;
            yield return null;

            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.UnitTestOverrideOnReady("{\"nuid\":\"abc\"}");
            NeftaAdapterEvents.InitWithAppId("5693275310653440", (InitConfiguration config) => { Debug.Log($"[PlayTest] Initialized, nuid: {config._nuid}"); });

            var startTime = Time.time;
            while (NeftaAdapterEvents.InitConfiguration == null)
            {
                if (Time.time - startTime > 5f)
                {
                    Assert.Fail("No OnReady callback in 5s");
                }
                yield return null;
            }

            const string AdUnitInterA = "auI_A";
            const string AdUnitInterB = "auI_B";
            
            var interstitialLogic = new InterstitialTestLogic();
            NeftaSdk.Interstitial = interstitialLogic;
            var rewardedLogic = new RewardedTestLogic();
            NeftaSdk.Rewarded = rewardedLogic;
            
            interstitialLogic.Initialize(AdUnitInterA, AdUnitInterB);
            NeftaSdk.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            
            NeftaAdapterEvents.UnitTestOverrideOnInsight(0, Insights.Interstitial, "{\"floor_price\":4.2}");
            NeftaAdapterEvents.UnitTestOverrideOnInsight(1, Insights.Interstitial, "{\"floor_price\":3.2}");
            NeftaSdk.LoadInterstitial(AdUnitInterA);

            yield return null;
            
            Assert.AreEqual(1, interstitialLogic.LoadRequests.Count);
            Assert.AreEqual(AdUnitInterA, interstitialLogic.LoadRequests[0].AdUnitId);
            Assert.IsTrue(interstitialLogic.LoadRequests[0].DisableAutoRetries);
            Assert.AreEqual("4.2", interstitialLogic.LoadRequests[0].BidFloor);
            Assert.False(interstitialLogic.IsAdReady());
            Assert.AreEqual(0, _loadCallbacks.Count);
            
            interstitialLogic.InvokeLoad(AdUnitInterA, 0.002);
            
            Assert.AreEqual(AdUnitInterA, _loadCallbacks[0]);
            Assert.True(interstitialLogic.IsAdReady());

            yield return null;
            
            Assert.AreEqual(2, interstitialLogic.LoadRequests.Count);
            Assert.AreEqual(AdUnitInterB, interstitialLogic.LoadRequests[1].AdUnitId);
            Assert.IsTrue(interstitialLogic.LoadRequests[1].DisableAutoRetries);
            Assert.AreEqual("3.2", interstitialLogic.LoadRequests[1].BidFloor);
            
            interstitialLogic.InvokeLoad(AdUnitInterB, 0.001);

            yield return null;
            
            Assert.AreEqual(1, _loadCallbacks.Count);

            interstitialLogic.ShowAd();

            yield return null;
            NeftaSdk.LoadInterstitial(AdUnitInterA);
            
            yield return null;
            
            Assert.AreEqual(2, _loadCallbacks.Count);
            Assert.AreEqual(AdUnitInterB, _loadCallbacks[1]);
        }

        private IEnumerator StartSimulator()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Editor/Tests/PlayTests/SimulatorScene");
            yield return null;
            
            yield return null;
            
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.InitWithAppId("5763106043068416", (InitConfiguration config) =>
            {
                Debug.Log($"[PlayTest] Initialized, nuid: {config._nuid}");
            });
            
            var simControllers = Object.FindObjectsByType<SimulatorUi>(FindObjectsSortMode.None);
            _interstitialUi = null;
            _rewardedUi = null;
            foreach (var simController in simControllers)
            {
                if (simController.name == "SimulatorInterstitial")
                {
                    _interstitialUi = simController;
                    _interstitialUi.Init(true);
                }
                else
                {
                    _rewardedUi = simController;
                    _rewardedUi.Init(true);
                }
            }
            
            Assert.IsNotNull(_interstitialUi);
            Assert.IsNotNull(_rewardedUi);

            var startTime = Time.time;
            while (NeftaAdapterEvents.InitConfiguration == null)
            {
                if (Time.time - startTime > 5f)
                {
                    Assert.Fail("No OnReady callback in 5s");
                }
                yield return null;
            }
        }

        private void ToggleLoad(SimulatorUi simulatorUi, bool value)
        {
            var loadField = typeof(SimulatorUi).GetField("_load", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadToggle = (Toggle)loadField.GetValue(simulatorUi);
            loadToggle.isOn = value;
        }

        private void SelectResponse(SimulatorUi simulatorUi, string track, int response)
        {
            var fieldName = "Fill2";
            if (response == 1)
            {
                fieldName = "Fill1";
            }
            else if (response == 2)
            {
                fieldName = "NoFill";
            }
            else if (response == 3)
            {
                fieldName = "Other";
            }
            fieldName = $"_{track}{fieldName}";
            
            var responseField = typeof(SimulatorUi).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var responseButton = (Button)responseField.GetValue(simulatorUi);
            responseButton.onClick.Invoke();
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _loadCallbacks.Add(adUnitId);
        }
    }
}