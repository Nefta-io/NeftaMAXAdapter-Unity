using System;

namespace NeftaCustomAdapter
{
    [Serializable]
    public class InsightsDto
    {
        public ChurnDto churn;
        public FloorPriceDto floor_price;
    }
    
    [Serializable]
    public class ChurnDto
    {
        public double d1_probability;
        public double d3_probability;
        public double d7_probability;
        public double d14_probability;
        public double d30_probability;
        public string probability_confidence;
    }

    [Serializable]
    public class FloorPriceDto
    {
        public AdConfigurationDto banner_configuration;
        public AdConfigurationDto interstitial_configuration;
        public AdConfigurationDto rewarded_configuration;
    }

    [Serializable]
    public class AdConfigurationDto
    {
        public int ad_opportunity_id;
        public double floor_price;
        public string ad_unit;
    }
}