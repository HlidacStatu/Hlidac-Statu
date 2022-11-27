using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Nest;

namespace HlidacStatu.DS.Api
{
    public class DockerSwarmHealthCheckResult
    {
        public class Node
        {
            public string Name { get; set; }
            public string Availability { get; set; }
            public string Role { get; set; }
            public string State { get; set; }
            public string ManagerState { get; set; }
            public string IPAddress { get; set; }
            public string[] Labels { get; set; }
            public decimal CPUs { get; set; }
            public long MemoryBytes { get; set; }
            public string Architecture { get; set; }
            public string DockerVersion { get; set; }
            public string OS { get; set; }
        }
 
        public class Service
        {
            public class PortConf
            {
                public string Protocol { get; set; }

                public uint TargetPort { get; set; }

                public uint PublishedPort { get; set; }

                public string PublishMode { get; set; }

                public override string ToString()
                {
                    return $"{PublishMode} {Protocol} {TargetPort}=>{PublishedPort}";
                }

            }
            public string Name { get; set; }
            public int Replicas { get; set; }
            public int RunningReplicas { get; set; }
            public PortConf[] Ports { get; set; }
        }
        public Node[] Nodes { get; set; }
        public Service[] Services { get; set; }
        //public Stack[] Stacks { get; set; }
    }
}
