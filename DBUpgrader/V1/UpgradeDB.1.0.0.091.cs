using DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.91")]
            public static void Init_1_0_0_91(IDatabaseUpgrader du)
            {
                string sql = @"

ALTER TABLE dbo.InDocTables ADD
	subject nvarchar(100) NULL



";
                du.RunDDLCommands(sql);
            }
        }
    }
}
