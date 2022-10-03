using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.113")]
            public static void Init_1_0_0_113(IDatabaseUpgrader du)
            {
                string sql = @"
Alter TABLE dbo.ConfigurationValues add environment nvarchar(30);
Alter TABLE dbo.ConfigurationValues add tag nvarchar(30);";
                du.RunDDLCommands(sql);
            }
        }
    }
}
