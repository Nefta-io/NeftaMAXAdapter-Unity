using UnityEngine;

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

        public readonly AdInsight Insight;

        public Insights()
        {
        }
        
        public Insights(int adapterResponseType, string adapterResponse)
        {
            if (adapterResponseType == Churn)
            {
                var churn = JsonUtility.FromJson<ChurnDto>(adapterResponse);
                if (churn != null)
                {
                    _churn = new Churn(churn);   
                }
            }
            else if (adapterResponseType == Banner)
            {
                var banner = JsonUtility.FromJson<AdConfigurationDto>(adapterResponse);
                if (banner != null)
                {
                    _banner = new AdInsight(NeftaAdapterEvents.AdType.Banner, banner);
                    Insight = _banner;
                }
            }
            else if (adapterResponseType == Interstitial)
            {
                var interstitial = JsonUtility.FromJson<AdConfigurationDto>(adapterResponse);
                if (interstitial != null)
                {
                    _interstitial = new AdInsight(NeftaAdapterEvents.AdType.Interstitial, interstitial);
                    Insight = _interstitial;
                }
            }
            else if (adapterResponseType == Rewarded)
            {
                var rewarded = JsonUtility.FromJson<AdConfigurationDto>(adapterResponse);
                if (rewarded != null)
                {
                    _rewarded = new AdInsight(NeftaAdapterEvents.AdType.Rewarded, rewarded);
                    Insight = _rewarded;
                }
            }
        }

        public override string ToString()
        {
            if (Insight == null)
            {
                return "No Insight";
            }
            return Insight.ToString();
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
        
        public Churn(ChurnDto dto)
        {
            if (dto != null)
            {
                _d1_probability = dto.d1_probability;
                _d3_probability = dto.d3_probability;
                _d7_probability = dto.d7_probability;
                _d14_probability = dto.d14_probability;
                _d30_probability = dto.d30_probability;
                _probability_confidence = dto.probability_confidence;
            }
        }
    }
    
    public class AdInsight
    {
        public int _requestId;
        public int _adOpportunityId;
        public int _auctionId;
        public NeftaAdapterEvents.AdType _type;
        public double _floorPrice;
        public string _adUnit;
        public float _delay;
        
        public AdInsight(NeftaAdapterEvents.AdType type, AdConfigurationDto dto)
        {
            _type = type;
            if (dto != null)
            {
                _requestId = dto.request_id;
                _adOpportunityId = dto.ad_opportunity_id;
                _auctionId = dto.auction_id;
                _floorPrice = dto.floor_price;
                _adUnit = dto.ad_unit;
                _delay = dto.delay;
            }
            else
            {
                _floorPrice = 0;
                _delay = 0;
            }
        }

        public override string ToString()
        {
            return $"AdInsight[type: {_type}, recommendedAdUnit: {_adUnit}, floorPrice: {_floorPrice}, delay: {_delay}, adOpportunityId: {_adOpportunityId} auctionId: {_auctionId}]";
        }
    }
}