using System;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    public class CamelotStatistics
    {
        public long MaxThreads { get; set; }
        public long Calls { get; set; }
        public long ParsedFiles { get; set; }
        public DateTime Started { get; set; }
        public long CurrentThreads { get; set; }
        public long SessionsOnDisk { get; set; }
        public long FilesOnDisk { get; set; }
        public long FilesOnDiskSize { get; set; }

        public string Version { get; set; }

    }
}
