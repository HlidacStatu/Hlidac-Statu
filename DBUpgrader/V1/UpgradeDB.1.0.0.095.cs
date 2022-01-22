using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.95")]
            public static void Init_1_0_0_95(IDatabaseUpgrader du)
            {
                string sql = @"
/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.Firma
	DROP CONSTRAINT DF_Firma_VersionUpdate
GO
CREATE TABLE dbo.Tmp_Firma
	(
	ICO nvarchar(30) NOT NULL,
	DIC nvarchar(30) NULL,
	Datum_zapisu_OR date NULL,
	Stav_subjektu tinyint NULL,
	Jmeno nvarchar(500) NULL,
	Kod_PF int NULL,
	VersionUpdate int NOT NULL,
	Esa2010 nvarchar(50) NULL,
	Source nvarchar(100) NULL,
	Popis nvarchar(100) NULL,
	JmenoAscii nvarchar(500) NULL,
	IsInRS smallint NULL,
	KrajId nvarchar(5) NULL,
	OkresId nvarchar(7) NULL,
	status int NULL,
	Typ int NULL,
	PocetZam int NULL,
	KodOkresu nvarchar(20) NULL,
	ICZUJ nvarchar(20) NULL,
	KODADM nvarchar(20) NULL,
	Adresa nvarchar(500) NULL,
	PSC nvarchar(50) NULL,
	Obec nvarchar(150) NULL,
	CastObce nvarchar(150) NULL,
	Ulice nvarchar(500) NULL,
	CisloDomu nvarchar(50) NULL,
	CisloOrientacni nvarchar(50) NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_Firma SET (LOCK_ESCALATION = TABLE)
GO
ALTER TABLE dbo.Tmp_Firma ADD CONSTRAINT
	DF_Firma_VersionUpdate DEFAULT ((0)) FOR VersionUpdate
GO
ALTER TABLE dbo.Tmp_Firma ADD CONSTRAINT
	DF_Firma_PocetZam DEFAULT 0 FOR PocetZam
GO
IF EXISTS(SELECT * FROM dbo.Firma)
	 EXEC('INSERT INTO dbo.Tmp_Firma (ICO, DIC, Datum_zapisu_OR, Stav_subjektu, Jmeno, Kod_PF, VersionUpdate, Esa2010, Source, Popis, JmenoAscii, IsInRS, KrajId, OkresId, status, Typ)
		SELECT ICO, DIC, Datum_zapisu_OR, Stav_subjektu, Jmeno, Kod_PF, VersionUpdate, CONVERT(nvarchar(50), Esa2010), Source, Popis, JmenoAscii, IsInRS, KrajId, OkresId, status, Typ FROM dbo.Firma WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.Firma
GO
EXECUTE sp_rename N'dbo.Tmp_Firma', N'Firma', 'OBJECT' 
GO
ALTER TABLE dbo.Firma ADD CONSTRAINT
	PK_Firma PRIMARY KEY CLUSTERED 
	(
	ICO
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
CREATE NONCLUSTERED INDEX IX_Firma_Ico ON dbo.Firma
	(
	ICO
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX idx_firma_jmenoascii ON dbo.Firma
	(
	JmenoAscii
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX ix_firma_jmeno ON dbo.Firma
	(
	Jmeno
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
COMMIT
";
                
                
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
