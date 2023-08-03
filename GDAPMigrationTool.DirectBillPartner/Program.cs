using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PartnerLed;
using PartnerLed.Logger;
using PartnerLed.Model;
using PartnerLed.Providers;


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


await RunAsync(host.Services, appSetting.customProperties.Version);
await host.RunAsync();

static async Task RunAsync(IServiceProvider serviceProvider, string version)
{
    Console.Clear();
    Console.WriteLine("Microsoft GDAP Migration Tool, by Cloud Factory\n");
    Console.WriteLine("Checking pre-requisites...");

    if (!CheckPrerequisites(serviceProvider))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Please request your Global Admin to launch the tool and\n" +
            "a. Enable this tool to automatically execute pre-requisite step to grant access to GDAP API.\nb. Provide consent for this tool to access the GDAP API and read your Tenant Security Groups.");
        Console.ResetColor();
        return;
    }

    Stopwatch stopwatch = Stopwatch.StartNew();

    await Migrate(serviceProvider, ExportImport.Json);

    stopwatch.Stop();
    Console.WriteLine($"[Completed the operation in {stopwatch.Elapsed}]\n");
    Console.WriteLine("\nCloud Factory is a Microsoft Indirect Provider (Distributor) and we did the magic. We hope you liked it! Thanks.");
    Console.Write("\n\n\nPress any key to continue...");
    Console.ReadKey();
    //goto SelectOption;
}

static async Task Migrate(IServiceProvider serviceProvider, ExportImport type)
{
    // https://learn.microsoft.com/en-us/azure/active-directory/roles/permissions-reference#role-template-ids
    var rolesFromFile = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Roles", "roles_direct-bill-partner.csv"));
    List<UnifiedRole> roles = new();
    foreach (string roleId in rolesFromFile.Split(';'))
        roles.Add(new() { RoleDefinitionId = roleId });

    List<DelegatedAdminRelationshipRequest>? allCustomers = new();

    string customerFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Customers", "customers.csv");

    if (File.Exists(customerFilePath))
    {
        var lines = File.ReadLines(customerFilePath, Encoding.UTF8);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var props = line.Split(';');

            if (props[0].ToLower().Trim() == "name") continue;

            allCustomers.Add(new DelegatedAdminRelationshipRequest
            {
                Name = props[0],
                PartnerTenantId = props[1],
                CustomerTenantId = props[2],
                OrganizationDisplayName = props[3].Replace("\"", string.Empty),
                Duration = props[4]
            });
        }
    }
    else
    {
        allCustomers = await serviceProvider.GetRequiredService<IDapProvider>().ExportCustomerDetails(type);
    }

    foreach (var customer in allCustomers!)
        customer.Duration = "730";

    var customersWithGdap = (await serviceProvider.GetRequiredService<IGdapProvider>().GetAllGDAPAsync(type)).ToList();
    var customerIdsToIgnore = customersWithGdap
        .Where(x =>
            x.Status == DelegatedAdminRelationshipStatus.Active ||
            x.Status == DelegatedAdminRelationshipStatus.Activating ||
            x.Status == DelegatedAdminRelationshipStatus.ApprovalPending)
        .Where(x => x.DisplayName.StartsWith("GDAP_"))
        .Select(x => x.Customer.TenantId)
        .ToHashSet();
    var customersToProcess = allCustomers.Where(x => !customerIdsToIgnore.Contains(x.CustomerTenantId)).ToList();

    var createGdapForCustomer = await serviceProvider.GetRequiredService<IGdapProvider>().CreateGDAPRequestAsync(type, customersToProcess, roles);

    var allSecurityGroups = await serviceProvider.GetRequiredService<IAccessAssignmentProvider>().ExportSecurityGroup(type);
    var adminAgents = allSecurityGroups.Single(x => x.DisplayName == "AdminAgents");

    var accessAssignment = new DelegatedAdminAccessAssignment
    {
        AccessContainer = new DelegatedAdminAccessContainer
        {
            AccessContainerId = adminAgents.Id,
            AccessContainerType = DelegatedAdminAccessContainerType.SecurityGroup,
        },
        AccessDetails = new DelegatedAdminAccessDetails
        {
            UnifiedRoles = roles,
        },
    };
    int success = 0;
    foreach (DelegatedAdminRelationship adminRelationship in createGdapForCustomer.successfulGDAP)
    {
        var updateSecurityGroup = await serviceProvider.GetRequiredService<IAccessAssignmentProvider>().PostGranularAdminAccessAssignment(adminRelationship, accessAssignment, new[] { adminAgents });
        if (!string.Equals(updateSecurityGroup.Status, "failed", StringComparison.InvariantCultureIgnoreCase))
            success++;
    }

    Console.WriteLine($"\nDone with {success} migrations");
}

static bool CheckPrerequisites(IServiceProvider serviceProvider)
{
    return serviceProvider.GetRequiredService<ITokenProvider>().CheckPrerequisite().Result;
}
