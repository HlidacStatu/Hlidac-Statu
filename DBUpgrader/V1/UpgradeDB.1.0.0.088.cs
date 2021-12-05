using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.88")]
            public static void Init_1_0_0_88(IDatabaseUpgrader du)
            {
                string sql = @"
Alter table BannedIPs add LastStatusCode int;
Alter table BannedIPs add PathList nvarchar(max);
";
                du.RunDDLCommands(sql);
            }
        }
    }
}
