﻿using Devmasters.DatabaseUpgrader;


namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {

        private partial class UpgradeDB
        {

            [DatabaseUpgradeMethod("1.0.0.20")]
            public static void Init_1_0_0_20(IDatabaseUpgrader du)
            {

                //fix of 1.0.0.14 script

                string sql = @"


";
                du.RunDDLCommands(sql);


            }




        }

    }
}
