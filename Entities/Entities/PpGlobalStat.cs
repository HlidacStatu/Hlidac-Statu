using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HlidacStatu.Entities;

public class PpGlobalStat
{
    private readonly Expression<Func<PuEvent, bool>> predicate;
    public int? Rok { get; set; }
    public int PocetPrijmu { get; set; }
    public int PocetPrijmuPozadano { get; set; }
    public int PocetOsobMaPlat { get; set; }
    public int PocetOsobPozadano { get; set; }
    public int PocetOrganizaciDaliPlat { get; set; }
    public int PocetOrganizaciPozadano { get; set; }
    public Dictionary<int, decimal> PercentilyPlatu { get; set; }

    public PpGlobalStat() { }

   
}