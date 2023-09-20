using GDAPMigrationTool.Core;
using GDAPMigrationTool.Core.Logger;
using GDAPMigrationTool.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var appSetting = new AppSetting();

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((services) =>
    {
        services.AddSingleton(appSetting);
        services.AddSingleton<IExportImportProviderFactory, ExportImportProviderFactory>();
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddSingleton<IDapProvider, DapProvider>();
        services.AddSingleton<IAzureRoleProvider, AzureRoleProvider>();
        services.AddSingleton<IGdapProvider, GdapProvider>();
        services.AddSingleton<IAccessAssignmentProvider, AccessAssignmentProvider>();
        services.AddSingleton<ICustomerProvider, CustomerProvider>();
    }).ConfigureLogging(logging =>
    {
        logging.ClearProviders().AddCustomLogger();
    }).Build();


await MainApp.RunAsync(host.Services, "roles_direct-bill-partner.csv", forceNewGdaps: true);
await host.RunAsync();
