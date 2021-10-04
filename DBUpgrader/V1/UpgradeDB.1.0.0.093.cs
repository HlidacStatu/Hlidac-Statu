using DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.93")]
            public static void Init_1_0_0_93(IDatabaseUpgrader du)
            {
                string sql = @"


CREATE TABLE SmlouvyDodavatele(
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SmlouvaId] [nvarchar](50) NOT NULL,
	[Ico] [nvarchar](30) NULL
	);

ALTER TABLE SmlouvyIds ADD
	IcoOdberatele [nvarchar](30) NULL;
";
                
                
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
