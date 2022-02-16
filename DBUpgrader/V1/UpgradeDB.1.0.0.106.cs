using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.106")]
            public static void Init_1_0_0_106(IDatabaseUpgrader du)
            {
                string sql = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataProtectionKeys]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[DataProtectionKeys](
	[Id] [int] identity(1,1) NOT NULL PRIMARY KEY,
	[FriendlyName] [nvarchar](max) NULL,
    [Xml] [nvarchar](max) NULL)
END
GO

	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
