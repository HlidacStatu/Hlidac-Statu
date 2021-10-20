using System;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    public class CamelotStatistics
    {
        public long MaxThreads { get; set; }
        public long CallsTotal { get; set; }
        public long CallsIn1H { get; set; }
        public long CallsIn24H { get; set; }
        public long ParsedFilesTotal { get; set; }
        public long ParsedFiles1H { get; set; }
        public long ParsedFiles24H { get; set; }

        public DateTime Started { get; set; }
        public long CurrentThreads { get; set; }
        public long SessionsOnDisk { get; set; }
        public long FilesOnDisk { get; set; }
        public long FilesOnDiskSize { get; set; }

        public string Version { get; set; }

    }
}
