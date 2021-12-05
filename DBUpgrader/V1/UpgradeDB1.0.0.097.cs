using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.97")]
            public static void Init_1_0_0_97(IDatabaseUpgrader du)
            {
                string sql = @"

/****** Object:  Table [dbo].[InDocJobNameDescription]    Script Date: 22.11.2021 19:55:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[InDocJobNameDescription](
	[pk] [int] IDENTITY(1,1) NOT NULL,
	[jobGrouped] [nvarchar](max) NOT NULL,
	[subject] [nvarchar](100) NOT NULL,
	[jobGroupedDescription] [nvarchar](max) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO



";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
