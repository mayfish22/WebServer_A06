using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using WebServer.Extensions;
using WebServer.Models.WebServerDB;
using WebServer.Services;

namespace WebServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            //�h��y�t
            //�N��a�y�t�ƪA�ȷs�W�ܪA�Ȯe���C
            builder.Services.AddLocalization();
            builder.Services.AddControllersWithViews()
                //�b cshtml ���ϥΦh��y��
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                //�b Model ���ϥΦh��y��
                .AddDataAnnotationsLocalization(
                options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                    factory.Create(typeof(Resource));
                });

            //�]�w�s�u�r��
            builder.Services.AddDbContext<WebServerDBContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetConnectionString("WebServerDB"));
            });

            // �ϥ� Session
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // �ϥ� Cookie
            builder.Services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    //�s���Q���������
                    options.AccessDeniedPath = new PathString("/Account/Signin");
                    //�n�J��
                    options.LoginPath = new PathString("/Account/Signin");
                    //�n�X��
                    options.LogoutPath = new PathString("/Account/Signout");
                });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<SiteService>();
            builder.Services.AddScoped<ValidatorService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
            builder.Services.AddScoped<WebServer.Filters.AuthorizeFilter>();

            var app = builder.Build();

            ServiceActivator.Configure(app.Services);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            using (var serviceScope = ServiceActivator.GetScope())
            {
                //�qDI Container ���o Service
                var siteService = (SiteService?)serviceScope?.ServiceProvider.GetService(typeof(SiteService));
                //�q��Ʈw�����o�y�t
                var cultures = siteService?.GetCultures();

                var localizationOptions = new RequestLocalizationOptions()
                    .SetDefaultCulture(cultures![0])//�w�]��
                    .AddSupportedCultures(cultures)
                    .AddSupportedUICultures(cultures);
                localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>{
                    //�� url �d�ߦr��ӳ]�w CultureInfo 
                    new QueryStringRequestCultureProvider(),
                    //�� Cookie �l�ܨϥΪ̺D�Τ�ƯS�ʸ�T
                    new CookieRequestCultureProvider(),
                    //�L�s�����n�D�� Accept-Language HTTP ���Y�Ӱ����ϥΪ̪��D�λy��
                    new AcceptLanguageHeaderRequestCultureProvider(),
                };
                app.UseRequestLocalization(localizationOptions);
            }

            app.UseRouting();

            app.UseAuthentication();//����

            app.UseAuthorization();//���v 

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}