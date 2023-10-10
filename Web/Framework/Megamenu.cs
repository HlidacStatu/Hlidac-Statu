using System;

namespace HlidacStatu.Web.Framework
{
    public class Megamenu
    {
        public enum Items
        {
            dalsi, firmy, hlidac, smlouvy, urady, verejnezakazky, test

        }

        private static string[] _allItems = null;
        public static string[] AllItems()
        { 
            if (_allItems == null)
                _allItems= Enum.GetNames(typeof(Items));
            return _allItems;
        }

    }
}
