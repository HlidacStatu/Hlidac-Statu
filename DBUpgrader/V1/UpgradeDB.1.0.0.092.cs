using DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.92")]
            public static void Init_1_0_0_92(IDatabaseUpgrader du)
            {
                string sql = @"


CREATE TABLE [dbo].[InDocJobNames](
	[pk] [int] IDENTITY(1,1) NOT NULL,
	[jobRaw] [nvarchar](max) NULL,
	[jobGrouped] [nvarchar](max) NULL,
	[subject] [nvarchar](100) NULL,
 CONSTRAINT [PK_InDocJobNames] PRIMARY KEY CLUSTERED 
(
	[pk] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE dbo.InDocTables ADD
	year int NULL

";
                du.RunDDLCommands(sql);
            }
        }
    }
}
