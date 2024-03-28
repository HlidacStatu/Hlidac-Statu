-- insert Mandays
INSERT INTO Ceny([JobPK]
           ,[SmlouvaId]
           ,[IcoOdberatele]
           ,[IcoDodavatele]
           ,[TablePk]
           ,[Polozka]
           ,[Tags]
           ,[Unit]
           ,[UnitText]
           ,[PricePerUnitVAT]
           ,[Year]
           ,[AnalyzaName]
           ,[Created])

SELECT
    j.pk as JobPk,
    t.smlouvaID,
    s.IcoOdberatele,
    d.Ico as IcoDodavatele,
    j.tablePK,
    j.jobGrouped as polozka,
    iif(j.tags='',null, j.tags) as tags,
    2 as unit, --MD from enum MeasureUnit
    'MD' as unitText,
    j.PriceVATCalculated as pricePerUnitVAT,
    t.year,
    Upper(t.analyza) as AnalyzaName ,
    j.created

FROM InDocTables t
 join InDocJobs j on t.pk = j.tablePK
 join SmlouvyIds s on s.Id = t.smlouvaID
 join SmlouvyDodavatele d on s.Id = d.SmlouvaId
 join (select min(j.pk) jobpk
       from InDocJobs j
                join InDocTables t on t.pk = j.tablePK
       where Unit = 2 --manday
         and Analyza = 'It'
         and t.year = 2023
         and j.PriceVATCalculated is not null
         and j.jobGrouped is not null
       group by jobRaw, PriceVATCalculated, Unit, smlouvaID, page) insel on insel.jobpk = j.pk
where not exists (select 1 from Ceny c where c.JobPK = j.pk )


-- insert hours translated into mandays
INSERT INTO Ceny([JobPK]
    ,[SmlouvaId]
    ,[IcoOdberatele]
    ,[IcoDodavatele]
    ,[TablePk]
    ,[Polozka]
    ,[Tags]
    ,[Unit]
    ,[UnitText]
    ,[PricePerUnitVAT]
    ,[Year]
    ,[AnalyzaName]
    ,[Created])

SELECT
    j.pk as JobPk,
    t.smlouvaID,
    s.IcoOdberatele,
    d.Ico as IcoDodavatele,
    j.tablePK,
    j.jobGrouped as polozka,
    iif(j.tags='',null, j.tags) as tags,
    2 as unit, --MD from enum MeasureUnit
    'MD' as unitText,
    j.PriceVATCalculated * 8 as pricePerUnitVAT,
    t.year,
    Upper(t.analyza) as AnalyzaName ,
    j.created

FROM InDocTables t
 join InDocJobs j on t.pk = j.tablePK
 join SmlouvyIds s on s.Id = t.smlouvaID
 join SmlouvyDodavatele d on s.Id = d.SmlouvaId
 join (select min(j.pk) jobpk
       from InDocJobs j
                join InDocTables t on t.pk = j.tablePK
       where Unit = 1 --hour
         and Analyza = 'It'
         and t.year = 2023
         and j.PriceVATCalculated is not null
         and j.jobGrouped is not null
       group by jobRaw, PriceVATCalculated, Unit, smlouvaID, page) insel on insel.jobpk = j.pk
where not exists (select 1 from Ceny c where c.JobPK = j.pk )
