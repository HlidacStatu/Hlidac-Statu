using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Entities.Views;
using Microsoft.EntityFrameworkCore;

namespace JobTableEditor.Data
{
    public class StatisticsService
    {
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