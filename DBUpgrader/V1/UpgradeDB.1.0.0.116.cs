using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.116")]
            public static void Init_1_0_0_116(IDatabaseUpgrader du)
            {
                du.AddColumnToTable("Options", "nvarchar(max)", "ItemToOcrQueue", true);
            }
        }
    }
}
