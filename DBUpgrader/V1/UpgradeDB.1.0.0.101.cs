using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.101")]
            public static void Init_1_0_0_101(IDatabaseUpgrader du)
            {
                string sql = @"
/****** Object:  Table [dbo].[CenyCustomer]    Script Date: 19.01.2022 10:32:19 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CenyCustomer]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[CenyCustomer](
	[Username] [nvarchar](256) NOT NULL,
	[Analyza] [nvarchar](50) NOT NULL,
	[Rok] [int] NOT NULL,
	[Created] [datetime] NOT NULL,
	[Paid] [datetime] NULL,
 CONSTRAINT [PK_CenyCustomer] PRIMARY KEY CLUSTERED 
(
	[Username] ASC,
	[Analyza] ASC,
	[Rok] ASC
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
