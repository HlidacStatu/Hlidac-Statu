using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities.Views
{
    [Keyless]
    public class UserJobStatistics
    {
        public string User { get; set; }
        public int Status { get; set; }
        public int Count { get; set; }
        public int AverageTime { get; set; }

        public string GetStatusName()
        {
            var state = (InDocTables.CheckState)Status;
            return state.ToString("G");
        }
    }
}