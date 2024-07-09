using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HlidacStatu.DS.Api.AITask
{

    public class Task
    {
        private static readonly ILogger _logger = Log.ForContext<Task>();

        public long QId { get; set; }
        public int Priority { get; set; } = 10;
        public string CallerId { get; set; }
        public string CallerTaskId { get; set; }
        public string CallerTaskType { get; set; }


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
        public bool force { get; set; } = false;


    }
}
