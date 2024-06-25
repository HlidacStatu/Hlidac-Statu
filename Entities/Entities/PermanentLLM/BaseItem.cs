using System;

namespace HlidacStatu.Entities.PermanentLLM
{
    public class BaseItem<T>
                where T : class
    {

        private string _id = null;

        [Nest.Keyword]
        public string Id
        {
            get
            {
                if (_id == null)
                    _id = GetId(this.DocumentType, this.DocumentID, this.FileID);

                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public static string GetId(string documentType, string documentId, string fileId)
        {

            if (string.IsNullOrWhiteSpace(documentId)
                && string.IsNullOrWhiteSpace(documentType)
                )
                return null;
            
            return $"{documentType}-{documentId}-{fileId ?? "0"}";
        }

        [Nest.Keyword]
        public string DocumentID { get; set; }
        [Nest.Keyword]
        public string FileID { get; set; }

        [Nest.Keyword]
        public virtual string DocumentType { get; set; }

        [Nest.Keyword]
        public string GeneratorVersion { get; set; }
        [Nest.Keyword]
        public string GeneratorName { get; set; }
        [Nest.Date]
        public DateTime Created { get; set; }

        [Nest.Keyword]
        public string PartType { get; set; } = typeof(T).Name;
        [Nest.Object(Enabled = false)]
        public T Parts { get; set; } = default;


    }
}

