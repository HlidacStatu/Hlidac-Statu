using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.120")]
            public static void Init_1_0_0_120(IDatabaseUpgrader du)
            {
                string sql = @"
CREATE TABLE [dbo].[PlatyUredniku](
	[pk] [bigint] IDENTITY(1,1) PRIMARY KEY,
	[Ico] [nvarchar](30),
	[DruhInstituce] [nvarchar](500),
    [NazevInstituce] [nvarchar](500),
    [Pozice] [nvarchar](500),
    [NazevPlatu] [nvarchar](500),
    [rok] [int], 
    [plat] [decimal],
    [odmeny] [decimal],
    [pocetmes] [int],
    [bonus] [decimal],
    [nefbonus] [nvarchar](max)
    );

CREATE NONCLUSTERED INDEX IX_platyuredniku ON dbo.PlatyUredniku (Ico); 
";
                du.RunDDLCommands(sql);
            }
        }
    }
}
