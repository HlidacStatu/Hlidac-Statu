using DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {

        private partial class UpgradeDB
        {

            [DatabaseUpgradeMethod("1.0.0.56")]
            public static void Init_1_0_0_56(IDatabaseUpgrader du)
            {
                du.AddColumnToTable("IP", "nvarchar(15)", "Audit", true);


            }




        }

    }
}
