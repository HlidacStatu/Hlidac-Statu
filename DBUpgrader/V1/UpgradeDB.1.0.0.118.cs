using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.118")]
            public static void Init_1_0_0_118(IDatabaseUpgrader du)
            {
                du.AddColumnToTable("ModifiedBy", "nvarchar(150)", "OsobaEvent", true);
                du.AddColumnToTable("Modified", "datetime", "OsobaEvent", true);
            }
        }
    }
}
