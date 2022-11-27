using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Entities.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HlidacStatu.JobTableEditor.Data
{
    public class StatisticsService
    {
        ILogger<JobService> _logger;

        public StatisticsService(ILogger<JobService> logger)
        {
            _logger = logger;
        }
        
        
        public async Task<List<UserJobStatistics>> UserStatisticsAsync(DateTime date,
            CancellationToken cancellationToken = default)
        {
            
            await using var db = new DbEntities();
            var result = await db.UserJobStatistics.FromSqlInterpolated($@"
                SELECT t.checkedBy 'user', t.status, COUNT(t.pk) 'count', AVG(t.checkElapsedInMs) 'averageTimeInMs'
                FROM InDocTables t
                where checkedDate >= {date.ToString("yyyy-MM-dd")}
                and t.status >= 2
                group by t.checkedBy, t.status
                order by t.checkedBy, t.status")
                .ToListAsync(cancellationToken);

            return result;
        }
        
        public Task<List<(string Klasifikace, int Pocet)>> WaitingInQueuesAsync(
            CancellationToken cancellationToken = default) 
            => InDocTablesRepo.WaitingInAllQueuesAsync(cancellationToken);
    }


    // assembled object from loaded data and some meta data which are going to be pushed on server
}