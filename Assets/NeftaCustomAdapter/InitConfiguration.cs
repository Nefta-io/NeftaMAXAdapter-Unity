using System;

namespace NeftaCustomAdapter
{
    public class InitConfiguration
    {
        [Obsolete] public bool _skipOptimization;
        public string _nuid;
        public bool _isSessionOptimized;

        public InitConfiguration(NeftaAdapterEvents.InitConfigurationDto dto)
        {
            if (dto != null)
            {
                _skipOptimization = dto.skipOptimization;
                _nuid = dto.nuid;
                _isSessionOptimized = dto.isSessionOptimized;
            }
        }
    }
}