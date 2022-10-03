using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.112")]
            public static void Init_1_0_0_112(IDatabaseUpgrader du)
            {
                string sql = @"
CREATE TABLE dbo.ConfigurationValues
(
    Id nvarchar(200) NOT NULL PRIMARY KEY,
    Value nvarchar(max)    
);";
                du.RunDDLCommands(sql);
            }
        }
    }
}
