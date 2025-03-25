using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.128")]
            public static void Init_1_0_0_128(IDatabaseUpgrader du)
            {
                //du.AddColumnToTable("UsePlugin", "int", "UptimeServer", false);

                string sql = @"
ALTER TABLE dbo.RecalculateItemQueue ADD
	Options nvarchar(MAX) NULL

";
                du.RunDDLCommands(sql);
            }
        }
    }
}
