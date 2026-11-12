using System;
using System.Globalization;
using AppsFlyerSDK;
using NeftaCustomAdapter;
using UnityEngine;

namespace AdDemo
{
    public class AttributionAppsFlyer : MonoBehaviour, IAppsFlyerConversionData
    {
        public void Start()
        {
            AppsFlyer.enableDebug(true);
            AppsFlyer.registerConversionListener(onConversionDataSuccess, onConversionDataFail);
            AppsFlyer.init("YOUR_DEV_KEY", "YOUR_APP_ID", this);
            AppsFlyer.start();
        }
        
        public async void onConversionDataSuccess(string conversionData)
        {
            var data = AppsFlyer.CallbackStringToDictionary(conversionData);

            var id = await AppsFlyer.getAppsFlyerUID();
            data.TryGetValue("media_source", out var network);
            data.TryGetValue("campaign", out var campaign);
            data.TryGetValue("af_cost_model", out var costType);
            var cost = 0.0;
            if (data.TryGetValue("cost_cents_USD", out var costObject))
            {
                if (costObject is double)
                {
                    cost = (double)costObject;
                }
                else
                {
                    cost = Double.Parse((string)costObject, CultureInfo.InvariantCulture);   
                }
            }
            NeftaAdapterEvents.OnAttributionChanged(NeftaAdapterEvents.Mmp.AppsFlyer, id, (string)network, (string)campaign, (string)costType, cost, "USD");
        }

        public void onConversionDataFail(string error)
        {
        }
    }
}