using System;
using HlidacStatu.Entities;
using Devmasters;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Entities.Views;
using Microsoft.EntityFrameworkCore;


namespace HlidacStatu.Repositories
{
    public partial class CenyCustomerRepo
    {

        public static async Task<bool> HasAccessAsync(string username, string analyza, int rok)
        {
            username = username?.Trim()?.ToLower();
            analyza = analyza?.Trim()?.ToUpper();

            if (analyza =="DEMO" && rok == 2018)
                return true;

            using (var db = new DbEntities())
            {
                return await db.CenyCustomer.AsQueryable()
                    .AnyAsync(m=>m.Username == username && m.Analyza == analyza && m.Rok == rok && m.Paid.HasValue);
            }
        }

        
        
    }
}