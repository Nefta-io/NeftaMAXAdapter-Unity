using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NeftaCustomAdapter;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Editor.Tests
{
    public class AdapterTest
    {
        private static readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        private InitConfiguration _config;
        private Insights _insight;
        
        [UnityTest]
        public IEnumerator TestInitNull()
        {
            NeftaAdapterEvents.UnitTestOverrideOnReady(null);
            NeftaAdapterEvents.InitWithAppId("appId", OnReady);
            yield return WaitOnInitResponse();
            Assert.IsFalse(_config._skipOptimization);
            Assert.IsNull(_config._nuid);
        }
        
        [UnityTest]
        public IEnumerator TestInitEmpty()
        {
            NeftaAdapterEvents.UnitTestOverrideOnReady("");
            NeftaAdapterEvents.InitWithAppId("appId", OnReady);
            yield return WaitOnInitResponse();
            Assert.IsFalse(_config._skipOptimization);
            Assert.IsNull(_config._nuid);
        }
        
        [UnityTest]
        public IEnumerator TestInitMalformed()
        {
            NeftaAdapterEvents.UnitTestOverrideOnReady("{");
            NeftaAdapterEvents.InitWithAppId("appId", OnReady);
            yield return WaitOnInitResponse();
            Assert.IsFalse(_config._skipOptimization);
            Assert.IsNull(_config._nuid);
        }
        
        [UnityTest]
        public IEnumerator TestInitDefault()
        {
            var nuid = "abc";
            NeftaAdapterEvents.UnitTestOverrideOnReady($"{{\"nuid\":\"{nuid}\",\"skipOptimization\":true,\"delays\":[17,321,456.7],\"noDynamicResponseRetryInMs\":10123,\"noDefaultResponseRetryInMs\":11654}}");
            NeftaAdapterEvents.InitWithAppId("appId", OnReady);
            yield return WaitOnInitResponse();
            Assert.IsTrue(_config._skipOptimization);
            Assert.AreEqual(nuid, _config._nuid);
            
            var delaysField = typeof(NeftaAdapterEvents).GetField("_delays", BindingFlags.NonPublic | BindingFlags.Static);
            var delays = (List<float>)delaysField.GetValue(null);
            Assert.AreEqual(3, delays.Count);
            Assert.AreEqual(17, delays[0], 17);
            Assert.AreEqual(321, delays[1]);
            Assert.AreEqual(456.7, delays[2], 0.0001);
            
            var noDynamicResponseRetryInMsField = typeof(NeftaAdapterEvents).GetField("NoDynamicResponseRetryInMs", BindingFlags.NonPublic | BindingFlags.Static);
            var noDynamicResponseRetryInMs = (int)noDynamicResponseRetryInMsField.GetValue(null);
            Assert.AreEqual(10123, noDynamicResponseRetryInMs);
            var noDefaultResponseRetryInMsField = typeof(NeftaAdapterEvents).GetField("NoDefaultResponseRetryInMs", BindingFlags.NonPublic | BindingFlags.Static);
            var noDefaultResponseRetryInMs = (int)noDefaultResponseRetryInMsField.GetValue(null);
            Assert.AreEqual(11654, noDefaultResponseRetryInMs);
        }
        
        [UnityTest]
        public IEnumerator TestInsight()
        {
            NeftaAdapterEvents.UnitTestOverrideOnReady("{\"nuid\":\"abc\",\"skipOptimization\":false,\"delays\":[]}");
            NeftaAdapterEvents.InitWithAppId("appId", OnReady);
            yield return WaitOnInitResponse();
            
            yield return WaitForInsight(Insights.Interstitial, "{\"ad_opportunity_id\":13,\"auction_id\":14,\"floor_price\":4.2,\"delay\":6.9,\"insight_context\":{\"corrected_mean\":\"0\"}}");
            
            Assert.IsTrue(_insight._interstitial != null);
            Assert.AreEqual(_insight.Insight, _insight._interstitial);
            Assert.AreEqual(13, _insight.Insight._adOpportunityId);
            Assert.AreEqual(14, _insight.Insight._auctionId);
            Assert.AreEqual(null, _insight.Insight._adUnit, null);
            Assert.AreEqual(4.2, _insight._interstitial._floorPrice, 0.0001);
            Assert.AreEqual(6.9, _insight._interstitial._delay, 0.0001);
        }

        [UnityTest]
        public IEnumerator TestRetryDelay()
        {
            var delays = new[] { 3f, 7f, 11f, 11f };
            NeftaAdapterEvents.UnitTestOverrideOnReady("{\"nuid\":\"abc\",\"skipOptimization\":false,\"delays\":[3,7,11]}");
            NeftaAdapterEvents.InitWithAppId("appId", OnReady);
            yield return WaitOnInitResponse();

            for (var i = 0; i < 4; i++)
            {
                var delay = delays[i];
                yield return WaitForInsight(Insights.Rewarded, $"{{\"ad_opportunity_id\":1,\"auction_id\":{i + 1},\"floor_price\":{delay}}}");
            
                Assert.IsTrue(_insight._rewarded != null);
                Assert.AreEqual(delay, _insight._rewarded._floorPrice, 0.0001);
                
                Assert.AreEqual(delay, NeftaAdapterEvents.GetRetryDelayInSeconds(_insight.Insight), 0.0001);
            }
        }

        private IEnumerator WaitOnInitResponse()
        {
            var start = Time.time;
            var responseTime = 0f;
            while (_config == null && responseTime < 5f)
            {
                responseTime = Time.time - start;
                yield return null;
            }
            
            TestContext.WriteLine($"OnReady delay {responseTime}");
            Assert.IsTrue(responseTime < 5f, "Init response not received under 5s");
        }
        
        private void OnReady(InitConfiguration config)
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            TestContext.WriteLine($"OnReady {_mainThreadId}/{Thread.CurrentThread.ManagedThreadId}");
            Assert.IsTrue(currentThreadId == _mainThreadId, "Init response not received on main thread");
            _config = config;  
        }

        private IEnumerator WaitForInsight(int insightType, string insightJson)
        {
            var requestIdField = typeof(NeftaAdapterEvents).GetField("_insightId", BindingFlags.NonPublic | BindingFlags.Static);
            var requestId = (int)requestIdField.GetValue(null);

            NeftaAdapterEvents.UnitTestOverrideOnInsight(requestId, insightType, insightJson);
            _insight = null;
            NeftaAdapterEvents.GetInsights(insightType, null, OnInsight);
            yield return WaitOnInsightResponse(5f);
        }
        
        private IEnumerator WaitOnInsightResponse(float maxDuration)
        {
            var start = Time.time;
            var responseTime = 0f;
            while (_insight == null && responseTime < maxDuration)
            {
                responseTime = Time.time - start;
                yield return null;
            }
            
            TestContext.WriteLine($"OnInsights delay {responseTime} .. {_insight}");
            Assert.IsTrue(responseTime < maxDuration, $"Insights response not received under {maxDuration}s");
        }

        private void OnInsight(Insights insight)
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            TestContext.WriteLine($"OnInsight {_mainThreadId}/{Thread.CurrentThread.ManagedThreadId}: {insight}");
            Assert.IsTrue(currentThreadId == _mainThreadId, "Insight response not received on main thread");
            _insight = insight;
        }
    }
}