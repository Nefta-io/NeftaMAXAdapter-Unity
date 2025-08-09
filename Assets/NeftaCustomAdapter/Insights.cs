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
        
        public Insights(InsightsDto dto)
        {
            if (dto == null)
            {
                return;
            }

            if (dto.churn != null)
            {
                _churn = new Churn()
                {
                    _d1_probability = dto.churn.d1_probability,
                    _d3_probability = dto.churn.d3_probability,
                    _d7_probability = dto.churn.d7_probability,
                    _d14_probability = dto.churn.d14_probability,
                    _d30_probability = dto.churn.d30_probability,
                    _probability_confidence = dto.churn.probability_confidence,
                };
            }

            if (dto.floor_price != null)
            {
                var banner = dto.floor_price.banner_configuration;
                if (banner != null)
                {
                    _banner = new AdInsight()
                    {
                        _type = NeftaAdapterEvents.AdType.Banner,
                        _floorPrice = banner.floor_price,
                        _adUnit = banner.ad_unit
                    };
                }
                var interstitial = dto.floor_price.interstitial_configuration;
                if (interstitial != null)
                {
                    _interstitial = new AdInsight()
                    {
                        _type = NeftaAdapterEvents.AdType.Interstitial,
                        _floorPrice = interstitial.floor_price,
                        _adUnit = interstitial.ad_unit
                    };
                }

                var rewarded = dto.floor_price.rewarded_configuration;
                if (rewarded != null)
                {
                    _rewarded = new AdInsight()
                    {
                        _type = NeftaAdapterEvents.AdType.Rewarded,
                        _floorPrice = rewarded.floor_price,
                        _adUnit = rewarded.ad_unit
                    };
                }
            }
        }
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