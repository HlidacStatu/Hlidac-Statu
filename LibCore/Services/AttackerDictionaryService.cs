using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        const int SaveTimeBetweentAttacksInSec = 15*60; //inSec
        const int PenaltyLimit = 150; //inSec

        public bool IsAttacker(IPAddress? ipAddress, int statusCode, string? path)
        {
            if (statusCode < 400)
                return false;

            var ipString = IpToString(ipAddress);
            
            var currentTime = DateTime.Now;
            Attackers.TryAdd(ipString, new Attacker(currentTime, 0));

            if (!Attackers.TryGetValue(ipString, out var attacker))
            {
                return false;
            }
            
            var diff = (currentTime - attacker.Last).TotalSeconds;

            //calculatePenalty
            int penalty = statusCode switch
            {
                466 => 5, // hacker, možná použít Hlidac.ErrorCodes enum pro chybové kódy
                >= 500 => 2, // server errors
                >= 400 => 1, // not found, forbidden, ...
                _ => 0
            };


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