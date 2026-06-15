//using Template.Web.Hubs;
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.IO;
using System.Linq;
using Template.Data;
using Template.Services;
using Template.Web.Infrastructure;
using Template.Web.SignalR.Hubs;

namespace Template.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Env { get; set; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Env = env;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddDbContext<TemplateDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: "Template");
            });

            // SERVICES FOR AUTHENTICATION
            // La sessione conserva i dati usati dalle pagine di MarcTempo.
            services.AddSession(options =>
            {
                /*
                 * Il nome versione 2 permette di ignorare automaticamente
                 * eventuali vecchi cookie di sessione.
                 */
                options.Cookie.Name = ".MarcTempo.Session.v2";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromHours(8);
            });

            // Il cookie contiene i claim necessari a ricostruire la sessione.
            services
                .AddAuthentication(
                    CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    /*
                     * Questo è il punto in cui si imposta
                     * il nome ".MarcTempo.Auth.v2".
                     */
                    options.Cookie.Name = ".MarcTempo.Auth.v2";

                    options.LoginPath = "/Login/Login";
                    options.AccessDeniedPath = "/Login/Login";

                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;

                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

            var builder = services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization(options =>
                {                        // Enable loading SharedResource for ModelLocalizer
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource));
                });

#if DEBUG
            builder.AddRazorRuntimeCompilation();
#endif

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.AreaViewLocationFormats.Clear();
                options.AreaViewLocationFormats.Add("/Areas/{2}/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");

                options.ViewLocationFormats.Clear();
                options.ViewLocationFormats.Add("/Features/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/Features/Views/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/Features/Views/Shared/{0}.cshtml");
                options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
            });

            // SIGNALR FOR COLLABORATIVE PAGES
            services.AddSignalR();

            // CONTAINER FOR ALL EXTRA CUSTOM SERVICES
            Container.RegisterTypes(services);
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            // Configurazione della pipeline HTTP.
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");

                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseRequestLocalization(
                SupportedCultures.CultureNames);

            /*
             * Il provider personalizzato deve essere configurato
             * prima di UseStaticFiles.
             */
            var nodeModules = new CompositePhysicalFileProvider(
                Directory.GetCurrentDirectory(),
                "node_modules");

            var areas = new CompositePhysicalFileProvider(
                Directory.GetCurrentDirectory(),
                "Areas");

            var compositeFileProvider =
                new CustomCompositeFileProvider(
                    env.WebRootFileProvider,
                    nodeModules,
                    areas);

            env.WebRootFileProvider = compositeFileProvider;

            // CSS, JavaScript e immagini vengono serviti prima del routing.
            app.UseStaticFiles();

            app.UseRouting();

            /*
             * Ordine importante:
             * 1. sessione;
             * 2. autenticazione;
             * 3. autorizzazione.
             */
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // Creazione degli utenti iniziali nel database InMemory.
            using (var scope =
                   app.ApplicationServices.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<TemplateDbContext>();

                SeedData.Initialize(dbContext);
            }

            app.UseEndpoints(endpoints =>
            {
                // Routing SignalR.
                endpoints.MapHub<TemplateHub>(
                    "/templateHub");

                endpoints.MapAreaControllerRoute(
                    name: "Example",
                    areaName: "Example",
                    pattern:
                        "Example/{controller=Users}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern:
                        "{controller=Login}/{action=Login}/{id?}");
            });
        }
    }

    public static class SupportedCultures
    {
        public readonly static string[] CultureNames;
        public readonly static CultureInfo[] Cultures;

        static SupportedCultures()
        {
            CultureNames = new[] { "it-it" };
            Cultures = CultureNames.Select(c => new CultureInfo(c)).ToArray();

            //NB: attenzione nel progetto a settare correttamente <NeutralLanguage>it-IT</NeutralLanguage>
        }
    }
}
