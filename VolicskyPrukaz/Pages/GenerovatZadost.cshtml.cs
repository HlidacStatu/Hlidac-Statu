using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VolicskyPrukaz.Models;
using VolicskyPrukaz.Services;

namespace VolicskyPrukaz.Pages;

public class GenerovatZadost : PageModel
{
    [BindProperty]
    public Zadost? Zadost { get; set; }
    
    public void OnGet()
    {
    }

    public IActionResult OnPost([FromServices]PdfGenerator pdfGenerator)
    {
        if (!ModelState.IsValid || Zadost is null)
        {
            return Page();
        }

        //return RedirectToPage(nameof(StazeniZadosti));

        var pdf = pdfGenerator.Create(Zadost);
        return File(pdf, MediaTypeNames.Application.Pdf, "volicskyPrukaz.pdf");
    }
}