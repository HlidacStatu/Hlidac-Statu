using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Entities
{
    public partial class QVoiceToText
    {
        public enum CheckState : int
        {
            WaitingInQueue = 0,
            InProgress = 1,
            Done = 2,
            ResultTaken = 100,
            Error = -1,
        }

        public class SourceOption
        {
            public string filename { get; set; }
            public string url { get; set; }
            public DatasetItem datasetItem { get; set; }

            public class DatasetItem
            {
                public string DatasetName { get; set; }
                public string ItemId { get; set; }
            }
        }

    }

}
