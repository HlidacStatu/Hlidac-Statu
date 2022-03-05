using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        private partial class UpgradeDB
        {
            [DatabaseUpgradeMethod("1.0.0.107")]
            public static void Init_1_0_0_107(IDatabaseUpgrader du)
            {
                string sql = @"
ALTER TABLE dbo.UptimeServer ADD
	LastUptimeStatus int NULL,
	LastAlertedStatus int NULL,
	LastAlertSent datetime NULL

GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.UptimeServer_SaveStatus
	@serverId int,
	@lastCheck datetime,
	@lastResponseCode money,
	@lastResponseSize bigint,
	@lastResponseTimeInMs bigint,
	@lastUptimeStatus int
AS
BEGIN
	SET NOCOUNT ON;

	update UptimeServer
	set LastCheck = @lastCheck,
		LastResponseCode = @lastResponseCode,
		LastResponseSize = @lastResponseSize,
		LastResponseTimeInMs = @lastResponseTimeInMs,
		LastUptimeStatus = @lastUptimeStatus,
        TakenByUptimer = null

	where Id = @serverId

END
GO

	";
                
                
                du.RunDDLCommands(sql);
            }
        }
    }
}
