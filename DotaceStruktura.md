## Minimální struktura pro dotace:

### Povinné (toto z vašeho evidenčního systému na dotace potřebujeme dostat):

**Žadatel název** - Jedná se o jméno firmy, nebo osoby, která dostala přidělenu dotaci  
**Žadatel IČO** - Jedná se o IČO firmy, nebo FOP, která dostala přidělenu dotaci  
**Žadatel rok** narození - Jedná se o rok narození osoby, která dostala přidělenu dotaci, pokud není identifikována pomocí IČO  
**Název projektu** - Čeho se dotace týká (za/na co dostává žadatel peníze), nebo také jaký je účel dotace  
**Datum schválení** - Kdy bylo schváleno. Stačí rok.  

**Název dotačního programu\**)** - Dotace jsou většinou rozdělovány na programové a individuální. Rádi bychom obdržely název programu, kterého se dotace týkají (např. Kotlíkové dotace, sportovní dotace mládež, kulturní dotace - divadla). Záleží na každé obci, jak dotace rozděluje, není potřeba vyplňovat programy uměle.  
**Kód dotačního programu\**)** - Pokud má dotační program přidělen kód, pak vyplňte i kód tohoto programu  


**Schválená částka\*)** - Kolik peněz bylo schváleno k proplacení na požadovaný projekt  
**Vyplacená částka\*)** - Kolik peněz nakonec žadatel na dotaci čerpal  

\* U hvězdičkou označených položek nám bude stačit, když bude vyplněna alespoň jedna z nich. Pokud budou označeny obě, pak to bude ideální, ale ne nutné.

\** Uveďte, pokud je k dispozici.


### Rádi bychom obdrželi (pokud něco z následujících položek není součástí exportu, nebo by bylo složité na dohledání, tak nevyplňujte):
**Vrácená částka** - Pokud došlo k nějakým vratkám, pak částka, kolik bylo vráceno.  
**Žadatel PSČ**  
**Žadatel obec**  
**Žadatel okres**  

### Nepovinné (Pokud je součástí exportu, můžete data předat, jinak se neobtěžujte s dohledáváním a vyplňováním):
**Zdroje dotace** - Kdo poskytuje peníze. Většinou půjde o Obec, která dotace spravuje (vy).  
**Zdroje dotace IČO** - Viz zdroje dotace - akorát IČO :)  

## Maximální struktura pro dotace: 

Struktura obsahuje vše, co má minimální struktura a navíc má rozpad financování na účetní operace a zdroje financí. Maximální struktura také přidává místo realizace - místo, kde byly peníze použity.


```
{
    "identifikator": "ABCD1235",
    "kod_projektu": "CZ.03.1.51/0.0/0.0/19_101/0014322",
    "nazev_projektu": "Podpora dětské skupiny  - navazující na projekt č.p. CZ.03.1.51/0.0/0.0/16_132/0006659",
    "popis_projektu": "Projekt je pokračováním předchozího úspěšného projektu, který má za cíl podpořit dětskou skupinu 'Paleček' ve vybudování dětského hřiště a jeho zabezpečení.",
    "stav_dotace": "schvalena/vyplacena/zrusena",
    "zadatel":{
        "nazev": "Moje firma s.r.o",
        "stat": "Česko",
        "okres": "Náchod",
        "psc": "55101",
        "obec": "Jaroměř",
        "ico": "01234567",
        "rok_narozeni": "1987"
    },
    "misto_realizace": {
        "stat": "Česko",
        "okres": "Hradec Králové",
        "psc": "50001",
        "obec": "Hradec Králové",
        "ulice": "28. října"
    },
    "program": {
      "kod": "CZ.03.1.51/0.0/0.0/19_101/0014322",
      "nazev": "Zelená úsporám",
      "uri": "https://novazelenausporam.cz/dokumenty/podminky-unor-2025/oprav-dum-po-babicce/",
      "datum_pocatku": "2022-03-13T00:00:00.000Z",
      "datum_konce": "2025-03-13T00:00:00.000Z"
    },
    "datum_schvaleni": "2022-03-13T00:00:00.000Z",
    "datum_planovaneho_ukonceni": "2022-03-13T00:00:00.000Z",
    "datum_skutecneho_ukonceni": "2022-03-13T00:00:00.000Z",
    "pozadovana_castka": 4000000,
    "schvalena_castka": 3450000,
    "vyplacena_castka": 3000000,
    "vracena_castka": 250000,
    "financovani": [
        {
            "datum_rozhodnuti": "2022-03-13T00:00:00.000Z",
            "rozhodnuta_castka": 1000000,
            "zdroj_penez_nazev": "Ministerstvo práce a sociálních věcí",
            "zdroj_penez_ico": "00551023",
            "cerpani": [
                {
                    "datum": "2022-03-20T00:00:00.000Z",
                    "castka": 500000
                },
                {
                    "datum": "2022-04-20T00:00:00.000Z",
                    "castka": 500000
                }
            ],
            "vratka": [
                {
                    "datum": "2023-05-20T00:00:00.000Z",
                    "castka": 50000
                }
            ]
        },
        {
            "datum_rozhodnuti": "2023-03-13T00:00:00.000Z",
            "rozhodnuta_castka": 1000000,
            "zdroj_penez_nazev": "EU",
            "zdroj_penez_ico": null,
            "cerpani": [
                {
                    "datum": "2023-03-21T00:00:00.000Z",
                    "castka": 500000
                },
                {
                    "datum": "2023-04-12T00:00:00.000Z",
                    "castka": 500000
                }
            ]
        },
        {
            "datum_rozhodnuti": "2024-03-13T00:00:00.000Z",
            "rozhodnuta_castka": 1450000,
            "zdroj_penez_nazev": "Ministerstvo práce a sociálních věcí",
            "zdroj_penez_ico": "00551023",
            "cerpani": [
                {
                    "datum": "2024-01-05T00:00:00.000Z",
                    "castka": 1000000
                }
            ],
            "vratka": [
                {
                    "datum": "2024-10-20T00:00:00.000Z",
                    "castka": 200000
                }
            ]
        }
    ],
    "spravce_dotace_nazev": "eufondy",
    "spravce_dotace_ico": "66002222",
    "datumaktualizaceudaju": "2024-11-14T23:42:29.935Z"
}
```
