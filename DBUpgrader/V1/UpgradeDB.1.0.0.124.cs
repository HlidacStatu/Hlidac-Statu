using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.124")]
            public static void Init_1_0_0_124(IDatabaseUpgrader du)
            {
                string sql = @"
alter table dbo.InDocJobs add jobGrouped2 nvarchar(300);
alter table dbo.InDocJobs add jobGrouped3 nvarchar(300);";
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
