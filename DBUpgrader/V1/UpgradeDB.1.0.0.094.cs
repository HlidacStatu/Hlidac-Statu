using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.94")]
            public static void Init_1_0_0_94(IDatabaseUpgrader du)
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
CREATE TABLE dbo.Tmp_SmlouvyDodavatele
	(
	SmlouvaId nvarchar(50) NOT NULL,
	Ico nvarchar(30) NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.Tmp_SmlouvyDodavatele SET (LOCK_ESCALATION = TABLE)
GO
IF EXISTS(SELECT * FROM dbo.SmlouvyDodavatele)
	 EXEC('INSERT INTO dbo.Tmp_SmlouvyDodavatele (SmlouvaId, Ico)
		SELECT SmlouvaId, Ico FROM dbo.SmlouvyDodavatele WITH (HOLDLOCK TABLOCKX)')
GO
DROP TABLE dbo.SmlouvyDodavatele
GO
EXECUTE sp_rename N'dbo.Tmp_SmlouvyDodavatele', N'SmlouvyDodavatele', 'OBJECT' 
GO
ALTER TABLE dbo.SmlouvyDodavatele ADD CONSTRAINT
	PK_SmlouvyDodavatele PRIMARY KEY CLUSTERED 
	(
	SmlouvaId,
	Ico
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
COMMIT


USE [Firmy]
GO
/****** Object:  StoredProcedure [dbo].[SmlouvaId_Save]    Script Date: 05.10.2021 17:40:03 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[SmlouvaId_Save] 
	@id nvarchar(50),
    @active int,
	@created datetime = null,
	@updated datetime = null,
	@icoOdberatele nvarchar(30) = null
AS
BEGIN
	SET NOCOUNT ON;
	declare @dt datetime;
	set @dt = getdate();

if (@created is null)
	set @created=@dt;
if (@updated is null)
	set @updated=@dt;

set transaction isolation level serializable
begin transaction

	IF EXISTS(SELECT id FROM SmlouvyIds with (updlock) 
			  WHERE id=@id)
	BEGIN
		update SmlouvyIds
		set updated = @updated, active = @active, IcoOdberatele = @icoOdberatele
		where id=@id
	END
	ELSE
	BEGIN
		insert into SmlouvyIds(id, created, updated, active, IcoOdberatele) values(@id, @created,@updated,@active,@icoOdberatele)
	END
commit

END
";
                
                
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
