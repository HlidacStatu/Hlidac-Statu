using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.119")]
            public static void Init_1_0_0_119(IDatabaseUpgrader du)
            {
                du.AddColumnToTable("DatumZaniku", "datetime", "Firma", true);
            }
        }
    }
}
