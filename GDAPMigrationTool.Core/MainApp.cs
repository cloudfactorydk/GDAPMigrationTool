using GDAPMigrationTool.Core.Model;
using GDAPMigrationTool.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text;

namespace GDAPMigrationTool.Core;

public class MainApp
{
    public static async Task RunAsync(IServiceProvider serviceProvider, string rolesFile, bool skipCreatingGdap = false)
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

        await Migrate(serviceProvider, rolesFile, skipCreatingGdap);

        stopwatch.Stop();
        Console.WriteLine($"[Completed the operation in {stopwatch.Elapsed}]\n");
        Console.WriteLine("\nCloud Factory is a Microsoft Indirect Provider (Distributor) and we did the magic. We hope you liked it! Thanks.");
        Console.Write("\n\n\nPress any key to continue...");
        Console.ReadKey();
    }

    public static async Task Migrate(IServiceProvider serviceProvider, string rolesFile, bool skipCreatingGdap = false)
    {
        // https://learn.microsoft.com/en-us/azure/active-directory/roles/permissions-reference#role-template-ids
        var rolesFromFile = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "Roles", rolesFile));
        List<UnifiedRole> roles = new();
        foreach (string roleId in rolesFromFile.Split(';'))
            roles.Add(new() { RoleDefinitionId = roleId });

        List<DelegatedAdminRelationshipRequest> customersToProcess = new();

        string customerFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Customers", "customers.csv");

        List<DelegatedAdminRelationship> customersWithGdap = null;
        if (File.Exists(customerFilePath))
        {
            var lines = File.ReadLines(customerFilePath, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var props = line.Split(';');

                if (props[0].ToLower().Trim() == "name") continue;

                customersToProcess.Add(new DelegatedAdminRelationshipRequest
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
            var allCustomers = await serviceProvider.GetRequiredService<IDapProvider>().ExportCustomerDetails(ExportImport.Json);
            customersWithGdap = (await serviceProvider.GetRequiredService<IGdapProvider>().GetAllGDAPAsync(ExportImport.Json)).ToList();
            var customerIdsToIgnore = customersWithGdap
                .Where(x =>
                    x.Status == DelegatedAdminRelationshipStatus.Active ||
                    x.Status == DelegatedAdminRelationshipStatus.Activating)
                .Where(x => x.DisplayName.StartsWith("GDAP_"))
                .Select(x => x.Customer.TenantId)
                .ToHashSet();
            customersToProcess = allCustomers.Where(x => !customerIdsToIgnore.Contains(x.CustomerTenantId)).ToList();
            foreach (var request in customersToProcess)
                request.Name = $"GDAP_2023_{request.CustomerTenantId}";
        }

        foreach (var customer in customersToProcess)
            customer.Duration = "730";

        (List<DelegatedAdminRelationship>? successfulGDAP, List<DelegatedAdminRelationshipErrored>? failedGDAP) createGdapForCustomer;
        if (!skipCreatingGdap)
        {
            createGdapForCustomer = await serviceProvider.GetRequiredService<IGdapProvider>().CreateGDAPRequestAsync(ExportImport.Json, customersToProcess, roles);
        }
        else
        {
            customersWithGdap ??= (await serviceProvider.GetRequiredService<IGdapProvider>().GetAllGDAPAsync(ExportImport.Json))
                .Where(x => x.Status == DelegatedAdminRelationshipStatus.Active ||
                            x.Status == DelegatedAdminRelationshipStatus.Activating)
                .ToList();
            createGdapForCustomer = (customersWithGdap, new List<DelegatedAdminRelationshipErrored>());
        }

        var allSecurityGroups = await serviceProvider.GetRequiredService<IAccessAssignmentProvider>().ExportSecurityGroup(ExportImport.Json);
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

    public static bool CheckPrerequisites(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ITokenProvider>().CheckPrerequisite().Result;
    }
}