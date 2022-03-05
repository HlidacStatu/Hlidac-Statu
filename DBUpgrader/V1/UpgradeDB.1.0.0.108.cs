using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.108")]
            public static void Init_1_0_0_108(IDatabaseUpgrader du)
            {
                string sql = @"
/****** Object:  StoredProcedure [dbo].[UptimeServer_SaveAlert]    Script Date: 05.03.2022 15:35:22 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UptimeServer_SaveAlert]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UptimeServer_SaveAlert] AS' 
END
GO


ALTER PROCEDURE [dbo].[UptimeServer_SaveAlert]
	@serverId int,
	@lastAlertedStatus int,
	@lastAlertSent datetime
AS
BEGIN
	SET NOCOUNT ON;

	update UptimeServer
	set LastAlertedStatus = @lastAlertedStatus,
		LastAlertSent = @lastAlertSent
	where Id = @serverId

END

GO




	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
