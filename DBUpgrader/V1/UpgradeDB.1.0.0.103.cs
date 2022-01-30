using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.103")]
            public static void Init_1_0_0_103(IDatabaseUpgrader du)
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
EXECUTE sp_rename N'dbo.InDocJobNameDescription.subject', N'Tmp_analyza', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.InDocJobNameDescription.Tmp_analyza', N'analyza', 'COLUMN' 
GO
ALTER TABLE dbo.InDocJobNameDescription ADD
	Classification nvarchar(100) NULL
GO
ALTER TABLE dbo.InDocJobNameDescription SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
