using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.123")]
            public static void Init_1_0_0_123(IDatabaseUpgrader du)
            {
                string sql = @"
drop index IX_PU_Organizace_Ico on dbo.PU_Organizace;
drop index IX_PU_Organizace_Zatrideni on dbo.PU_Organizace;
drop index IX_PU_Organizace_Podoblast on dbo.PU_Organizace;
alter table dbo.PU_Organizace drop column Ico;

alter table dbo.PU_Organizace drop column Nazev;
         
create index Firma_DS_DatovaSchranka_index on dbo.Firma_DS (DatovaSchranka);
";
                du.RunDDLCommands(sql);
            }
        }
    }
}
