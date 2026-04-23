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
            UnityEngine.SceneManagement.SceneManager.LoadScene("AdDemo/AdDemoScene");
            yield return null;
            
            yield return null;
            var infoController = Object.FindFirstObjectByType<InfoController>();
            var toggleUIMethod = typeof(InfoController).GetMethod("ToggleUI", BindingFlags.NonPublic | BindingFlags.Instance);
            toggleUIMethod.Invoke(infoController, new object[] { true });
            
            var simControllers = Object.FindObjectsByType<SimulatorController>(FindObjectsSortMode.None);
            SimulatorController interstitialController = null;
            SimulatorController rewardedController = null;
            foreach (var simController in simControllers)
            {
                if (simController.name == "InterstitialSimulatorController")
                {
                    interstitialController = simController;
                }
                else
                {
                    rewardedController = simController;
                }
            }
            
            Assert.IsNotNull(interstitialController);

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
            var trackA = ((SimulatorInterstitialLogic)interstitialController.AdLogic).GetTrack(true);
            var trackB = ((SimulatorInterstitialLogic)interstitialController.AdLogic).GetTrack(false);
            
            // initiate load
            var loadFiled = typeof(SimulatorController).GetField("_load", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadToggle = (Toggle)loadFiled.GetValue(interstitialController);
            loadToggle.isOn = true;
            
            // verify trackA is loaded, trackB is still idle
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(trackA.GetState == SimulatorInterstitialLogic.TrackStatus.State.LoadingWithInsights);
            Debug.Log($".. {trackA.GetInsight} .. {trackB.GetState}");
            Assert.IsTrue(trackB.GetState == SimulatorInterstitialLogic.TrackStatus.State.Idle);
        }
    }
}