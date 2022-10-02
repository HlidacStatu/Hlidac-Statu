using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VolicskyPrukaz.Models;

namespace VolicskyPrukaz.Services;

public class PdfGenerator
{
    private string _rootPath;

    private const string DatumVoleb = "13. a 14. ledna 2023";
    private const string DatumVoleb2 = "27. a 28. ledna 2023";

    private const string NadpisPrvniRadek = "Žádost o vydání voličského průkazu";
    private const string NadpisDruhyRadek = "pro hlasování ve volbách prezidenta ČR";
    
    
    private const string HlavniTextCastPrvni =
        $"Podle ustanovení § 33 zákona č. 275/2012 Sb., o volbě prezidenta republiky a o změně některých zákonů (zákon o volbě prezidenta republiky), § 6a zákona č. 247/1995 Sb., o volbách do Parlamentu České republiky a o změně a doplnění některých dalších zákonů, ve znění pozdějších předpisů, § 30 zákona č. 62/2003 Sb., o volbách do Evropského parlamentu a o změně některých zákonů, ve znění pozdějších předpisů, § 26a zákona č. 130/2000 Sb., o volbách do zastupitelstev krajů a o změně některých zákonů, ve znění pozdějších předpisů (dále jen zákon o volbách do EP), žádám ";


    public PdfGenerator(IHostEnvironment environment)
    {
        _rootPath = environment.ContentRootPath;
        
        //register fonts
        var fontFolder = Path.Combine(_rootPath, "wwwroot", "css", "fonts", "cabin");
        var fonts = Directory.GetFiles(fontFolder, "*.ttf");

        foreach (var font in fonts)
        {
            FontManager.RegisterFont(File.OpenRead(font));
        }
        
    }

    public byte[] Create(Zadost zadost)
    {
        string hlavniTextCastDruha;
            
        string nadpisTretiRadek; 
        if (zadost.PrvniKolo && !zadost.DruheKolo)
        {
            nadpisTretiRadek = $"konaných dne {DatumVoleb} (první kolo)";
            hlavniTextCastDruha = $" o vydání voličského průkazu pro hlasování ve volbách prezidenta ČR konaných:\nve dnech {DatumVoleb} (první kolo),\nneboť nebudu moci volit ve volebním okrsku, v jehož seznamu voličů jsem zapsán(a)."; 
        }
        else if (zadost.DruheKolo && !zadost.PrvniKolo)
        {
            nadpisTretiRadek = $"konaných dne {DatumVoleb2} (druhé kolo)";
            hlavniTextCastDruha = $" o vydání voličského průkazu pro hlasování ve volbách prezidenta ČR konaných:\nve dnech {DatumVoleb2} (druhé kolo),\nneboť nebudu moci volit ve volebním okrsku, v jehož seznamu voličů jsem zapsán(a).";
        }
        else
        {
            nadpisTretiRadek = $"konaných v termínech\n{DatumVoleb} (první kolo),\na {DatumVoleb2} (druhé kolo)";
            hlavniTextCastDruha = $" o vydání voličského průkazu pro hlasování ve volbách prezidenta ČR konaných:\nve dnech {DatumVoleb} (první kolo)\na ve dnech {DatumVoleb2} (druhé kolo),\nneboť nebudu moci volit ve volebním okrsku, v jehož seznamu voličů jsem zapsán(a).";
        }
        
        
        var logoPath = Path.Combine(_rootPath, "wwwroot", "assets", "images", "hslogo.png");

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
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Line(NadpisPrvniRadek).Bold().FontSize(26).LineHeight(0.8f);
                        text.Line(NadpisDruhyRadek).SemiBold().FontSize(20).LineHeight(0.9f);
                        text.Line(nadpisTretiRadek).SemiBold().FontSize(20).LineHeight(1.0f);
                    });

                // Tělo
                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);
                        x.Item()
                            .AlignRight()
                            .Border(1)
                            .PaddingVertical(15)
                            .PaddingLeft(5)
                            .PaddingRight(200)
                            .Text("Číslo voličského průkazu: ");

                        x.Item().Text(text =>
                        {
                            text.Span(HlavniTextCastPrvni);
                            text.Span($"{zadost.UradNazev}, {zadost.AdresaUradu}").Weight(FontWeight.SemiBold);
                            text.Span(hlavniTextCastDruha);
                        });

                        x.Item().Text(text =>
                        {
                            text.Span("Jméno a příjmení žadatele (voliče): ").Weight(FontWeight.SemiBold);
                            text.Line(zadost.JmenoZadatele);

                            text.Span("Datum narození: ").Weight(FontWeight.SemiBold);
                            text.Line(zadost.DatumNarozeniZadatele);

                            text.Span("Trvalý pobyt: ").Weight(FontWeight.SemiBold);
                            text.Line(zadost.AdresaZadatele);

                            text.Span("Telefonní číslo: ").Weight(FontWeight.SemiBold);
                            text.Line(zadost.TelefonZadatele);
                        });


                        x.Item().Text(text =>
                        {
                            text.Line($"K tomu sděluji, že voličský průkaz {zadost.Prevzeti}").Weight(FontWeight.SemiBold);
                            if (zadost.Prevzeti == "žádám zaslat na jinou adresu: ")
                            {
                                text.Span(zadost.PrevzetiAdresa);
                            }
                        });
                        
                        x.Item().PaddingTop(100).Row(row =>
                        {
                            row.RelativeItem();
                            row.RelativeItem();
                            row.ConstantItem(200).AlignBottom().AlignCenter()
                                .Text(text =>
                                {
                                    text.Line("_____________________");
                                    text.Line("podpis voliče");
                                });
                        });
                        x.Item().Text("Voličský průkaz převzal volič osobně dne: _________________");
                    });

                page.Footer()
                    .Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(200).AlignRight().Column(col =>
                        {
                            col.Item().Width(60).AlignRight().Image(logoPath, ImageScaling.FitArea);
                            col.Item().AlignRight().Text("www.HlidacStatu.cz").FontSize(7);
                        });
                    });
            });
        }).GeneratePdf();

        return document;
    }
}