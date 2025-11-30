using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api
{
    public class WinSystemMonitorData
    {
        public string MonitorVersion { get; set; }
        public DateTime Created { get; set; }
        public decimal CPU { get; set; }
        public long UsedMemoryBytes { get; set; }
        public long TotalMemoryBytes { get; set; }
        public long SystemUpTimeSeconds { get; set; }
        public long SystemDiskFreeBytes { get; set; }
        public long SystemDiskTotalBytes { get; set; }
        public IIS.Request[] IISRequests { get; set; }
        public class IIS
        {
            public Request[] Requests { get; set; }

            public class Request
            {
                /*
                 Name	Value	Description
Unknown	0	IIS 7 is in an unknown state.
BeginRequest	1	IIS 7 began processing a request.
AuthenticateRequest	2	IIS 7 authenticated a request.
AuthorizeRequest	4	IIS 7 authorized a request.
ResolveRequestCache	8	IIS 7 satisfied a request from the cache.
MapRequestHandler	16	IIS 7 mapped the request handler.
AcquireRequestState	32	IIS 7 acquired the state for a request.
PreExecuteRequestHandler	64	IIS 7 executed a request handler.
ExecuteRequestHandler	128	IIS 7 executed a request handler.
ReleaseRequestState	256	IIS 7 released the state for a request.
UpdateRequestCache	512	IIS 7 updated the cache.
LogRequest	1024	IIS 7 logged the request.
EndRequest	2048	IIS 7 ended a request.
SendResponse	536870912	IIS 7 sent a response. The event that is associated with this pipeline state is nondeterministic.
                 */
                public enum PipelineStateEnum
                {
                    Unknown = 0,
                    BeginRequest = 1,
                    AuthenticateRequest = 2,
                    AuthorizeRequest = 4,
                    ResolveRequestCache = 8,
                    MapRequestHandler = 0x10,
                    AcquireRequestState = 0x20,
                    PreExecuteRequestHandler = 0x40,
                    ExecuteRequestHandler = 0x80,
                    ReleaseRequestState = 0x100,
                    UpdateRequestCache = 0x200,
                    LogRequest = 0x400,
                    EndRequest = 0x800,
                    SendResponse = 0x20000000
                }

                private int _processId;

                public string ClientIPAddr { get; set; }

                public string ConnectionId { get; set; }

                public string CurrentModule { get; set; }

                public string HostName { get; set; }

                public string LocalIPAddress { get; set; }

                public int LocalPort { get; set; }

                public PipelineStateEnum PipelineState { get; set; }

                public int ProcessId { get; set; }

                public string RequestId { get; set; }

                public int SiteId { get; set; }
                public string ApplicationPoolName { get; set; }

                public int TimeElapsedInMs { get; set; }

                public int TimeInModuleInMs { get; set; }

                public int TimeInStateInMs { get; set; }

                public string Url { get; set; }

                public string Verb { get; set; }
            }
        }
    }
}
