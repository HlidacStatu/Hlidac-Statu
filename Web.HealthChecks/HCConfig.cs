using Microsoft.Extensions.Configuration;

namespace HlidacStatu.Web.HealthChecks
{
    public interface IHCConfig
    { 
        string DefaultSectionName { get; }
    }

    public class HCConfig<T>
        where T: IHCConfig, new()
    {
        public T ConfigData { get; } = default(T);

        public HCConfig(IConfigurationSection configurationSection)
            : this(configurationSection, new T().DefaultSectionName)
        {
        }
            public HCConfig(IConfigurationSection configurationSection, string healthCheckSectionNameForT)
        {
            ConfigData = configurationSection.GetSection(healthCheckSectionNameForT).Get<T>();
        }
    }
}
