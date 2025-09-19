delete from dbo.Ceny where year = 2024;

-- insert Jobgrouped1
INSERT INTO Ceny([JobPK],[SmlouvaId],[IcoOdberatele],[IcoDodavatele],[TablePk],[Polozka],[Tags],[Unit],[UnitText],[PricePerUnitVAT],[Year],[AnalyzaName],[Created])
SELECT j.pk as JobPk, t.smlouvaID, s.IcoOdberatele, d.Ico as IcoDodavatele, j.tablePK, j.jobGrouped as polozka, iif(j.tags='',null, j.tags) as tags, 2 as unit, 'MD' as unitText, iif(j.unit = 2, j.PriceVATCalculated, j.PriceVATCalculated * 8) as pricePerUnitVAT, t.year, Upper(t.analyza) as AnalyzaName , j.created
  FROM InDocTables t
  join InDocJobs j on t.pk = j.tablePK
  join SmlouvyIds s on s.Id = t.smlouvaID
  join SmlouvyDodavatele d on s.Id = d.SmlouvaId
  join (select min(j.pk) jobpk
          from InDocJobs j
          join InDocTables t on t.pk = j.tablePK
         where Unit in (1,2) --manhours,manday
           and Analyza = 'It'
           and t.year = 2024
           and j.PriceVATCalculated is not null
           and j.jobGrouped is not null
         group by jobRaw, PriceVATCalculated, Unit, smlouvaID, page) insel on insel.jobpk = j.pk
 where not exists (select 1 from Ceny c where c.JobPK = j.pk and c.polozka = j.jobgrouped)

-- insert Jobgrouped2
INSERT INTO Ceny([JobPK],[SmlouvaId],[IcoOdberatele],[IcoDodavatele],[TablePk],[Polozka],[Tags],[Unit],[UnitText],[PricePerUnitVAT],[Year],[AnalyzaName],[Created])
SELECT j.pk as JobPk, t.smlouvaID, s.IcoOdberatele, d.Ico as IcoDodavatele, j.tablePK, j.jobGrouped2 as polozka, iif(j.tags='',null, j.tags) as tags, 2 as unit, 'MD' as unitText, iif(j.unit = 2, j.PriceVATCalculated, j.PriceVATCalculated * 8) as pricePerUnitVAT, t.year, Upper(t.analyza) as AnalyzaName , j.created
  FROM InDocTables t
  join InDocJobs j on t.pk = j.tablePK
  join SmlouvyIds s on s.Id = t.smlouvaID
  join SmlouvyDodavatele d on s.Id = d.SmlouvaId
  join (select min(j.pk) jobpk
          from InDocJobs j
          join InDocTables t on t.pk = j.tablePK
         where Unit in (1,2) --manhours,manday
           and Analyza = 'It'
           and t.year = 2024
           and j.PriceVATCalculated is not null
           and j.jobGrouped2 is not null
         group by jobRaw, PriceVATCalculated, Unit, smlouvaID, page) insel on insel.jobpk = j.pk
 where not exists (select 1 from Ceny c where c.JobPK = j.pk and c.polozka = j.jobgrouped2)

-- insert Jobgrouped3
INSERT INTO Ceny([JobPK],[SmlouvaId],[IcoOdberatele],[IcoDodavatele],[TablePk],[Polozka],[Tags],[Unit],[UnitText],[PricePerUnitVAT],[Year],[AnalyzaName],[Created])
SELECT j.pk as JobPk, t.smlouvaID, s.IcoOdberatele, d.Ico as IcoDodavatele, j.tablePK, j.jobGrouped3 as polozka, iif(j.tags='',null, j.tags) as tags, 2 as unit, 'MD' as unitText, iif(j.unit = 2, j.PriceVATCalculated, j.PriceVATCalculated * 8) as pricePerUnitVAT, t.year, Upper(t.analyza) as AnalyzaName , j.created
  FROM InDocTables t
  join InDocJobs j on t.pk = j.tablePK
  join SmlouvyIds s on s.Id = t.smlouvaID
  join SmlouvyDodavatele d on s.Id = d.SmlouvaId
  join (select min(j.pk) jobpk
          from InDocJobs j
          join InDocTables t on t.pk = j.tablePK
         where Unit in (1,2) --manhours,manday
           and Analyza = 'It'
           and t.year = 2024
           and j.PriceVATCalculated is not null
           and j.jobGrouped3 is not null
         group by jobRaw, PriceVATCalculated, Unit, smlouvaID, page) insel on insel.jobpk = j.pk
 where not exists (select 1 from Ceny c where c.JobPK = j.pk and c.polozka = j.jobgrouped3)