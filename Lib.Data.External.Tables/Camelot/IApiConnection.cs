namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    public interface IApiConnection
    {
        string GetEndpointUrl();
        string GetApiKey();
        void DeclareDeadEndpoint(string url);
    }
}
