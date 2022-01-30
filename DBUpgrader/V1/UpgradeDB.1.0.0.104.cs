using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.104")]
            public static void Init_1_0_0_104(IDatabaseUpgrader du)
            {
                string sql = @"
/****** Object:  Table [dbo].[InDocTags]    Script Date: 28.01.2022 23:48:07 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InDocTags]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[InDocTags](
	[pk] [int] IDENTITY(1,1) NOT NULL,
	[keyword] [nvarchar](500) NOT NULL,
	[tag] [nvarchar](500) NOT NULL,
	[analyza] [nvarchar](150) NOT NULL,
 CONSTRAINT [PK_InDocTags] PRIMARY KEY CLUSTERED 
(
	[pk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO


	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
