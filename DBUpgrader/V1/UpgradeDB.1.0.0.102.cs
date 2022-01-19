using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.102")]
            public static void Init_1_0_0_102(IDatabaseUpgrader du)
            {
                string sql = @"
alter table indoctables add
    Klasifikace nvarchar(100) null,
    KlasifikaceManual nvarchar(100) null,
    Analyza nvarchar(100) null;

alter table indoctables drop column hasteribleformat;
	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
