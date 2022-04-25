using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.110")]
            public static void Init_1_0_0_110(IDatabaseUpgrader du)
            {
                string sql = @"
alter table indoctables drop column subject;
alter table indocjobs drop column salarymd;
alter table indocjobs drop column salarymdvat;
";
                du.RunDDLCommands(sql);
            }
        }
    }
}
