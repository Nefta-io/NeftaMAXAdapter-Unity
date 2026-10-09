namespace NeftaCustomAdapter
{
    public class InitConfiguration
    {
        public string _nuid;
        public bool _isSessionOptimized;

        public InitConfiguration(NeftaAdapterEvents.InitConfigurationDto dto)
        {
            if (dto != null)
            {
                _nuid = dto.nuid;
                _isSessionOptimized = dto.isSessionOptimized;
            }
        }
    }
}