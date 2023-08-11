using System.IO;
using HlidacStatu.Web.Models;
using Microsoft.Extensions.Hosting;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HlidacStatu.Web.Pdfs;

public class ObtezujiciHovorPdf
{
    private static string[] AdresatLines = new string[]
    {
        "Český telekomunikační úřad",
        "poštovní přihrádka 02",
        "225 02 Praha 025",
        "datová schránka: a9qaats",
        "e-mail: podatelna@ctu.cz"
    };
    
    public static byte[] Create(ObtezujiciHovor zadost)
    {
        // var logoPath = Path.Combine(_rootPath, "wwwroot", "assets", "images", "hslogo.png");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(2, Unit.Centimetre);
                page.MarginLeft(2, Unit.Centimetre);
                page.MarginRight(2, Unit.Centimetre);
                page.MarginBottom(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontSize(10)
                    .Black()
                    .FontFamily("Cabin")
                    .Weight(FontWeight.Normal));

                // Hlavička
                page.Header()
                    .AlignLeft()
                    .Text(text =>
                    {
                        foreach (var line in AdresatLines)
                        {
                            text.Line(line).SemiBold().FontSize(15).LineHeight(0.6f);    
                        }
                    });

                // Tělo
                page.Content()
                    .PaddingVertical(0.3f, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);
                        x.Item()
                            .AlignLeft()
                            .PaddingVertical(15)
                            .Text("Věc: Podání stížnosti na nevyžádaný marketingový hovor");

                        x.Item().Text(text =>
                        {
                            text.Span($"Dne {zadost.Datum} došlo mou osobou k přijetí hovoru na mém telefonním čísle od volajícího {zadost.Spolecnost}. Jednalo se o marketingový hovor, který byl obtěžující a nevyžádaný. Volajícímu nebyl dán mou osobou k takovým hovorům souhlas a to ani nikdy v minulosti. Dodávám, že mé telefonní číslo není uvedené v žádném veřejně dostupném seznamu.");
                        });

                        x.Item()
                            .AlignLeft()
                            .PaddingVertical(15)
                            .Text("Bližší informace o nevyžádaném marketingovém hovoru");
                        
                        x.Item().Text(text =>
                        {
                            text.Line("Příjemce hovoru:");
                            text.Span("Jméno: ").SemiBold();
                            text.Line(zadost.Jmeno);
                            text.Span("Telefonní číslo: ").SemiBold();
                            text.Line(zadost.Volany);
                            text.Span("Operator: ").SemiBold();
                            text.Line(zadost.Teloperator);
                            text.Span("Kontakt: ").SemiBold();
                            text.Line(zadost.Kontakt);
                            
                            text.Line("Volající:");
                            text.Span("Název: ").SemiBold();
                            text.Line(zadost.Spolecnost);
                            text.Span("Telefonní číslo: ").SemiBold();
                            text.Line(zadost.Volajici);
                        });
                        
                    });

                page.Footer()
                    .Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(200).AlignRight().Column(col =>
                        {
                            // col.Item().Width(60).AlignRight().Image(logoPath, ImageScaling.FitArea);
                            col.Item().AlignRight().Text("www.HlidacStatu.cz").FontSize(7);
                        });
                    });
            });
        }).GeneratePdf();

        return document;
    }
}