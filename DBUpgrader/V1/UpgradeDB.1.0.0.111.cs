using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.111")]
            public static void Init_1_0_0_111(IDatabaseUpgrader du)
            {
                string sql = @"
CREATE TABLE dbo.AdresaOvm
(
    Id int NOT NULL PRIMARY KEY,
    UliceNazev nvarchar(100),
    CisloDomovni int NULL,
    ObecNazev nvarchar(100),
    ObecKod int NULL,
    CastObceNeboKatastralniUzemi nvarchar(100),
    PSC nvarchar(8),
    KrajNazev nvarchar(100),
    CisloOrientacni nvarchar(20),
    NestrukturovanaAdresa nvarchar(MAX)
);
CREATE TABLE dbo.AdresniMisto
(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    KodAdm int,
    KodObce int,
    NazevObce nvarchar(100),
    KodMomc int NULL,
    NazevMomc nvarchar(100),
    NazevMop nvarchar(100),
    KodCastiObce int NULL,
    NazevCastiObce nvarchar(100),
    NazevUlice nvarchar(100),
    TypSO nvarchar(50),
    CisloDomovni int,
    CisloOrientacni nvarchar(20),
    ZnakCislaOrientacniho nvarchar(100),
    Psc nvarchar(8),
    CoordX nvarchar(20),
    CoordY nvarchar(20),
    OneLiner nvarchar(MAX),
    CisloVolebnihoOkrsku nvarchar(50)
);
CREATE TABLE dbo.OrganVerejneMoci
(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Zkratka nvarchar(100),
    ICO nvarchar(20),
    Nazev nvarchar(MAX),
    AdresaOvmId int,
    TypOvmId int,
    PravniFormaOvmId nvarchar(20),
    PrimarniOvm nvarchar(50),
    IdDS nvarchar(20),
    TypDS nvarchar(20),
    StavDS int,
    StavSubjektu int,
    DetailSubjektu nvarchar(MAX),
    IdentifikatorOvm nvarchar(50),
    KategorieOvm nvarchar(MAX)
);
CREATE TABLE dbo.PravniFormaOvm
(
    Id nvarchar(20) NOT NULL PRIMARY KEY,
    Text nvarchar(MAX)
);
CREATE TABLE dbo.TypOvm
(
    Id int NOT NULL PRIMARY KEY,
    Text nvarchar(MAX)
);

";
                du.RunDDLCommands(sql);
            }
        }
    }
}
