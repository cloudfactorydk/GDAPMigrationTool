using GBM.Model;
using GBM.Utility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PartnerLed.Model;
using PartnerLed.Utility;
using System.Globalization;
using System.Net;
using Microsoft.Identity.Client;

namespace PartnerLed.Providers
{
    internal class DapProvider : IDapProvider
    {
        /// <summary>
        /// Provider Instances.
        /// </summary>
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<DapProvider> logger;
        private readonly IGdapProvider gdapProvider;
        private readonly IExportImportProviderFactory exportImportProviderFactory;
        private readonly IAccessAssignmentProvider accessAssignmentProvider;
        private readonly CustomProperties customProperties;

        protected ProtectedApiCallHelper protectedApiCallHelper;

        private string GdapBaseEndpoint { get; set; }

        /// <summary>
        /// URLs of the protected Web APIs to call GDAP (here Traffic Manager endpoints)
        /// </summary>
        protected string WebApiUrlAllDaps { get { return $"{GdapBaseEndpoint}/v1/delegatedAdminCustomers"; } }

        /// <summary>
        /// URLs of the protected Web APIs to call GDAP (here Traffic Manager endpoints)
        /// </summary>
        protected string WebApiUrlAllDapsBulk { get { return $"{GdapBaseEndpoint}/v1/delegatedAdminCustomersBulk"; } }

        /// <summary>
        /// DAP provider constructor.
        /// </summary>
        public DapProvider(ITokenProvider tokenProvider, AppSetting appSetting, IExportImportProviderFactory exportImportProviderFactory, IGdapProvider gdapProvider,
            IAccessAssignmentProvider accessAssignmentProvider, ILogger<DapProvider> logger)
        {
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.exportImportProviderFactory = exportImportProviderFactory;
            this.gdapProvider = gdapProvider;
            this.accessAssignmentProvider = accessAssignmentProvider;
            protectedApiCallHelper = new ProtectedApiCallHelper(appSetting.Client);
            GdapBaseEndpoint = appSetting.GdapBaseEndPoint;
            customProperties = appSetting.customProperties;
        }

        private async Task<string?> getToken(Resource resource)
        {
            var authenticationResult = await tokenProvider.GetTokenAsync(resource);
            return authenticationResult?.AccessToken;
        }

        public async Task<AuthenticationResult> getTokenRaw(Resource resource)
        {
            var authenticationResult = await tokenProvider.GetTokenAsync(resource);
            return authenticationResult;
        }


        /// <summary>
        /// Export customer Details based on user selection of type
        /// </summary>
        /// <param name="type">Export type "JSON" or "CSV" based on user selection.</param>
        /// <returns></returns>
        public async Task<List<DelegatedAdminRelationshipRequest>?> ExportCustomerDetails(ExportImport type)
        {
            var exportImportProvider = exportImportProviderFactory.Create(type);
            var url = $"{WebApiUrlAllDaps}?$count=true&$filter=dapEnabled+eq+true&$orderby=organizationDisplayName";
            var nextLink = string.Empty;
            List<DelegatedAdminRelationshipRequest> allCustomer = new List<DelegatedAdminRelationshipRequest>();
            try
            {
                Console.WriteLine("Downloading customers..");
                // Helper.ResetSpin("Pages.. ");
                protectedApiCallHelper.setHeader(false);
                do
                {
                    var accessToken = await getToken(Resource.TrafficManager);
                    if (!string.IsNullOrEmpty(nextLink))
                    {
                        url = nextLink;
                    }

                    var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync(url, accessToken);
                    // Helper.Spin();
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result) as JObject;
                        var partnerTenantId = tokenProvider.getPartnertenantId();
                        var nextData = result.Properties().Where(p => p.Name.Contains("nextLink"));
                        nextLink = string.Empty;
                        if (nextData != null)
                        {
                            nextLink = (string?)nextData.FirstOrDefault();
                        }
                        // Helper.Spin();
                        allCustomer.AddRange(GetListDelegatedRequest(result, partnerTenantId));
                    }
                    else
                    {
                        string userResponse = GetUserResponse(response.StatusCode);
                        nextLink = string.Empty;
                        // Console.WriteLine($"{userResponse}");
                    }
                } while (!string.IsNullOrEmpty(nextLink));

                // var path = $"{Constants.OutputFolderPath}/customers";
                //await exportImportProvider.WriteAsync(allCustomer, $"{path}.{Helper.GetExtenstion(type)}");
                Console.WriteLine($"\nDownloaded customers: " + allCustomer.Count);
                return allCustomer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while exporting customer Details: " + ex.Message);
                logger.LogError(ex.Message);
                return null;
            }
        }

        private List<DelegatedAdminRelationshipRequest> GetListDelegatedRequest(JObject result, string partnerTenantId)
        {
            var allCustomer = new List<DelegatedAdminRelationshipRequest>();
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                foreach (var item in child.Value)
                {
                    var dapCustomer = item.ToObject<DelegatedAdminCustomer>();
                    // check for default value
                    var displayName = !string.IsNullOrEmpty(customProperties.DefaultGDAPName) ? string.Format(CultureInfo.InvariantCulture, customProperties.DefaultGDAPName, dapCustomer.CustomerTenantId) : string.Empty;
                    var duration = !string.IsNullOrEmpty(customProperties.DefaultGDAPDuration) ? $"{customProperties.DefaultGDAPDuration}" : string.Empty;
                    allCustomer.Add(new DelegatedAdminRelationshipRequest() { Name = displayName, Duration = duration, CustomerTenantId = dapCustomer.CustomerTenantId, OrganizationDisplayName = dapCustomer.OrganizationDisplayName, PartnerTenantId = partnerTenantId });
                }
            }

            return allCustomer;

        }

        private string GetUserResponse(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized: return "Authentication Failed. Please make sure your Sign-in credentials are correct and MFA enabled.";
                case HttpStatusCode.NotFound: return "Customers not found.";
                default: return "Failed to get Customer details.";
            }
        }

        /// <summary>
        /// Generate GDAP relationship with Access Assignment in one flow.
        /// </summary>
        /// <param name="type">Export type "JSON" or "CSV" based on user selection.</param>
        /// <returns></returns>
        public async Task<bool> GenerateDAPRelatioshipwithAccessAssignment(
            ExportImport type,
            List<DelegatedAdminRelationshipRequest> customers,
            List<UnifiedRole> roles)
        {
            //var option = Helper.UserConfirmation("Warning: To run this operation, please make sure all the input files are in the folder: \n" + $"{Constants.InputFolderPath}");

            try
            {
                //if (option)
                //{
                TimeSpan ts = new TimeSpan(0, 0, 5);
                TimeSpan ts1 = new TimeSpan(0, 0, 10);

                await gdapProvider.CreateGDAPRequestAsync(type, customers, roles);
                Console.WriteLine($"\nWaiting for relationship activations... 10 seconds");
                Thread.Sleep(ts1);
                await gdapProvider.RefreshGDAPRequestAsync(type);

                while (!Helper.UserConfirmation("Confirm all GDAP relationship activation complete[y/Y] or refresh again[r/R]:", false))
                {
                    await gdapProvider.RefreshGDAPRequestAsync(type);
                }

                //await accessAssignmentProvider.CreateAccessAssignmentRequestAsync(type);
                Console.WriteLine($"\nWaiting for security group-role assignment activations...");
                Thread.Sleep(ts1);
                await accessAssignmentProvider.RefreshAccessAssignmentRequest(type);
                //}
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return true;
        }

        public async Task<bool> ExportCustomerBulk()
        {
            var confirmation = Helper.UserConfirmation("Warning: This might be a long running operation.");

            if (!confirmation)
            {
                return true;
            }
            try
            {
                var accessToken = await getToken(Resource.TrafficManager);
                var stream = await protectedApiCallHelper.CallWebApiProcessSteamAsync(WebApiUrlAllDapsBulk, accessToken);
                SaveStreamAsFile(Constants.OutputFolderPath, stream, "customers-stream.csv.gz");
                Console.WriteLine($"Downloaded at {Constants.OutputFolderPath}/customers - stream.csv.gz");
            }
            catch (HttpRequestException rex)
            {
                if (rex.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.LogError("\nThe data is available for Partners with number of Customers more than 300.");
                }
                else
                {
                    logger.LogError(rex.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return true;
        }

        private static void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        {
            DirectoryInfo info = new DirectoryInfo(filePath);
            if (!info.Exists)
            {
                info.Create();
            }

            string path = Path.Combine(filePath, fileName);
            using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
            {
                inputStream.CopyTo(outputFileStream);
            }
        }
    }
}
