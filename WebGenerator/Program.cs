using HlidacStatu.LibCore.Extensions;

namespace Gen
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.ConfigureHostForWeb(args);

            //inicializace statických proměnných
            Devmasters.Config.Init(builder.Configuration);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();



            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{action=Index}/{id?}", 
                defaults: new { controller = "Home" });

            app.Run();
        }
    }
}
