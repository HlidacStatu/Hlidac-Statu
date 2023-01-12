using Devmasters.DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.117")]
            public static void Init_1_0_0_117(IDatabaseUpgrader du)
            {
                string sql = @"

/****** Object:  Table [dbo].[MonitoredTasks]    Script Date: 11.01.2023 16:58:59 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MonitoredTasks]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[MonitoredTasks](
	[pk] [bigint] IDENTITY(1,1) NOT NULL,
	[application] [nvarchar](200) NOT NULL,
	[part] [nvarchar](500) NOT NULL,
	[itemupdated] [datetime] NOT NULL,
	[started] [datetime] NULL,
	[finished] [datetime] NULL,
	[progress] [smallmoney] NULL,
	[success] [bit] NULL,
	[exception] [nvarchar](max) NULL,
	[callingstack] [nvarchar](max) NULL,
 CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED 
(
	[pk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO




";
                du.RunDDLCommands(sql);
            }
        }
    }
}
