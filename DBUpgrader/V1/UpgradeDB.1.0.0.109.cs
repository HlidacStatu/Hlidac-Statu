using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.109")]
            public static void Init_1_0_0_109(IDatabaseUpgrader du)
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
CREATE TABLE dbo.Tmp_CenyCustomer
	(
	Username nvarchar(256) NOT NULL,
	Analyza nvarchar(50) NOT NULL,
	Rok int NOT NULL,
	Created datetime NOT NULL,
	Paid datetime NULL,
	[Level] int NOT NULL,
	Amount money NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_CenyCustomer SET (LOCK_ESCALATION = TABLE)
GO
ALTER TABLE dbo.Tmp_CenyCustomer ADD CONSTRAINT
	DF_CenyCustomer_Level DEFAULT 0 FOR [Level]
GO
IF EXISTS(SELECT * FROM dbo.CenyCustomer)
	 EXEC('INSERT INTO dbo.Tmp_CenyCustomer (Username, Analyza, Rok, Created, Paid, [level],amount)
		SELECT Username, Analyza, Rok, Created, Paid,2,199000 FROM dbo.CenyCustomer WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.CenyCustomer
GO
EXECUTE sp_rename N'dbo.Tmp_CenyCustomer', N'CenyCustomer', 'OBJECT' 
GO
ALTER TABLE dbo.CenyCustomer ADD CONSTRAINT
	PK_CenyCustomer PRIMARY KEY CLUSTERED 
	(
	Username,
	Analyza,
	Rok
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
COMMIT
";
                du.RunDDLCommands(sql);
            }
        }
    }
}
