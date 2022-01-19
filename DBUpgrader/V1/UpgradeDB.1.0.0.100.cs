using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.100")]
            public static void Init_1_0_0_100(IDatabaseUpgrader du)
            {
                string sql = @"
alter table indocjobs add
    UnitCount decimal(18,9);
	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
