using System;

namespace HlidacStatu.Entities.PermanentLLM
{
    public class BaseItem
    {

        private string _id = null;

        [Nest.Keyword]
        public string Id
        {
            get
            {
                if (_id == null)
                    _id = GetId(this.DocumentType, this.DocumentID, this.PartType, this.FileID);

                return _id;
            }
            set
            {
                _id = value;
            }
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
        public virtual string PartType { get; set; }


        public static string GetId(string documentType, string documentId, string partType, string fileId)
        {

            if (string.IsNullOrWhiteSpace(documentId)
                && string.IsNullOrWhiteSpace(documentType)
                )
                return null;

            return $"{documentType}-{partType}-{documentId}-{fileId ?? "0"}";
        }


    }

    public class BaseItem<T> : BaseItem
                where T : class
    {


        [Nest.Keyword]
        public override string PartType { get; set; } = typeof(T).Name;

        [Nest.Object(Enabled = false)]
        public T Parts { get; set; } = default;


    }
}

