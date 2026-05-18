using System.Collections;
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
    public class DualTrackTest
    {
        [UnityTest]
        public IEnumerator BasicFlow()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Editor/Tests/PlayTests/PlayTestScene");
            yield return null;
            
            yield return null;
            
            NeftaAdapterEvents.EnableLogging(true);
            NeftaAdapterEvents.InitWithAppId("5693275310653440", (InitConfiguration config) =>
            {
                Debug.Log($"[PlayTest] Initialized, nuid: {config._nuid}");
            });
            
            var simControllers = Object.FindObjectsByType<SimulatorUi>(FindObjectsSortMode.None);
            SimulatorUi interstitialUi = null;
            SimulatorUi rewardedUi = null;
            foreach (var simController in simControllers)
            {
                if (simController.name == "SimulatorInterstitial")
                {
                    interstitialUi = simController;
                    interstitialUi.Init();
                }
                else
                {
                    rewardedUi = simController;
                    rewardedUi.Init();
                }
            }
            
            Assert.IsNotNull(interstitialUi);

            var startTime = Time.time;
            while (NeftaAdapterEvents.InitConfiguration == null)
            {
                if (Time.time - startTime > 5f)
                {
                    Assert.Fail("No OnReady callback in 5s");
                }
                yield return null;
            }
            
            // get tracks
            var trackA = ((InterstitialSimulator)interstitialUi.AdLogic).GetTrack(true);
            var trackB = ((InterstitialSimulator)interstitialUi.AdLogic).GetTrack(false);
            
            // initiate load
            var loadFiled = typeof(SimulatorUi).GetField("_load", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadToggle = (Toggle)loadFiled.GetValue(interstitialUi);
            loadToggle.isOn = true;
            
            // verify trackA is loaded, trackB is still idle
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(trackA.GetState == InterstitialSimulator.TrackStatus.State.LoadingWithInsights);
            Assert.IsTrue(trackB.GetState == InterstitialSimulator.TrackStatus.State.Idle);
        }
    }
}