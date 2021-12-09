using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.98")]
            public static void Init_1_0_0_98(IDatabaseUpgrader du)
            {
                string sql = @"


/****** Object:  Table [dbo].[InDocTablesCheck]    Script Date: 10.12.2021 0:00:08 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[InDocTablesCheck](
	[pk] [bigint] IDENTITY(1,1) NOT NULL,
	[created] [datetime] NOT NULL,
	[smlouvaID] [nvarchar](20) NOT NULL,
	[prilohaHash] [nvarchar](90) NOT NULL,
	[page] [int] NOT NULL,
	[tableOnPage] [int] NOT NULL,
	[algorithm] [nvarchar](50) NOT NULL,
	[precalculatedScore] [money] NOT NULL,
	[year] [int] NULL,
	[subjectCheck] [nvarchar](50) not null,
	[algorithmCheck] [nvarchar](50) not null,
 CONSTRAINT [PK_InDocTablesCheck] PRIMARY KEY CLUSTERED 
(
	[pk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] 
GO

ALTER TABLE [dbo].[InDocTablesCheck] ADD  CONSTRAINT [DF_InDocTablesCheck_precalculatedScore]  DEFAULT ((0)) FOR [precalculatedScore]
GO


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
CREATE NONCLUSTERED INDEX IX_InDocTablesCheck_uniq ON dbo.InDocTablesCheck
	(
	smlouvaID,
	prilohaHash,
	page,
	tableOnPage,
	algorithm,
	year,
	subjectCheck
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE dbo.InDocTablesCheck SET (LOCK_ESCALATION = TABLE)
GO
COMMIT


";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
