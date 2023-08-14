using System.IO;
using HlidacStatu.Web.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HlidacStatu.Web.Pdfs;

public static class ObtezujiciHovorPdf
{
    private static string[] AdresatLines = new string[]
    {
        "Český telekomunikační úřad",
        "poštovní přihrádka 02",
        "225 02 Praha 025",
        "datová schránka: a9qaats",
        "e-mail: podatelna@ctu.cz"
    };

    private static string _logoPath;

    
    static ObtezujiciHovorPdf()
    {
        string root = Devmasters.Config.GetWebConfigValue("WebAppRoot");
        //register fonts
        var fontFolder = Path.Combine(root, "wwwroot", "fonts", "cabin");
        var fonts = Directory.GetFiles(fontFolder, "*.ttf");

        foreach (var font in fonts)
        {
            FontManager.RegisterFont(File.OpenRead(font));
        }
        
        _logoPath = Path.Combine(root, "wwwroot", "Content", "Img", "hslogo.png");

        QuestPDF.Settings.License = LicenseType.Community;
    }
    
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
                            text.Line(line).SemiBold();    
                        }
                    });

                // Tělo
                page.Content()
                    .PaddingVertical(0.3f, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(10);
                        x.Item()
                            .AlignLeft()
                            .PaddingTop(15)
                            .Text("Věc: Podání stížnosti na nevyžádaný marketingový hovor")
                            .Bold();

                        x.Item().Text(text =>
                        {
                            text.Span($"Dne {zadost.DatumHovoru} došlo mou osobou k přijetí hovoru na mém telefonním čísle {zadost.CisloVolaneho}. Jednalo se o marketingový hovor, který byl obtěžující a nevyžádaný. Volajícímu nebyl dán mou osobou k takovým hovorům souhlas a to ani nikdy v minulosti. Dodávám, že mé telefonní číslo není uvedené v žádném veřejně dostupném seznamu.");
                        });

                        x.Item()
                            .AlignLeft()
                            .PaddingTop(15)
                            .Text("Bližší informace o nevyžádaném marketingovém hovoru")
                            .Bold();
                        
                        x.Item().Text(text =>
                        {
                            text.Line("Příjemce hovoru").Bold();
                            text.Span("Jméno: ").SemiBold();
                            text.Line(zadost.Jmeno);
                            text.Span("Telefonní číslo: ").SemiBold();
                            text.Line(zadost.CisloVolaneho);
                            text.Span("Operator: ").SemiBold();
                            text.Line(zadost.Teloperator);
                            text.Span("Kontakt: ").SemiBold();
                            text.Line(zadost.Kontakt);
                        });
                        
                        x.Item().Text(text =>
                        {
                            text.Line("Volající").Bold();
                            text.Span("Název společnosti: ").SemiBold();
                            text.Line(zadost.VolajiciSpolecnost);
                            text.Span("Jméno volajícího: ").SemiBold();
                            text.Line(zadost.VolajiciJmeno);
                            text.Span("Telefonní číslo: ").SemiBold();
                            text.Line(zadost.CisloVolajiciho);
                        });
                        
                        x.Item().Text(text =>
                        {
                            text.Line("Hovor").Bold();
                            text.Span("Datum hovoru: ").SemiBold();
                            text.Line(zadost.DatumHovoru);
                            text.Span("Čas hovoru: ").SemiBold();
                            text.Line(zadost.CasHovoru);
                        });
                        
                    });

                page.Footer()
                    .Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(200).AlignRight().Column(col =>
                        {
                            col.Item().Width(60).AlignRight().Image(_logoPath, ImageScaling.FitArea);
                            col.Item().AlignRight().Text("www.HlidacStatu.cz").FontSize(7);
                        });
                    });
            });
        }).GeneratePdf();
        
        return document;
    }
}