using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace HlidacStatu.LibCore.Services
{
    public class AttackerDictionaryService
    {
        private class Attacker
        {
            public int Num { get; set; }
            public DateTime Last { get; set; }

            public List<string?> Paths { get; set; } = new List<string?>();

            public Attacker(DateTime last, int num)
            {
                Last = last;
                Num = num;
            }
        }

        private ConcurrentDictionary<string, Attacker> Attackers { get; } = new();

        const int SaveTimeBetweentAttacksInSec = 15 * 60; //inSec
        const int PenaltyLimit = 1500; //inSec
        public static string[] whitelistedIps = new string[] { "77.93.208.131", "217.31.202.16", "89.22.68.163" };

        public bool IsAttacker(IPAddress? ipAddress, int statusCode, string? path)
        {
            if (statusCode < 400)
                return false;

            var ipString = IpToString(ipAddress);
            if (ipString.StartsWith("10.10") || whitelistedIps.Contains(ipString))
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
            else if (statusCode >= 400 && path.StartsWith("/api"))
                penalty = 1; // not found, forbidden, ... in API
            else if (statusCode >= 400)
                penalty = 10; // not found, forbidden, ...
            else
                penalty = 0;


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

            return attacker.Num > PenaltyLimit;
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