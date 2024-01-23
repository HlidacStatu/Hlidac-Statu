using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.122")]
            public static void Init_1_0_0_122(IDatabaseUpgrader du)
            {
                string sql = @"
Create table PU_Organizace (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Ico nvarchar(30),
    DS nvarchar(50),
    Nazev nvarchar(300),
    Info nvarchar(max),
    HiddenNote nvarchar(max),
    Zatrideni nvarchar(300),
    Oblast nvarchar(300)
);

Create table PU_OrganizaceTags (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrganizace int,
    Tag nvarchar(50)
)

Create table PU_Plat (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrganizace int,
    Rok int,
    NazevPozice nvarchar(300),
    Plat Decimal(19,4),
    Odmeny Decimal(19,4),
    Uvazek Decimal(19,4),
    PocetMesicu Decimal(19,4),
    NefinancniBonus nvarchar(max),
    PoznamkaPozice nvarchar(max),
    PoznamkaPlat nvarchar(max),
    SkrytaPoznamka nvarchar(max),
    JeHlavoun bit,
    DisplayOrder int
);

Create table PU_OranizaceMetadata (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    IdOrganizace int,
    Rok int,
    ZpusobKomunikace int,
    ZduvodneniMimoradnychOdmen bit,
    DatumOdeslaniZadosti date,
    DatumPrijetiOdpovedi date,
    SkrytaPoznamka nvarchar(max)
);

CREATE NONCLUSTERED INDEX IX_PU_Organizace_Ico ON dbo.PU_Organizace (Ico);
CREATE NONCLUSTERED INDEX IX_PU_Organizace_DS ON dbo.PU_Organizace (DS);
CREATE NONCLUSTERED INDEX IX_PU_Organizace_Oblast ON dbo.PU_Organizace (Oblast);
CREATE NONCLUSTERED INDEX IX_PU_Organizace_Zatrideni ON dbo.PU_Organizace (Zatrideni);
CREATE NONCLUSTERED INDEX IX_PU_OrganizaceTags_Tag ON dbo.PU_OrganizaceTags (Tag);
CREATE NONCLUSTERED INDEX IX_PU_OrganizaceTags_IdOrganizace ON dbo.PU_OrganizaceTags (IdOrganizace);
CREATE NONCLUSTERED INDEX IX_PU_Plat_Rok ON dbo.PU_Plat (Rok);
CREATE NONCLUSTERED INDEX IX_PU_Plat_NazevPozice ON dbo.PU_Plat (NazevPozice);
CREATE NONCLUSTERED INDEX IX_PU_Plat_IdOrganizace ON dbo.PU_Plat (IdOrganizace);
CREATE NONCLUSTERED INDEX IX_PU_OranizaceMetadata_IdOrganizace ON dbo.PU_OranizaceMetadata (IdOrganizace);
CREATE NONCLUSTERED INDEX IX_PU_OranizaceMetadata_Rok ON dbo.PU_OranizaceMetadata (Rok);
";
                du.RunDDLCommands(sql);
            }
        }
    }
}
