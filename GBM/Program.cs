using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    List<DelegatedAdminRelationshipRequest>? allCustomers = await serviceProvider.GetRequiredService<IDapProvider>().ExportCustomerDetails(type);
    foreach (var customer in allCustomers)
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

    var roles = new List<UnifiedRole>
    {
        // https://learn.microsoft.com/en-us/azure/active-directory/roles/permissions-reference#role-template-ids
        new () { RoleDefinitionId = "c4e39bd9-1100-46d3-8c65-fb160da0071f" },
        new () { RoleDefinitionId = "38a96431-2bdf-4b4c-8b6e-5d3d8abac1a4" },
        new () { RoleDefinitionId = "88d8e3e3-8f55-4a1e-953a-9b9898b8876b" },
        new () { RoleDefinitionId = "9360feb5-f418-4baa-8175-e2a00bac4301" },
        new () { RoleDefinitionId = "29232cdf-9323-42fd-ade2-1d097af3e4de" },
        new () { RoleDefinitionId = "f2ef992c-3afb-46b9-b7cf-a126ee74c451" },
        new () { RoleDefinitionId = "fdd7a751-b60b-444a-984c-02652fe8fa1c" },
        new () { RoleDefinitionId = "95e79109-95c0-4d8e-aee3-d01accf2d47b" },
        new () { RoleDefinitionId = "729827e3-9c14-49f7-bb1b-9608f156bbb8" },
        new () { RoleDefinitionId = "3a2c62db-5318-420d-8d74-23affee5d9d5" },
        new () { RoleDefinitionId = "4d6ac14f-3453-41d0-bef9-a3e0c569773a" },
        new () { RoleDefinitionId = "790c1fb9-7f7d-4f88-86a1-ef1f95c05c1b" },
        new () { RoleDefinitionId = "644ef478-e28f-4e28-b9dc-3fdde9aa0b1f" },
        new () { RoleDefinitionId = "5d6b6bb7-de71-4623-b4af-96380a352509" },
        new () { RoleDefinitionId = "f023fd81-a637-4b56-95fd-791ac0226033" },
        new () { RoleDefinitionId = "f28a1f50-f6e7-4571-818b-6a12f2af6b6c" },
        new () { RoleDefinitionId = "fcf91098-03e3-41a9-b5ba-6f0ec8188a12" },
        new () { RoleDefinitionId = "fe930be7-5e62-47db-91af-98c3a49a38b1" },
    };

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
