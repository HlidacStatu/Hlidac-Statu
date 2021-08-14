using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;

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

        public static async Task BanIp(string ipAddress, DateTime expiration, int lastStatusCode, string pathList)
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
                await dbContext.BannedIps.AddAsync(bannedIp);
            }
            
            bannedIp.Expiration = expiration;
            bannedIp.Created = DateTime.Now;
            bannedIp.LastStatusCode = lastStatusCode;
            bannedIp.PathList = pathList;
            
            await dbContext.SaveChangesAsync();
        }
        
        public static async Task AllowIp(string ipAddress)
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