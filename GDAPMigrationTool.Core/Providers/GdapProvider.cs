// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using GDAPMigrationTool.Core.Model;
using GDAPMigrationTool.Core.Utility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDAPMigrationTool.Core.Providers
{
    public class GdapProvider : IGdapProvider
    {
        /// <summary>
        /// Provider Instances
        /// </summary>
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<GdapProvider> logger;
        private readonly IExportImportProviderFactory exportImportProviderFactory;
        private readonly CustomProperties customProperties;

        /// <summary>
        /// GDAP provider constructor.
        /// </summary>
        public GdapProvider(ITokenProvider tokenProvider, AppSetting appSetting, IExportImportProviderFactory exportImportProviderFactory, ILogger<GdapProvider> logger)
        {
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.exportImportProviderFactory = exportImportProviderFactory;
            protectedApiCallHelper = new ProtectedApiCallHelper(appSetting.Client);
            GdapBaseEndpoint = appSetting.GdapBaseEndPoint;
            customProperties = appSetting.customProperties;
        }

        /// <summary>
        /// Base endpoint for Traffic Manager
        /// </summary>
        private string GdapBaseEndpoint { get; set; }

        protected ProtectedApiCallHelper protectedApiCallHelper;

        /// <summary>
        /// URLs of the protected Web APIs to call GDAP (here Traffic Manager endpoints)
        /// </summary>
        private string WebApiUrlAllGdaps { get { return $"{GdapBaseEndpoint}/v1/delegatedAdminRelationships"; } }

        private async Task<string?> getToken(Resource resource)
        {
            var authenticationResult = await tokenProvider.GetTokenAsync(resource);
            return authenticationResult?.AccessToken;
        }

        /// <summary>
        /// Create File for terminate list
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<bool> CreateTerminateRelationshipFile(ExportImport type)
        {
            try
            {
                var fileName = "gdapRelationship_terminate";
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var path = $"{Constants.InputFolderPath}/gdapRelationship/{fileName.Trim().ToLower()}.{Helper.GetExtenstion(type)}";
                //Create a empty list;
                var dummyList = new List<DelegatedAdminRelationship>();
                if (!File.Exists(path))
                {
                    await exportImportProvider.WriteAsync(dummyList, $"{Constants.InputFolderPath}/gdapRelationship/{fileName}.{Helper.GetExtenstion(type)}");
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

        private async Task<DelegatedAdminRelationshipErrored?> PostGdapRelationship(string url, DelegatedAdminRelationship delegatedAdminRelationship)
        {
            try
            {
                logger
                    .LogInformation($"GDAP Request:\n{delegatedAdminRelationship.DisplayName}-{delegatedAdminRelationship.Customer.TenantId}\n{JsonConvert.SerializeObject(delegatedAdminRelationship.AccessDetails.UnifiedRoles)}\n");
                var accessToken = await getToken(Resource.TrafficManager);
                var response = await protectedApiCallHelper.CallWebApiPostAndProcessResultAsync(url, accessToken,
                    JsonConvert.SerializeObject(delegatedAdminRelationship, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }).ToString());

                string userResponse = GetUserResponse(response.StatusCode);
                Console.WriteLine($"{delegatedAdminRelationship.Customer.DisplayName} - {userResponse}");

                logger
                  .LogInformation($"GDAP Response:\n{delegatedAdminRelationship.DisplayName} {delegatedAdminRelationship.Customer.TenantId}\n {response.Content.ReadAsStringAsync().Result} \n");

                var relationshipObject = response.IsSuccessStatusCode
                       ? JsonConvert.DeserializeObject<DelegatedAdminRelationshipErrored>(response.Content.ReadAsStringAsync().Result)
                       : new DelegatedAdminRelationshipErrored()
                       {
                           DisplayName = delegatedAdminRelationship.DisplayName,
                           Id = string.Empty,
                           Duration = delegatedAdminRelationship.Duration,
                           Customer = new DelegatedAdminRelationshipCustomerParticipant() { DisplayName = delegatedAdminRelationship.Customer.DisplayName, TenantId = delegatedAdminRelationship.Customer.TenantId },
                           Partner = new DelegatedAdminRelationshipParticipant() { TenantId = delegatedAdminRelationship.Partner.TenantId },
                           ErrorDetail = response.Content.ReadAsStringAsync().Result
                       };
                return relationshipObject;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine($"{delegatedAdminRelationship.DisplayName} - Unexpected error.");
                return new DelegatedAdminRelationshipErrored()
                {
                    DisplayName = delegatedAdminRelationship.DisplayName,
                    Id = string.Empty,
                    Duration = delegatedAdminRelationship.Duration,
                    Customer = new DelegatedAdminRelationshipCustomerParticipant() { DisplayName = delegatedAdminRelationship.Customer.DisplayName, TenantId = delegatedAdminRelationship.Customer.TenantId },
                    Partner = new DelegatedAdminRelationshipParticipant() { TenantId = delegatedAdminRelationship.Partner.TenantId },
                    ErrorDetail = ex.Message
                };
            }


        }

        private string GetUserResponse(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                    return "Created.";
                case HttpStatusCode.Conflict: return "GDAP Relationship name already exits.";
                case HttpStatusCode.NotFound: return "GDAP relationship is already created but User does not have permissions to approve a relationship.";
                case HttpStatusCode.Forbidden: return "Please check if DAP relationship exists with the Customer or \nif Conditional Access Policy (CAP) is applied.";
                case HttpStatusCode.Unauthorized: return "Unauthorized. Please make sure your Sign-in credentials are correct and MFA enabled.";
                case HttpStatusCode.BadRequest: return "Please check input setup for Customers and ADRoles.";
                default: return "Failed to create. The customer does not exist, DAP relationship is missing or Conditional Access Policy (CAP) is applied.";
            }

        }

        /// <summary>
        /// Fetch of details of GDAP relationship.
        /// </summary>
        /// <param name="nextLink">For fetching paginated query.</param>
        /// <returns>GDAP relationship object</returns>
        private async Task<JObject?> GetGdapRelationships(string? nextLink = null)
        {
            var url = WebApiUrlAllGdaps;
            if (string.IsNullOrEmpty(nextLink))
            {
                url = $"{url}?$count=true";
            }
            else { url = nextLink; }
            var accessToken = await getToken(Resource.TrafficManager);
            var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync(url, accessToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception("Unauthorized. Please make sure your Sign-in credentials are correct and MFA enabled.");
                }
            }

            var readAsStringAsync = response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(readAsStringAsync.Result);
        }

        /// <summary>
        /// Update call to protected web API
        /// </summary>
        /// <param name="delegatedAdminRelationship"></param>
        /// <returns></returns>
        private async Task<DelegatedAdminRelationship> UpdateTerminationStatus(DelegatedAdminRelationship delegatedAdminRelationship)
        {
            try
            {
                var status = new UpdateStatus() { action = "terminate" };
                var data = JsonConvert.SerializeObject(status).ToString();
                var url = $"{WebApiUrlAllGdaps}/{delegatedAdminRelationship.Id}/requests";
                var accessToken = await getToken(Resource.TrafficManager);
                var response = await protectedApiCallHelper.CallWebApiPostAndProcessResultAsync(url, accessToken, data);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("Unauthorized. Please make sure your Sign-in credentials are correct and MFA enabled.");
                    }
                }

                delegatedAdminRelationship.Status = DelegatedAdminRelationshipStatus.TerminationRequested;
                return delegatedAdminRelationship;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine($"{delegatedAdminRelationship.Id} - Unexpected error.");
                return delegatedAdminRelationship;
            }
        }

        /// <summary>
        /// Fetch of details of GDAP relationship.
        /// </summary>
        /// <param name="granularRelationshipId">GDAP relationshipId</param>
        /// <returns>GDAP relationship object</returns>
        private async Task<DelegatedAdminRelationship> GetGdapRelationship(string granularRelationshipId)
        {
            try
            {
                var url = WebApiUrlAllGdaps;
                if (!string.IsNullOrEmpty(granularRelationshipId))
                {
                    url = $"{url}/{granularRelationshipId}";
                }
                else
                {
                    throw new Exception("GDAP relationship id missing.");
                }

                var accessToken = await getToken(Resource.TrafficManager);
                var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync(url, accessToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("Unauthorized. Please make sure your Sign-in credentials are correct and MFA enabled.");
                    }
                }
                return JsonConvert.DeserializeObject<DelegatedAdminRelationship>(response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine($"{granularRelationshipId} - Unexpected error.");
                return new DelegatedAdminRelationship()
                {
                    Id = granularRelationshipId,
                    Status = DelegatedAdminRelationshipStatus.Approved
                };
            }
        }

        /// <summary>
        ///  Track and update the status of existing GDAP relationship status.
        /// </summary>
        /// <param name="type">Export type "JSON" or "CSV" based on user selection.</param>
        /// <returns></returns>
        public async Task<bool> RefreshGDAPRequestAsync(ExportImport type)
        {
            var fileNames = new[] { "gdapRelationship", "gdapRelationship_terminate" };

            foreach (var fileName in fileNames)
            {
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var path = $"{Constants.InputFolderPath}/gdapRelationship/{fileName.Trim().ToLower()}.{Helper.GetExtenstion(type)}";
                // checking file exist or not
                if (File.Exists(path))
                {
                    try
                    {
                        var gDapRelationshipList = await exportImportProvider.ReadAsync<DelegatedAdminRelationship>(path);
                        Console.WriteLine($"Reading file {path}");

                        var statusToUpdate = CheckStatus(fileName);
                        var inputRequest = gDapRelationshipList?.Where(x => x.Status.HasValue && statusToUpdate.Contains(x.Status.Value)).ToList();
                        var remainingDataList = gDapRelationshipList?.Where(x => !x.Status.HasValue || !statusToUpdate.Contains(x.Status.Value)).ToList();

                        if (!inputRequest.Any())
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"No '{statusToUpdate[0]}' relationships found in the file {path}\n");
                            Console.ResetColor();
                            continue;
                        }

                        IEnumerable<string>? gdapIdList = inputRequest?.Select(p => p.Id.ToString());
                        var responseList = new ConcurrentBag<DelegatedAdminRelationship>();
                        Console.WriteLine("Refreshing relationship(s) status...");
                        var options = new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = 10
                        };
                        protectedApiCallHelper.setHeader(false);
                        if (gdapIdList.Any())
                        {
                            await Parallel.ForEachAsync(gdapIdList, options, async (gdapId, cancellationToken) =>
                            {
                                responseList.Add(await GetGdapRelationship(gdapId));
                            });
                        }


                        if (remainingDataList.Any())
                        {
                            foreach (var item in remainingDataList)
                            {
                                responseList.Add(item);
                            }
                        }

                        Console.WriteLine($"Downloaded latest statuses of GDAP Relationship(s) at {Constants.InputFolderPath}//gdapRelationship/{fileName}.{Helper.GetExtenstion(type)}\n");
                        await exportImportProvider.WriteAsync(responseList, $"{Constants.InputFolderPath}/gdapRelationship/{fileName}.{Helper.GetExtenstion(type)}");
                    }
                    catch (IOException ex)
                    {
                        logger.LogError(ex.Message);
                        Console.WriteLine("Make sure the file is closed before running the operation.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }
                }
                else { continue; }
            }

            return true;
        }

        private List<DelegatedAdminRelationshipStatus> CheckStatus(string fileName)
        {
            if (fileName == "gdapRelationship_terminate")
            {
                return new List<DelegatedAdminRelationshipStatus>()
            { DelegatedAdminRelationshipStatus.TerminationRequested, DelegatedAdminRelationshipStatus.Terminating };
            }

            return new List<DelegatedAdminRelationshipStatus>()
            { DelegatedAdminRelationshipStatus.Approved };
        }

        /// <summary>
        ///  Create GDAP relationship Object.
        /// </summary>
        /// <param name="type">Export type "JSON" or "CSV" based on user selection.</param>
        /// <param name="inputCustomer"></param>
        /// <param name="unifiedRoles"></param>
        /// <param name="delegatedAdminRelationshipRequests"></param>
        /// <returns></returns>
        public async Task<(List<DelegatedAdminRelationship>? successfulGDAP, List<DelegatedAdminRelationshipErrored>? failedGDAP)> CreateGDAPRequestAsync(
            ExportImport type,
            List<DelegatedAdminRelationshipRequest> inputCustomer,
            List<UnifiedRole> roleList)
        {
            try
            {
                // var exportImportProvider = exportImportProviderFactory.Create(type);
                // var inputCustomer = await exportImportProvider.ReadAsync<DelegatedAdminRelationshipRequest>($"{Constants.InputFolderPath}/customers.{Helper.GetExtenstion(type)}");
                // var inputAdRole = await exportImportProvider.ReadAsync<ADRole>($"{Constants.InputFolderPath}/ADRoles.{Helper.GetExtenstion(type)}");

                //IEnumerable<UnifiedRole> roleList = inputAdRole.Select(x => new UnifiedRole() { RoleDefinitionId = x.Id.ToString() });

                //var option = Helper.UserConfirmation($"Warning: There are {inputAdRole.Count} roles configured for creating GDAP relationship(s), are you sure you want to continue?");
                //if (!option)
                //{
                //    return (null, null);
                //}
                var gdapList = new List<DelegatedAdminRelationship>();
                foreach (var item in inputCustomer)
                {
                    var gdapRelationship = new DelegatedAdminRelationship()
                    {
                        Customer = new DelegatedAdminRelationshipCustomerParticipant() { TenantId = item.CustomerTenantId, DisplayName = item.OrganizationDisplayName },
                        Partner = new DelegatedAdminRelationshipParticipant() { TenantId = item.PartnerTenantId.ToString() },
                        DisplayName = item.Name,
                        Duration = $"P{item.Duration}D",
                        AccessDetails = new DelegatedAdminAccessDetails() { UnifiedRoles = roleList },
                    };
                    gdapList.Add(gdapRelationship);

                }

                Console.WriteLine("Creating new relationship(s)...");
                var url = $"{WebApiUrlAllGdaps}/migrate";
                var allGdapRelationList = new ConcurrentBag<DelegatedAdminRelationshipErrored>();

                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 5,
                };
                protectedApiCallHelper.setHeader(false);
                await Parallel.ForEachAsync(gdapList, options, async (g, _) =>
                {
                    var result = await PostGdapRelationship(url, g);
                    if (result is null)
                        return;

                    allGdapRelationList.Add(result);
                });

                // filtering the Relationship based on success
                var successfulGDAP = allGdapRelationList.Where<DelegatedAdminRelationship>(item => item.Status == DelegatedAdminRelationshipStatus.Approved).ToList();
                var failedGDAP = allGdapRelationList.Where(item => item.Status != DelegatedAdminRelationshipStatus.Approved || string.IsNullOrEmpty(item.Status.ToString())).ToList();
                // Console.WriteLine("Downloading GDAP Relationship(s)...");
                if (customProperties.ReplaceFileDuringUpdate && File.Exists($"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}"))
                {
                    Filehelper.RenameFolder($"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}");
                }
                //await exportImportProvider.WriteAsync(successfulGDAP, $"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}");
                //if (failedGDAP.Any())
                //{
                //    await exportImportProvider.WriteAsync<DelegatedAdminRelationshipErrored>((IEnumerable<DelegatedAdminRelationshipErrored>)failedGDAP, $"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship_failed.{Helper.GetExtenstion(type)}");
                //}
                // Console.WriteLine($"Downloaded new GDAP Relationship(s): SUCCESS[{successfulGDAP.Count}] | FAILED[{failedGDAP.Count}]");
                return (successfulGDAP, failedGDAP);
            }
            catch (IOException ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine("Make sure all input file(s) are closed before running the operation.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while save the Granular Relationship");
                logger.LogError(ex.Message);
            }
            return (null, null);
        }

        /// <summary>
        /// Export all GDAP relationship for partner tenant.
        /// </summary>
        /// <param name="type">Export type "JSON" or "CSV" based on user selection.</param>
        /// <returns></returns>
        public async Task<List<DelegatedAdminRelationship>?> GetAllGDAPAsync(ExportImport type)
        {
            try
            {
                //var exportImportProvider = exportImportProviderFactory.Create(type);
                var gdapList = new List<DelegatedAdminRelationship>();
                var nextLink = string.Empty;
                //Console.WriteLine("Downloading relationship(s)...");
                //Helper.ResetSpin("Page.. ");
                protectedApiCallHelper.setHeader(false);
                do
                {
                    var response = await GetGdapRelationships(nextLink);
                    var nextData = response.Properties().Where(p => p.Name.Contains("nextLink"));
                    nextLink = string.Empty;
                    //Helper.Spin();
                    if (nextData != null)
                    {
                        nextLink = (string?)nextData.FirstOrDefault();
                        if (!string.IsNullOrEmpty(nextLink))
                        {
                            Uri nextUri = new Uri(nextLink);
                            nextLink = WebApiUrlAllGdaps + nextUri.Query;
                        }
                    }
                    foreach (JProperty? child in response.Properties().Where(p => !p.Name.StartsWith("@")))
                    {
                        gdapList.AddRange(child.Value.Select(item => item.ToObject<DelegatedAdminRelationship>()));
                    }

                } while (!string.IsNullOrEmpty(nextLink));

                // await exportImportProvider.WriteAsync(gdapList, $"{Constants.OutputFolderPath}/ExistingGdapRelationship.{Helper.GetExtenstion(type)}");
                // Console.WriteLine($"\nDownloaded existing GDAP relationship(s): " + gdapList.Count);
                return gdapList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }


        /// <summary>
        /// Terminate GDAP relationships
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<bool> TerminateGDAPRequestAsync(ExportImport type)
        {
            try
            {
                var fileName = "gdapRelationship_terminate";
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var path = $"{Constants.InputFolderPath}/gdapRelationship/{fileName}.{Helper.GetExtenstion(type)}";
                var gDapRelationshipList = await exportImportProvider.ReadAsync<DelegatedAdminRelationship>(path);
                Console.WriteLine($"Reading file @ {path}");

                if (!gDapRelationshipList.Any())
                {
                    Console.WriteLine($"No relationships found to terminate, check the path {path}");
                    return true;
                }
                var inputRequest = gDapRelationshipList?.Where(x => x.Status == DelegatedAdminRelationshipStatus.Active).ToList();
                var remainingDataList = gDapRelationshipList?.Where(x => x.Status != DelegatedAdminRelationshipStatus.Active).ToList();

                if (!inputRequest.Any())
                {
                    Console.WriteLine($"No active relationships found to terminate, check the path {path}");
                    return true;
                }

                var option = Helper.UserConfirmation($"Warning: There are {inputRequest.Count()} GDAP relationship(s) to terminate, are you sure you want to continue?");
                if (!option)
                {
                    return true;
                }

                var responseList = new ConcurrentBag<DelegatedAdminRelationship>();
                protectedApiCallHelper.setHeader(false);

                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 5
                };

                await Parallel.ForEachAsync(inputRequest, options, async (g, cancellationToken) =>
                {
                    responseList.Add(await UpdateTerminationStatus(g));
                });

                if (remainingDataList.Any())
                {
                    foreach (var item in remainingDataList)
                    {
                        responseList.Add(item);
                    }
                }

                Console.WriteLine($"Termination request complete for active GDAP Relationship(s) from {Constants.InputFolderPath}//gdapRelationship/{fileName}.{Helper.GetExtenstion(type)}");
                await exportImportProvider.WriteAsync(responseList, $"{Constants.InputFolderPath}/gdapRelationship/{fileName}.{Helper.GetExtenstion(type)}");
            }
            catch (IOException ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine("Make sure the file is closed before running the operation.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return true;
        }
    }
}
