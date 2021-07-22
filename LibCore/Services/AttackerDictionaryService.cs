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

            if (diff >= 600)
            {
                attacker.Num = 1;
                attacker.Paths.Clear();
            }
            else
            {
                attacker.Num += 1;
            }
            attacker.Paths.Add(path);

            return statusCode switch
            {
                //migrace: přidat generování 466 response do error controlleru v případě problematických requestů podle global.asax
                466 => attacker.Num > 10, // hacker, možná použít Hlidac.ErrorCodes enum pro chybové kódy
                >=500 => attacker.Num > 15, // server errors
                >=400 => attacker.Num > 20, // not found, forbidden, ...
                _ => false
            };
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