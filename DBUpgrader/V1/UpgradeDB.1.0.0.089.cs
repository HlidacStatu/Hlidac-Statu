using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.89")]
            public static void Init_1_0_0_89(IDatabaseUpgrader du)
            {
                string sql = @"
Alter table UcetniJednotka add Id int IDENTITY not null ;
Alter table UcetniJednotka add CONSTRAINT pk_UcetniJednotka_ID primary key(Id);
";
                du.RunDDLCommands(sql);
            }
        }
    }
}
