using System.Collections.Concurrent;
using GDAPMigrationTool.Core.Model;
using GDAPMigrationTool.Core.Utility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Attribute = GDAPMigrationTool.Core.Model.Attribute;

namespace GDAPMigrationTool.Core.Providers
{
    public class CustomerProvider : ICustomerProvider
    {
        /// <summary>
        /// The token provider.
        /// </summary>
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<CustomerProvider> logger;
        private readonly IExportImportProviderFactory exportImportProviderFactory;
        private readonly CustomProperties customProperties;

        protected ProtectedApiCallHelper protectedApiCallHelper;

        private string PartnerCenterAPI { get; set; }

        /// <summary>
        /// AccessAssignment provider constructor.
        /// </summary>
        public CustomerProvider(ITokenProvider tokenProvider, AppSetting appSetting, IExportImportProviderFactory exportImportProviderFactory, ILogger<CustomerProvider> logger)
        {
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.exportImportProviderFactory = exportImportProviderFactory;
            protectedApiCallHelper = new ProtectedApiCallHelper(appSetting.Client);
            customProperties = appSetting.customProperties;
            PartnerCenterAPI = appSetting.PartnerCenterAPI;
        }

        private async Task<string?> getToken(Resource resource)
        {
            var authenticationResult = await tokenProvider.GetTokenAsync(resource);
            return authenticationResult?.AccessToken;
        }

        private async Task<DAPRemovalResponse> PatchDAPRemoval(DAPTerminate customer)
        {
            var CustomerDetails = new DAPRemovalResponse()
            {
                CustomerTenantId = customer.CustomerTenantId,
                OrganizationDisplayName = customer.OrganizationDisplayName
            };
            try
            {
                var accessToken = await getToken(Resource.PartnerCenter);
                var url = $"{PartnerCenterAPI}{customer.CustomerTenantId}";
                var data = JsonConvert.SerializeObject(new DAPRemovalInfo() { AllowDelegatedAccess = false, attributes = new Attribute() { objectType = "Customer" } }).ToString();
                var response = await protectedApiCallHelper.CallWebApiPatchAndProcessResultAsync(url, accessToken, string.Empty, data);

                logger.LogInformation($"DAP removal Response:\n {customer.CustomerTenantId}-{response.StatusCode} \n {response.Content.ReadAsStringAsync().Result} \n");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"{customer.OrganizationDisplayName} - DAP Successfully removed.");
                    CustomerDetails.status = "Success";
                }
                else
                {
                    Console.WriteLine($"{customer.OrganizationDisplayName} - DAP removal failed.");
                    CustomerDetails.status = "Failed";
                }

                return CustomerDetails;
            }
            catch (Exception ex)
            {

                logger.LogError(ex.Message);
                CustomerDetails.status = "Error";
                return CustomerDetails;
            }
        }

        /// <summary>
        /// Create File for terminate list
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<bool> CreateDAPTerminateFile(ExportImport type)
        {
            try
            {
                var fileName = "customer_dap_terminate";
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var path = $"{Constants.InputFolderPath}/{fileName.Trim().ToLower()}.{Helper.GetExtenstion(type)}";
                //Create a dummy list;
                var dummyList = new List<DAPTerminate>();
                if (!File.Exists(path))
                {
                    await exportImportProvider.WriteAsync(dummyList, $"{Constants.InputFolderPath}/{fileName}.{Helper.GetExtenstion(type)}");
                }
            }
            catch (IOException ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine("Make sure the file is closed before running the operation.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return true;

        }

        /// <summary>
        /// DAP removal API PC
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<bool> DAPTermination(ExportImport type)
        {
            try
            {
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var path = $"{Constants.InputFolderPath}/customer_dap_terminate.{Helper.GetExtenstion(type)}";
                var inputCustomer = await exportImportProvider.ReadAsync<DAPTerminate>(path);

                if (!inputCustomer.Any())
                {
                    Console.WriteLine(" Error while Processing the input. Incorrect data provided for processing. Please check the input file.");
                    Console.WriteLine($"Check the path {path}");
                    return true;
                }

                var option = Helper.UserConfirmation($"Warning: This is permanent change, are you sure you want to continue with {inputCustomer.Count()} record(s) for DAP removal?");
                if (!option)
                {
                    return true;
                }

                var responseList = new ConcurrentBag<DAPRemovalResponse>();

                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 5
                };
                protectedApiCallHelper.setHeader(false);
                if (inputCustomer.Any())
                {
                    await Parallel.ForEachAsync(inputCustomer, options, async (customer, cancellationToken) =>
                    {
                        if (customer != null)
                        {
                            responseList.Add(await PatchDAPRemoval(customer));
                        }
                    });
                }

                var dapTerminationpath = $"{Constants.InputFolderPath}/dapTermination/dap_terminated.{Helper.GetExtenstion(type)}";
                if (customProperties.ReplaceFileDuringUpdate && File.Exists(dapTerminationpath))
                {
                    Filehelper.RenameFolder(dapTerminationpath);
                }
                Console.WriteLine($"Downloaded latest statuses of DAP Relationship(s) at {dapTerminationpath}");
                await exportImportProvider.WriteAsync(responseList, dapTerminationpath);

            }
            catch (IOException ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine("Make sure all input file(s) are closed before running the operation.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return true;
        }
    }
}