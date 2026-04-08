namespace NeftaCustomAdapter
{
    public class InitConfiguration
    {
        public bool _skipOptimization;
        public string _nuid;

        public InitConfiguration(NeftaAdapterEvents.InitConfigurationDto dto)
        {
            if (dto != null)
            {
                _skipOptimization = dto.skipOptimization;
                _nuid = dto.nuid;   
            }
        }
    }
}