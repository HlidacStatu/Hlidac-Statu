using System;

namespace HlidacStatu.DS.Api.Voice2Text
{

    public class Task
    {
        public long QId { get; set; }
        public int Priority { get; set; } = 10;
        public string CallerId { get; set; }
        public string CallerTaskId{ get; set; }


        public string Source { get; set; }
        public Options SourceOptions { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Done { get; set; }
        public DateTime Started { get; set; } = DateTime.Now;

        public string Result { get; set; }

        public CheckState Status { get; set; }

        public enum CheckState : int
        {
            WaitingInQueue = 0,
            InProgress = 1,
            Done = 2,
            ResultTaken = 100,
            Error = -1,
        }

    }


    public class Options
    {

        public WordcabTranscribe.SpeechToText.AudioRequestOptions audioOptions { get; set; }
        public bool deleteFileAfterProcess { get; set; }

        public string datasetName { get; set; }
        public string itemId { get; set; }
    }
}
