using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.115")]
            public static void Init_1_0_0_115(IDatabaseUpgrader du)
            {
                string sql = @"
ALTER TABLE dbo.ConfigurationValues ADD CONSTRAINT uniqueKeys UNIQUE (KeyName, Environment, Tag);";
                du.RunDDLCommands(sql);
            }
        }
    }
}
