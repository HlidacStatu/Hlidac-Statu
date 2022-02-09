using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.105")]
            public static void Init_1_0_0_105(IDatabaseUpgrader du)
            {
                string sql = @"
/****** Object:  Table [dbo].[UptimeServer]    Script Date: 09.02.2022 15:43:47 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UptimeServer]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[UptimeServer](
	[Id] [nvarchar](30) NOT NULL,
	[Created] [datetime] NOT NULL,
	[PublicUrl] [nvarchar](500) NOT NULL,
	[RealUrl] [nvarchar](500) NULL,
	[AdditionalParams] [nvarchar](max) NULL,
	[Plugin] [nvarchar](300) NULL,
	[Groups] [nvarchar](300) NOT NULL,
	[Name] [nvarchar](300) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[ICO] [nvarchar](30) NOT NULL,
	[Priorita] [int] NOT NULL,
	[IntervalInSec] [int] NOT NULL,
	[LastCheck] [datetime] NULL,
	[LastResponseCode] [money] NULL,
	[LastResponseTimeInMs] [bigint] NULL,
	[LastResponseSize] [bigint] NULL,
	[SSLGrade] [nvarchar](30) NULL,
 CONSTRAINT [PK_UptimeServer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO




	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
