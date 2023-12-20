using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using SagreEventi.Web.Server.Extensions;
using SagreEventi.Web.Server.HostedServices;
using SagreEventi.Web.Server.Models.Services.Application;
using SagreEventi.Web.Server.Models.Services.Infrastructure;

namespace SagreEventi.Web.Server;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            });

        services.AddRazorPages();
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin();
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
            });
        });

        services.AddDbContextPool<AppDbContext>(optionsBuilder =>
        {
            string connectionString = Configuration.GetSection("ConnectionStrings").GetValue<string>("Default");
            optionsBuilder.UseSqlServer(connectionString, options =>
            {
                // Info su: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
                options.EnableRetryOnFailure(3); // Abilito il connection resiliency
                options.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                options.MigrationsHistoryTable("Migrations", "dbo");
            });
        });

        services.AddSingleton<IHostedService, EventiHostedService>();
        services.AddTransient<IEventiService, EfCoreEventiService>();

        services.AddSwaggerServices(Configuration);
        services.Configure<KestrelServerOptions>(Configuration.GetSection("Kestrel"));
    }

    public void Configure(WebApplication app)
    {
        IWebHostEnvironment env = app.Environment;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sagre ed Eventi v1"));
        }

        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles();

        app.UseStaticFiles();
        app.UseRouting();

        app.UseCors();
        app.MapRazorPages();

        app.MapControllers();
        app.MapFallbackToFile("index.html");
    }
}