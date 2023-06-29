using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.121")]
            public static void Init_1_0_0_121(IDatabaseUpgrader du)
            {
                du.AddColumnToTable("HeadOfOffice", "bit", "PlatyUredniku", true);
                du.AddColumnToTable("Link", "nvarchar(500)", "PlatyUredniku", true);
            }
        }
    }
}
