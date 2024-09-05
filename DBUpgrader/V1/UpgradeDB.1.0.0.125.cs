using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.125")]
            public static void Init_1_0_0_125(IDatabaseUpgrader du)
            {
                string sql = @"CREATE TABLE SmlouvaVerejnaZakazka (
                    VzId NVARCHAR(255) NOT NULL,
                    IdSmlouvy NVARCHAR(255) NOT NULL,
                    CosineSimilarity FLOAT NOT NULL,
                    ModifiedDate DATETIME NOT NULL,
                    PRIMARY KEY (VzId, IdSmlouvy)
                );

                CREATE INDEX IX_SmlouvaVerejnaZakazka_VzId ON SmlouvaVerejnaZakazka (VzId);";
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
