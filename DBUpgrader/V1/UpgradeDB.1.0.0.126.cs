using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.126")]
            public static void Init_1_0_0_126(IDatabaseUpgrader du)
            {
                //du.AddColumnToTable("UsePlugin", "int", "UptimeServer", false);

                string sql = @"
ALTER TABLE dbo.UptimeServer ADD
	UsePlugin int NOT NULL CONSTRAINT DF_UptimeServer_UsePlugin DEFAULT 1

";
                //du.RunDDLCommands(sql);
            }
        }
    }
}
