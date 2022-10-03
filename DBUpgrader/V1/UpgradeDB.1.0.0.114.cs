using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.114")]
            public static void Init_1_0_0_114(IDatabaseUpgrader du)
            {
                string sql = @"
Drop table dbo.ConfigurationValues;
CREATE TABLE dbo.ConfigurationValues
(
    Id int IDENTITY(1,1) PRIMARY KEY, 
    KeyName nvarchar(200) NOT NULL,
    KeyValue nvarchar(max),
    Environment nvarchar(30),
    Tag nvarchar(30)
);";
                du.RunDDLCommands(sql);
            }
        }
    }
}
