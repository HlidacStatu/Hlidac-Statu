﻿using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {

        private partial class UpgradeDB
        {

            [DatabaseUpgradeMethod("1.0.0.78")]
            public static void Init_1_0_0_78(IDatabaseUpgrader du)
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
ALTER TABLE dbo.DumpData ADD
	den int NULL
GO
ALTER TABLE dbo.DumpData ADD CONSTRAINT
	DF_DumpData_den DEFAULT 0 FOR den
GO
ALTER TABLE dbo.DumpData SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
";
                du.RunDDLCommands(sql);

            }
        }
    }
}
