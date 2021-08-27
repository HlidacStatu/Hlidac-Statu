using DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
	{
		private partial class UpgradeDB
		{
			[DatabaseUpgradeMethod("1.0.0.90")]
			public static void Init_1_0_0_90(IDatabaseUpgrader du)
			{
				string sql = @"

/****** Object:  Table [dbo].[InDocJobs]    Script Date: 26.08.2021 21:51:32 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InDocJobs]') AND type in (N'U'))
DROP TABLE [dbo].[InDocJobs]
GO

/****** Object:  Table [dbo].[InDocJobs]    Script Date: 26.08.2021 21:51:32 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[InDocJobs](
	[pk] [bigint] IDENTITY(1,1) NOT NULL,
	[tablePK] [bigint] NOT NULL,
	[jobRaw] [nvarchar](300) NOT NULL,
	[jobGrouped] [nvarchar](300) NULL,
	[salaryMD] [money] NULL,
	[salaryMDVat] [money] NULL,
	[created] [datetime] NULL,
	[tags] [nvarchar](max) NULL,
 CONSTRAINT [PK_InDocJobs] PRIMARY KEY CLUSTERED 
(
	[pk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


USE [Firmy]
GO

ALTER TABLE [dbo].[InDocTables] DROP CONSTRAINT [DF_InDocTables_status]
GO

ALTER TABLE [dbo].[InDocTables] DROP CONSTRAINT [DF_InDocTables_precalculatedScore]
GO

/****** Object:  Table [dbo].[InDocTables]    Script Date: 27.08.2021 5:56:41 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InDocTables]') AND type in (N'U'))
DROP TABLE [dbo].[InDocTables]
GO

/****** Object:  Table [dbo].[InDocTables]    Script Date: 27.08.2021 5:56:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[InDocTables](
	[pk] [bigint] IDENTITY(1,1) NOT NULL,
	[created] [datetime] NOT NULL,
	[smlouvaID] [nvarchar](20) NOT NULL,
	[prilohaHash] [nvarchar](90) NOT NULL,
	[page] [int] NOT NULL,
	[json] [nvarchar(MAX)] NOT NULL,
	[tableOnPage] [int] NOT NULL,
	[algorithm] [nvarchar](50) NOT NULL,
	[precalculatedScore] [money] NOT NULL,
	[preFoundRows] [int] NULL,
	[preFoundCols] [int] NULL,
	[preFoundJobs] [int] NULL,
	[status] [int] NOT NULL,
	[checkedBy] [nvarchar](250) NULL,
	[checkedDate] [datetime] NULL,
	[note] [nvarchar](max) NULL,
	[tags] [nvarchar](max) NULL,
	[checkElapsedInMs] [int] NULL,
 CONSTRAINT [PK_InDocTables] PRIMARY KEY CLUSTERED 
(
	[pk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[InDocTables] ADD  CONSTRAINT [DF_InDocTables_precalculatedScore]  DEFAULT ((0)) FOR [precalculatedScore]
GO

ALTER TABLE [dbo].[InDocTables] ADD  CONSTRAINT [DF_InDocTables_status]  DEFAULT ((0)) FOR [status]
GO



";
				du.RunDDLCommands(sql);
			}
		}
	}
}
