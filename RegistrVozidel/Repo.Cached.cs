using HlidacStatu.Caching;
using HlidacStatu.RegistrVozidel.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;
using HlidacStatu.Caching;

namespace HlidacStatu.RegistrVozidel
{
    public static partial class Repo
    {

        public static class Cached
        {
            private static readonly ILogger _logger = Serilog.Log.ForContext(typeof(Cached));
            internal static IFusionCache Cache = null;

            static Cached()
            {
                try
                {
                    Cache = HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(Cached));

                }
                catch (Exception e)
                {

                    _logger.Fatal(e, "Cannot create cache in RegistrVozidel.Repo.Cached");
                    //throw;
                }

            }
            public static ValueTask<List<Models.VozidloLight>> GetForICOAsync(string ico,
                Enums.Vztah_k_vozidluEnum? vztah = null
                )
                => Cache.GetOrSetAsync($"_GetForICOAsync_{ico}_{vztah}", async _ =>
                {
                    return await Repo.GetForICOAsync(ico, vztah);
                },
                    options =>
                    {
                        options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12));
                        options.ModifyEntryOptionsFactoryTimeouts(factoryHardTimeout: TimeSpan.FromMinutes(10));
                    });
        }
    }
}