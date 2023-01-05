namespace HlidacStatu.Entities.Entities;

public class ConfigurationValue
{
    public class Environments
    {
        public const string Production = nameof(Production);
        public const string Stage = nameof(Stage);
    }
    
    public int Id { get; set; }
    public string KeyName { get; set; }
    public string KeyValue { get; set; }
    public string Environment { get; set; }
    public string Tag { get; set; }
}