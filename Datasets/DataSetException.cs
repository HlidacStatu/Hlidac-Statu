using System;
using System.Runtime.Serialization;

namespace HlidacStatu.Datasets
{
    public class DataSetException : ArgumentException
    {
        public string DatasetId { get; set; }
        public static DataSetException GetExc(string datasetId, int number, string description, string errorDetail = null)
        {
            return new DataSetException(datasetId, ApiResponseStatus.Error(number, description, errorDetail));
        }

        public ApiResponseStatus APIResponse { get; set; } = null;

        public DataSetException(string datasetId, ApiResponseStatus apiResp)
            : base()
        {
            DatasetId = datasetId;
            APIResponse = apiResp;
        }

        public DataSetException(string datasetId, string message, ApiResponseStatus apiResp)
            : base(message)
        {
            DatasetId = datasetId;
            APIResponse = apiResp;
        }

        public DataSetException(string datasetId, string message, ApiResponseStatus apiResp, Exception innerException)
            : base(message, innerException)
        {
            DatasetId = datasetId;
            APIResponse = apiResp;
        }

        protected DataSetException(string datasetId, ApiResponseStatus apiResp, SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            DatasetId = datasetId;
            APIResponse = apiResp;
        }
        public override string Message {
            get {
                return $"Dataset:{DatasetId}\nApiResponse:{APIResponse.ToString()}\n{base.Message}";
                }
        }
    
        
    }
}
