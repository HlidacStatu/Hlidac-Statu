using DatabaseUpgrader;

namespace HlidacStatu.DBUpgrades
{
    public static partial class DBUpgrader
    {
        public static void UpgradeDatabases(string cnnString)
        {

            MsSqlDatabaseUpgrader core = new MsSqlDatabaseUpgrader(
                cnnString,
                typeof(UpgradeDB),
                MsSqlDatabaseObjectTypeForDatabaseVersion.ExtendedProperty
            );

            core.Upgrade();


        }

    }
}
