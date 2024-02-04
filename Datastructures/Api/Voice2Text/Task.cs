using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HlidacStatu.DS.Api.Voice2Text
{

    public class Task
    {
        private static readonly ILogger _logger = Log.ForContext<Task>();

        public long QId { get; set; }
        public int Priority { get; set; } = 10;
        public string CallerId { get; set; }
        public string CallerTaskId { get; set; }


        public string Source { get; set; }
        public Options SourceOptions { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Done { get; set; }
        public DateTime? Started { get; set; } = DateTime.Now;

        public Devmasters.SpeechToText.Term[] Result { get; set; }
        public string Exception { get; set; }

        public CheckState Status { get; set; }

        public enum CheckState : int
        {
            WaitingInQueue = 0,
            InProgress = 1,
            Done = 2,
            ResultTaken = 100,
            Error = -1,
        }

        public string ProcessEngine { get; set; }

    }


    public class Options
    {

        [Description("not in use")]
        public string[]? vocabulary { get; set; } = null;
        [Description("not in use")]
        public bool? diarization { get; set; } = null;

        public string language { get; set; }

        public bool deleteFileAfterProcess { get; set; }

        public string datasetName { get; set; }
        public string itemId { get; set; }
    }
}
