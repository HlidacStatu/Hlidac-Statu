﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Entities
{
    public class BlurredPageStatistics
    {
        public long total { get; set; }
        public long currTaken { get; set; }
        public long totalFailed { get; set; }

        public long savedInThread { get; set; }
        public long runningSaveThreads { get; set; }
        public long savingPagesInThreads { get; set; }

        public perItemStat<long>[] activeTasks { get; set; }
        public perItemStat<decimal>[] avgTaskLegth { get; set; }
        public perItemStat<decimal>[] longestTasks { get; set; }
        public class perItemStat<T>
        {
            public string email { get; set; }
            public T count { get; set; }

        }

    }
}