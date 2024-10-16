﻿using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {

        private partial class UpgradeDB
        {

            [DatabaseUpgradeMethod("1.0.0.77")]
            public static void Init_1_0_0_77(IDatabaseUpgrader du)
            {

                string sql = @"BEGIN TRANSACTION
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
CREATE TABLE dbo.SSMQ
	(
	pk int NOT NULL,
	qname nvarchar(255) NOT NULL,
	itemid nvarchar(500) NOT NULL,
	itemstatus int NOT NULL,
	itemvalue nvarchar(MAX) NOT NULL,
	created datetime NOT NULL,
	createdby nvarchar(255) NOT NULL,
	priority int NOT NULL,
	changed datetime NOT NULL,
	changedBy nvarchar(255) NOT NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE dbo.SSMQ ADD CONSTRAINT
	DF_SSMQ_created DEFAULT GetDate() FOR created
GO
ALTER TABLE dbo.SSMQ ADD CONSTRAINT
	DF_SSMQ_priority DEFAULT 1 FOR priority
GO
ALTER TABLE dbo.SSMQ ADD CONSTRAINT
	PK_SSMQ PRIMARY KEY CLUSTERED 
	(
	pk
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
CREATE NONCLUSTERED INDEX IX_qname ON dbo.SSMQ
	(
	qname
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_created_priority ON dbo.SSMQ
	(
	created
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE dbo.SSMQ SET (LOCK_ESCALATION = TABLE)
GO
COMMIT";
                du.RunDDLCommands(sql);

            }
        }
    }
}
