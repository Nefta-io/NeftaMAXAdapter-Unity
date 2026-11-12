using com.adjust.sdk;
using NeftaCustomAdapter;

namespace AdDemo
{
    public class AttributionAdjust
    {
        public void Start()
        {
            var adjustConfig = new AdjustConfig("token", AdjustEnvironment.Sandbox);
            adjustConfig.setAttributionChangedDelegate(OnAttributionChanged);
            Adjust.start(adjustConfig);
        }

        private void OnAttributionChanged(AdjustAttribution a)
        {
            NeftaAdapterEvents.OnAttributionChanged(NeftaAdapterEvents.Mmp.Adjust, a.adid, a.network, a.campaign, a.costType, a.costAmount.GetValueOrDefault(), a.costCurrency);
        }
    }
}