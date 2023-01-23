using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public class BannedIpRepo
    {
        public static List<BannedIp> GetBannedIps()
        {
            using var dbContext = new DbEntities();
            return dbContext.BannedIps.AsNoTracking()
                .Where(b => b.Expiration == null || b.Expiration > DateTime.Now)
                .ToList();
        }

        public static async Task BanIpAsync(string ipAddress, DateTime expiration, int lastStatusCode, string pathList)
        {

            await using var dbContext = new DbEntities();

            var bannedIp = await dbContext.BannedIps
                .AsQueryable()
                .Where(b => b.Ip == ipAddress)
                .FirstOrDefaultAsync();

            if (bannedIp == default)
            {
                bannedIp = new BannedIp()
                {
                    Ip = ipAddress
                };
                dbContext.BannedIps.Add(bannedIp);
            }

            bannedIp.Expiration = expiration;
            bannedIp.Created = DateTime.Now;
            bannedIp.LastStatusCode = lastStatusCode;
            bannedIp.PathList = pathList;

            await dbContext.SaveChangesAsync();
        }

        public static async Task AllowIpAsync(string ipAddress)
        {
            await using var dbContext = new DbEntities();

            var ip = await dbContext.BannedIps
                .AsQueryable()
                .Where(b => b.Ip == ipAddress)
                .FirstOrDefaultAsync();

            dbContext.Remove(ip);
            await dbContext.SaveChangesAsync();
        }

    }
}