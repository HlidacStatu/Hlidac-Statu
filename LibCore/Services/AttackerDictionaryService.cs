using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace HlidacStatu.LibCore.Services
{
    public class AttackerDictionaryService
    {

        // private class RadwareNetwork : Devmasters.Net.Crawlers.CrawlerBase
        // {
        //     public override string Name => "RadwareNetwork";
        //
        //     public override string[] IP => new string[] {
        //         "141.226.101.0/24",
        //         "66.22.0.0/17",
        //         "159.122.76.110",
        //         "141.226.97.0/24"
        //     };
        //
        //     public override string[] HostNameRegex => null;
        //
        //     public override string[] UserAgent => null;
        //
        //     public override async Task<bool> ReloadDefinitionsFromInternetAsync()
        //     {
        //         return true;
        //     }
        // }
        //
        // static Devmasters.Net.Crawlers.ICrawler radware = new RadwareNetwork();
        // private Devmasters.Net.Crawlers.ICrawler _radware = new RadwareNetwork();


        private class Attacker
        {
            public int Num { get; set; }
            public DateTime Last { get; set; }

            public List<string?> Paths { get; set; } = new List<string?>();
            public readonly object SyncLock = new object();

            public Attacker(DateTime last, int num)
            {
                Last = last;
                Num = num;
            }
        }

        private ConcurrentDictionary<string, Attacker> Attackers { get; } = new();

        const int SaveTimeBetweentAttacksInSec = 15 * 60; //inSec
        const int PenaltyLimit = 1500; //inSec

        public bool IsAttacker(IPAddress? ipAddress, int statusCode, string? path)
        {
            if (statusCode < 400)
                return false;

            //whitelisted Radware Ips
            // if (radware.IsItCrawler(IpToString(ipAddress), ""))
            //     return false;

            var ipString = IpToString(ipAddress);
            if (ipString.StartsWith("10.10"))
                return false;

            var currentTime = DateTime.Now;
            Attackers.TryAdd(ipString, new Attacker(currentTime, 0));

            if (!Attackers.TryGetValue(ipString, out var attacker))
            {
                return false;
            }

            var diff = (currentTime - attacker.Last).TotalSeconds;

            //calculatePenalty
            int penalty = 0;

            if (statusCode == 466)
                penalty = 50;
            else if (path == "/health")
                penalty = 0;
            else if (statusCode >= 500)
                penalty = 20; // server errors
            else if (statusCode >= 400 && path != null && path.StartsWith("/api"))
                penalty = 1; // not found, forbidden, ... in API
            else
                penalty = 10; // not found, forbidden, ...

            bool isAttacker;
            lock (attacker.SyncLock)
            {
                if (diff >= SaveTimeBetweentAttacksInSec && penalty > 0)
                {
                    attacker.Num = penalty;
                    attacker.Paths.Clear();
                }
                else if (penalty > 0)
                {
                    attacker.Num += penalty;
                }
                attacker.Paths.Add(path);
                isAttacker = attacker.Num > PenaltyLimit;
            }
            
            return isAttacker;
        }

        public string PathsForIp(IPAddress? ipAddress)
        {
            var ipString = IpToString(ipAddress);
            if (Attackers.TryGetValue(ipString, out var attacker))
            {
                return string.Join("\n", attacker.Paths);
            }

            return "";
        }

        private string IpToString(IPAddress? ipAddress)
        {
            return ipAddress?.ToString() ?? "_empty";
        }


    }

}