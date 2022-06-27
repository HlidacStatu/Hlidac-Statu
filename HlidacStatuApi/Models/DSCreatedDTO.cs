namespace HlidacStatuApi.Models
{
    public class DSCreatedDTO
    {
        public DSCreatedDTO(string datasetId)
        {
            DatasetId = datasetId;
        }
        public string DatasetId { get; set; }
    }
}