using System;

namespace NeftaCustomAdapter
{
    public class Insights
    {
        public const int None = 0;
        public const int Churn = 1 << 0;
        public const int Banner = 1 << 1;
        public const int Interstitial = 1 << 2;
        public const int Rewarded = 1 << 3;
        
        public Churn _churn;
        public AdInsight _banner;
        public AdInsight _interstitial;
        public AdInsight _rewarded;
    }
    
    public class Churn
    {
        public double _d1_probability;
        public double _d3_probability;
        public double _d7_probability;
        public double _d14_probability;
        public double _d30_probability;
        public string _probability_confidence;
    }
    
    public class AdInsight
    {
        public NeftaAdapterEvents.AdType _type;
        public double _floorPrice;
        public string _adUnit;

        public override string ToString()
        {
            return $"AdInsight[type: {_type}, recommendedAdUnit: {_adUnit}, floorPrice: {_floorPrice}]";
        }
    }
}