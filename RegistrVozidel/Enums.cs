using Devmasters.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.RegistrVozidel
{
    public  class Enums
    {
        [Devmasters.Enums.ShowNiceDisplayName()]
        public enum Vztah_k_vozidluEnum
        {
            [NiceDisplayName("Neuveden")]
            Neuveden = 0,
            [NiceDisplayName("Vlastník")]
            Vlastnik = 1,
            [NiceDisplayName("Provozovatel")]
            Provozovatel = 2
        }

    }
}
