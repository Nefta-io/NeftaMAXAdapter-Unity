using System;

namespace NeftaCustomAdapter
{
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
    public class AdConfigurationDto
    {
        public int request_id;
        public int ad_opportunity_id;
        public int auction_id;
        public double floor_price;
        public string ad_unit;
        public float delay;
        public InsightDto b;
    }
    
    [Serializable]
    public class InsightDto
    {
        public int request_id;
        public int ad_opportunity_id;
        public int auction_id;
        public double floor_price;
        public float delay;
    }
}