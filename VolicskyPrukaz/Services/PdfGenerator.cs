using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace VolicskyPrukaz.Services;

public class PdfGenerator
{
    private string _rootPath;

    private const string DatumVoleb = "23. a 24. září 2022";


    private const string NadpisPrvniRadek = "Žádost o vydání voličského průkazu";
    private const string NadpisDruhyRadek = "pro hlasování ve volbách do senátu ČR";
    private const string NadpisTretiRadek = $"konaných dne {DatumVoleb}";

    private const string HlavniTextCastPrvni =
        $"Podle ustanovení § 33 zákona č. 275/2012 Sb., o volbě prezidenta republiky a o změně některých zákonů (zákon o volbě prezidenta republiky),§ 6a zákona č. 247/1995 Sb., o volbách do Parlamentu České republiky a o změně a doplnění některých dalších zákonů, ve znění pozdějších předpisů, § 30 zákona č. 62/2003 Sb., o volbách do Evropského parlamentu a o změně některých zákonů, ve znění pozdějších předpisů, § 26a zákona č. 130/2000 Sb., o volbách do zastupitelstev krajů a o změně některých zákonů, ve znění pozdějších předpisů (dále jen zákon o volbách do EP), žádám ";

    private const string HlavniTextCastDruha =
        $" o vydání voličského průkazu pro hlasování ve volbách do Senátu ČR konaných ve dnech {DatumVoleb}, neboť nebudu moci volit ve volebním okrsku, v jehož seznamu voličů jsem zapsán(a).";

    public PdfGenerator(IHostEnvironment environment)
    {
        _rootPath = environment.ContentRootPath;
    }

    public byte[] Create(string adresa)
    {
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
                    .FontFamily("Calibri")
                    .Weight(FontWeight.Normal));

                // Hlavička
                page.Header()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Line(NadpisPrvniRadek).Bold().FontSize(26).LineHeight(0.8f);
                        text.Line(NadpisDruhyRadek).SemiBold().FontSize(20).LineHeight(0.8f);
                        text.Line(NadpisTretiRadek).SemiBold().FontSize(20).LineHeight(0.8f);
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
                            text.Span(adresa).Weight(FontWeight.SemiBold);
                            text.Span(HlavniTextCastDruha);
                        });

                        x.Item().Text(text =>
                        {
                            text.Span("Jméno a příjmení žadatele (voliče): ").Weight(FontWeight.SemiBold);
                            text.Line("Radek dlouhej");

                            text.Span("Datum narození: ").Weight(FontWeight.SemiBold);
                            text.Line("9. 9. 1999");

                            text.Span("Trvalý pobyt: ").Weight(FontWeight.SemiBold);
                            text.Line("Moje tajná adresa 577, 999 09 Nikdálkov");

                            text.Span("Telefonní číslo: ").Weight(FontWeight.SemiBold);
                            text.Line("721 456 654");
                        });


                        x.Item().Text("K tomu sděluji, že voličský průkaz <placeholder>");
                        x.Item().Row(row =>
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