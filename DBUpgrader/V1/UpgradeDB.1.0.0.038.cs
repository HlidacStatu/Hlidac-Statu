﻿using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {

        private partial class UpgradeDB
        {

            [DatabaseUpgradeMethod("1.0.0.38")]
            public static void Init_1_0_0_38(IDatabaseUpgrader du)
            {

                string sql = @"
update watchdog 
set dataType = 'Smlouva'
where dataType is null
";

                du.AddColumnToTable("dataType", "nvarchar(50)", "watchdog", true);
                du.RunDDLCommands(sql);

                //add new bank account



            }




        }

    }
}
