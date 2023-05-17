using GBM.Model;
using GBM.Utility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PartnerLed.Model;
using PartnerLed.Utility;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;

namespace PartnerLed.Providers
{
    internal class AccessAssignmentProvider : IAccessAssignmentProvider
    {
        /// <summary>
        /// The token provider.
        /// </summary>
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<AccessAssignmentProvider> logger;
        private readonly IExportImportProviderFactory exportImportProviderFactory;
        private readonly CustomProperties customProperties;

        /// <summary>
        /// AccessAssignment provider constructor.
        /// </summary>
        public AccessAssignmentProvider(ITokenProvider tokenProvider, AppSetting appSetting, IExportImportProviderFactory exportImportProviderFactory, ILogger<AccessAssignmentProvider> logger)
        {
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.exportImportProviderFactory = exportImportProviderFactory;
            protectedApiCallHelper = new ProtectedApiCallHelper(appSetting.Client);
            GraphBaseEndpoint = appSetting.MicrosoftGraphBaseEndpoint;
            GdapBaseEndpoint = appSetting.GdapBaseEndPoint;
            customProperties = appSetting.customProperties;
        }

        protected ProtectedApiCallHelper protectedApiCallHelper;

        /// <summary>
        /// Base endpoint for Traffic Manager
        /// </summary>
        private string GdapBaseEndpoint { get; set; }

        /// <summary>
        /// Base endpoint for Graph
        /// </summary>
        private string GraphBaseEndpoint { get; set; }

        /// <summary>
        /// URLs of the protected Web APIs to call Graph Endpoint
        /// </summary>
        private string graphEndpoint { get { return $"{GraphBaseEndpoint}/v1.0/groups?$filter=securityEnabled+eq+true&$select=id,displayName"; } }

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
        public async Task<bool> CreateDeleteAccessAssignmentFile(ExportImport type)
        {
            try
            {
                var fileName = "accessAssignment_delete";
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var path = $"{Constants.InputFolderPath}/accessAssignment/{fileName.Trim().ToLower()}.{Helper.GetExtenstion(type)}";
                //Create a dummy list;
                var dummyList = new List<DelegatedAdminAccessAssignmentRequest>();
                if (!File.Exists(path))
                {
                    await exportImportProvider.WriteAsync(dummyList, $"{Constants.InputFolderPath}/accessAssignment/{fileName}.{Helper.GetExtenstion(type)}");
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
        /// Generate the Security Group list from Graph Endpoint.
        /// </summary>
        /// <returns> true </returns>
        public async Task<List<SecurityGroup?>?> ExportSecurityGroup(ExportImport type)
        {
            try
            {
                var url = graphEndpoint;
                var nextLink = string.Empty;
                var securityGroup = new List<SecurityGroup?>();

                Console.WriteLine("\nUpdating Security Groups");
                protectedApiCallHelper.setHeader(true);

                do
                {
                    var accessToken = await getToken(Resource.GraphManager);
                    if (!string.IsNullOrEmpty(nextLink))
                    {
                        url = nextLink;
                    }

                    var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync(url, accessToken);
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result) as JObject;
                        var nextData = result.Properties().Where(p => p.Name.Contains("nextLink"));
                        nextLink = string.Empty;
                        if (nextData != null)
                        {
                            nextLink = (string?)nextData.FirstOrDefault();
                        }

                        foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
                        {
                            securityGroup.AddRange(child.Value.Select(item => item.ToObject<SecurityGroup>()));
                        }
                    }
                    else
                    {
                        string userResponse = "Failed to get Security Groups.";
                        Console.WriteLine($"{userResponse}");
                    }
                } while (!string.IsNullOrEmpty(nextLink));
                //var writer = exportImportProviderFactory.Create(type);
                //await writer.WriteAsync(securityGroup, $"{Constants.OutputFolderPath}/securityGroup.{Helper.GetExtenstion(type)}");
                // Console.WriteLine($"Downloaded Security Groups: " + securityGroup.Count);
                return securityGroup;
            }

            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Track and update the status of existing Access Assignment objects.
        /// </summary>
        /// <returns>true </returns>
        public async Task<bool> RefreshAccessAssignmentRequest(ExportImport type)
        {
            var fileNames = new[] { "accessAssignment", "accessAssignment_update", "accessAssignment_delete" };

            var exportImportProvider = exportImportProviderFactory.Create(type);
            var securityRolepath = $"{Constants.InputFolderPath}/securityGroup.{Helper.GetExtenstion(type)}";
            var securityGroupList = await exportImportProvider.ReadAsync<SecurityGroup>(securityRolepath);
            foreach (var fileName in fileNames)
            {
                var accessAssignmentFilepath = $"{Constants.InputFolderPath}/accessAssignment/{fileName.Trim().ToLower()}.{Helper.GetExtenstion(type)}";
                if (File.Exists(accessAssignmentFilepath))
                {
                    try
                    {
                        var accessAssignmentList = await exportImportProvider.ReadAsync<DelegatedAdminAccessAssignmentRequest>(accessAssignmentFilepath);
                        Console.WriteLine($"Reading files @ {accessAssignmentFilepath}");
                        var statusToUpdate = GetStatus(fileName);
                        var inputRequest = accessAssignmentList?.Where(x => statusToUpdate.Contains(x.Status?.ToLower())).ToList();
                        var remainingDataList = accessAssignmentList?.Where(x => !statusToUpdate.Contains(x.Status?.ToLower())).ToList();

                        if (!inputRequest.Any())
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"No '{statusToUpdate[0]}' access assignments found in the file {accessAssignmentFilepath}\n");
                            Console.ResetColor();
                            continue;
                        }

                        var responseList = new ConcurrentBag<DelegatedAdminAccessAssignmentRequest>();
                        Console.WriteLine("Refreshing status of access assignment(s)..");
                        protectedApiCallHelper.setHeader(false);
                        var options = new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = 10
                        };
                        if (inputRequest.Any())
                        {
                            await Parallel.ForEachAsync(inputRequest, options, async (delegatedAdminAccessAssignmentRequest, cancellationToken) =>
                            {
                                responseList.Add(await GetDelegatedAdminAccessAssignment(delegatedAdminAccessAssignmentRequest, delegatedAdminAccessAssignmentRequest.AccessAssignmentId, securityGroupList));
                            });
                        }

                        if (remainingDataList.Any())
                        {
                            foreach (var item in remainingDataList)
                            {
                                responseList.Add(item);
                            }
                        }
                        Console.WriteLine("Downloading Access Assignment(s)...");
                        await exportImportProvider.WriteAsync(responseList, $"{Constants.InputFolderPath}/accessAssignment/{fileName}.{Helper.GetExtenstion(type)}");
                        Console.WriteLine($"Downloaded Access Assignment(s) at {Constants.InputFolderPath}/accessAssignment/{fileName}.{Helper.GetExtenstion(type)}\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error occurred while save the Access Assignment");
                        logger.LogError(ex.Message);
                    }
                }
                else { continue; }
            }

            return true;
        }

        private List<string> GetStatus(string file)
        {
            switch (file)
            {
                case "accessAssignment":
                    return new List<string>() { "pending", "active" };
                case "accessAssignment_update":
                    return new List<string>() { "pending", "active" };
                case "accessAssignment_delete":
                    return new List<string>() { "deleting" };
                default: return new List<string>() { "active" };
            }
        }

        /// <summary>
        /// Create bulk Delegated Admin relationship access assignment.
        /// </summary>
        /// <returns>true </returns>
        public async Task<(IEnumerable<DelegatedAdminAccessAssignmentRequest> successfulAccessAssignment, IEnumerable<DelegatedAdminAccessAssignmentRequest> failedAccessAssignment
            )> CreateAccessAssignmentRequestAsync(
            ExportImport type,
            List<SecurityGroup> securityGroupList,
            List<ADRole> rolesForSecurityGroup,
            List<DelegatedAdminRelationship> gDapRelationshipList)
        {
            try
            {
                //var exportImportProvider = exportImportProviderFactory.Create(type);
                //var gDapFilepath = $"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}";
                //var gDapRelationshipList = await exportImportProvider.ReadAsync<DelegatedAdminRelationship>(gDapFilepath);
                //Console.WriteLine($"Reading files @ {gDapFilepath}");

                //var azureRoleFilePath = $"{Constants.InputFolderPath}/ADRoles.{Helper.GetExtenstion(type)}";
                //Console.WriteLine($"Reading files @ {azureRoleFilePath}");
                //var inputAdRole = await exportImportProvider.ReadAsync<ADRole>(azureRoleFilePath);

                //var securityRolepath = $"{Constants.InputFolderPath}/securityGroup.{Helper.GetExtenstion(type)}";
                //Console.WriteLine($"Reading files @ {securityRolepath}");
                //var securityGroupList = await exportImportProvider.ReadAsync<SecurityGroup>(securityRolepath);

                //if (!securityGroupList.Any())
                //{
                //    throw new Exception($"No security groups found in gdapbulkmigration/securitygroup.{Helper.GetExtenstion(type)}");
                //}

                //if (securityGroupList.Any(item => string.IsNullOrEmpty(item.CommaSeperatedRoles)))
                //{
                //    throw new Exception($"One or more security groups do not have roles mapped in gdapbulkmigration/securitygroup.{Helper.GetExtenstion(type)}");
                //}

                //var option = Helper.UserConfirmation($"Waring: There are {securityGroupList.Count} Security Groups configured for Access Assignment, are you sure you want to continue with this?");
                //if (!option)
                //{
                //    return true;
                //}
                var list = new List<DelegatedAdminAccessAssignment>();
                var responseList = new List<DelegatedAdminAccessAssignmentRequest>();
                // get the unique AD roles 
                list.AddRange(from SecurityGroup? item in securityGroupList select GetAdminAccessAssignmentObject(item.Id, item.Roles, rolesForSecurityGroup));

                var inputList = gDapRelationshipList.Where(g => g.Status == DelegatedAdminRelationshipStatus.Active).ToList();

                try
                {
                    protectedApiCallHelper.setHeader(false);
                    foreach (var gdapRelationship in inputList)
                    {
                        var tasks = list?.Select(item => PostGranularAdminAccessAssignment(gdapRelationship, item, securityGroupList));
                        DelegatedAdminAccessAssignmentRequest?[] collection = await Task.WhenAll(tasks);
                        responseList.AddRange(collection);
                    }

                    //if (customProperties.ReplaceFileDuringUpdate && File.Exists($"{Constants.InputFolderPath}/accessAssignment/accessAssignment.{Helper.GetExtenstion(type)}"))
                    //{
                    //    Filehelper.RenameFolder($"{Constants.InputFolderPath}/accessAssignment/accessAssignment.{Helper.GetExtenstion(type)}");
                    //}

                    //separating the failed 
                    var failedStatus = new List<string> { "error", "failed" };
                    var successfulAccessAssignment = responseList.Where(item => !failedStatus.Contains(item.Status.ToLower()) && !string.IsNullOrEmpty(item.Status)).ToList();
                    var failedAccessAssignment = responseList.Where(item => string.IsNullOrEmpty(item.Status) || failedStatus.Contains(item.Status.ToLower())).ToList();

                    //if (failedAccessAssignment.Any())
                    //{
                    //    //Generating the failed file
                    //    await exportImportProvider.WriteAsync(failedAccessAssignment, $"{Constants.InputFolderPath}/accessAssignment/accessAssignment_failed.{Helper.GetExtenstion(type)}");
                    //}

                    //await exportImportProvider.WriteAsync(SucessfulAccessAssignment, $"{Constants.InputFolderPath}/accessAssignment/accessAssignment.{Helper.GetExtenstion(type)}");
                    //// Generating a UPDATE FILE 
                    //await exportImportProvider.WriteAsync(SucessfulAccessAssignment, $"{Constants.InputFolderPath}/accessAssignment/accessAssignment_update.{Helper.GetExtenstion(type)}");
                    Console.WriteLine($"Downloaded Access Assignment(s): SUCCESS[{successfulAccessAssignment.Count}] | FAILED[{failedAccessAssignment.Count}]");
                    return (successfulAccessAssignment, failedAccessAssignment);
                }
                catch
                {
                    Console.WriteLine($"Error occurred while save the Access Assignment");
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }


            return (null, null);
        }

        /// <summary>
        /// Create a Delegated Admin relationship access assignment.
        /// </summary>
        /// <param name="gdapRelationship"></param>
        /// <param name="data"></param>
        /// <param name="SecurityGroupList"></param>
        /// <returns>DelegatedAdminAccessAssignmentRequest</returns>
        public async Task<DelegatedAdminAccessAssignmentRequest?> PostGranularAdminAccessAssignment(DelegatedAdminRelationship gdapRelationship, DelegatedAdminAccessAssignment data, IEnumerable<SecurityGroup> SecurityGroupList)
        {
            try
            {
                var url = $"{WebApiUrlAllGdaps}/{gdapRelationship.Id}/accessAssignments";

                logger.LogInformation($"Assignment Request:\n{gdapRelationship.Id}\n{JsonConvert.SerializeObject(data.AccessDetails)}");
                var token = await getToken(Resource.TrafficManager);
                HttpResponseMessage response = await protectedApiCallHelper.CallWebApiPostAndProcessResultAsync(url, token, JsonConvert.SerializeObject(data,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }).ToString());

                string userResponse = GetUserResponse(response.StatusCode);
                Console.WriteLine($"{gdapRelationship.Customer.DisplayName} - SecurityGroup updated");

                var accessAssignmentObject = response.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<DelegatedAdminAccessAssignment>(response.Content.ReadAsStringAsync().Result)
                    : new DelegatedAdminAccessAssignment() { Status = "Failed", Id = string.Empty };

                logger.LogInformation($"Assignment Response:\n {gdapRelationship.Id}-{response.StatusCode} \n {response.Content.ReadAsStringAsync().Result} \n");

                return new DelegatedAdminAccessAssignmentRequest()
                {
                    GdapRelationshipId = gdapRelationship.Id,
                    Customer = gdapRelationship.Customer,
                    AccessAssignmentId = accessAssignmentObject.Id,
                    SecurityGroupId = accessAssignmentObject.AccessContainer?.AccessContainerId,
                    SecurityGroupName = GetGroupName(accessAssignmentObject.AccessContainer?.AccessContainerId, SecurityGroupList),
                    CommaSeperatedRoles = accessAssignmentObject.AccessDetails != null ?
                    string.Join(",", accessAssignmentObject.AccessDetails?.UnifiedRoles.Select(item => item.RoleDefinitionId)) : string.Empty,
                    Status = accessAssignmentObject.Status,
                    Etag = accessAssignmentObject.ETag,
                    CreatedDateTime = accessAssignmentObject.CreatedDateTime,
                    LastModifiedDateTime = accessAssignmentObject.LastModifiedDateTime,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return new DelegatedAdminAccessAssignmentRequest()
            {
                GdapRelationshipId = gdapRelationship.Id,
                Customer = gdapRelationship.Customer,
                SecurityGroupId = data.AccessContainer.AccessContainerId,
                SecurityGroupName = GetGroupName(data.AccessContainer.AccessContainerId, SecurityGroupList),
                CommaSeperatedRoles = string.Join(",", data.AccessDetails.UnifiedRoles.Select(item => item.RoleDefinitionId)),
                Status = "Failed"
            };
        }

        private string GetUserResponse(HttpStatusCode statusCode, bool updatedStatus = false)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                    return updatedStatus ? "No change" : "Created";
                case HttpStatusCode.Created:
                    return "Created.";
                case HttpStatusCode.Accepted:
                    return "Updated";
                case HttpStatusCode.NoContent:
                    return "Deleted";
                case HttpStatusCode.Conflict:
                    return "Access assignment already exits.";
                case HttpStatusCode.Forbidden: return "Please check if DAP relationship exists with the Customer.";
                case HttpStatusCode.Unauthorized: return "Unauthorized. Please make sure your Sign-in credentials are correct and MFA enabled.";
                case HttpStatusCode.BadRequest: return "Please check input setup for gdaprelationships and securitygroup configuration or Etag ";
                default: return "Failed to create. Please try again.";
            }

        }

        private string GetDeletionUserResponse(HttpStatusCode statusCode, bool updatedStatus = false)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                    return updatedStatus ? "No change" : "Deleted";
                case HttpStatusCode.Accepted:
                    return "Updated";
                case HttpStatusCode.NoContent:
                    return "Deleted";
                case HttpStatusCode.Conflict:
                    return "Access assignment already deleted.";
                case HttpStatusCode.Forbidden: return "Forbidden.";
                case HttpStatusCode.Unauthorized: return "Unauthorized. Please make sure your Sign-in credentials are correct and MFA enabled.";
                case HttpStatusCode.BadRequest: return "Please check input setup for gdaprelationships and securitygroup configuration or Etag ";
                default: return "Failed to delete. Please try again.";
            }

        }

        /// <summary>
        /// Gets the Delegated Admin relationship access assignment for a given Delegated Admin relationship ID.
        /// </summary>
        /// <param name="delegatedAdminAccessAssignmentRequest"></param>
        /// <param name="accessAssignmentId"></param>
        /// <param name="SecurityGroupList"></param>
        /// <returns>DelegatedAdminAccessAssignmentRequest</returns>
        private async Task<DelegatedAdminAccessAssignmentRequest> GetDelegatedAdminAccessAssignment(DelegatedAdminAccessAssignmentRequest delegatedAdminAccessAssignmentRequest, string accessAssignmentId, IEnumerable<SecurityGroup> SecurityGroupList)
        {
            var gdapId = delegatedAdminAccessAssignmentRequest.GdapRelationshipId;
            var CustomerDetails = delegatedAdminAccessAssignmentRequest.Customer;
            try
            {
                var url = $"{WebApiUrlAllGdaps}/{gdapId}/accessAssignments/{accessAssignmentId}";
                var token = await getToken(Resource.TrafficManager);
                var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync(url, token);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var accessAssignmentObject = JsonConvert.DeserializeObject<DelegatedAdminAccessAssignment>(response.Content.ReadAsStringAsync().Result);

                    var delegatedAccessAssignmentReq = new DelegatedAdminAccessAssignmentRequest()
                    {
                        GdapRelationshipId = gdapId,
                        Customer = CustomerDetails,
                        AccessAssignmentId = accessAssignmentId,
                        SecurityGroupId = accessAssignmentObject.AccessContainer.AccessContainerId,
                        SecurityGroupName = GetGroupName(accessAssignmentObject.AccessContainer.AccessContainerId, SecurityGroupList),
                        CommaSeperatedRoles = string.Join(",", accessAssignmentObject.AccessDetails.UnifiedRoles.Select(item => item.RoleDefinitionId)),
                        Status = accessAssignmentObject.Status,
                        Etag = accessAssignmentObject.ETag,
                        CreatedDateTime = accessAssignmentObject.CreatedDateTime,
                        LastModifiedDateTime = accessAssignmentObject.LastModifiedDateTime,
                    };
                    return delegatedAccessAssignmentReq;
                }

                return new DelegatedAdminAccessAssignmentRequest()
                {
                    GdapRelationshipId = gdapId,
                    AccessAssignmentId = accessAssignmentId,
                    Customer = CustomerDetails,
                    Status = "Failed"
                };

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine($"{gdapId} - Unexpected error.");
                return new DelegatedAdminAccessAssignmentRequest()
                {
                    GdapRelationshipId = gdapId,
                    AccessAssignmentId = accessAssignmentId,
                    Customer = CustomerDetails,
                    Status = "Error"
                };
            }
        }

        /// <summary>
        /// Get Security Group Name
        /// </summary>
        /// <param name="securityGroupId"></param>
        /// <param name="SecurityGroupList"></param>
        /// <returns></returns>
        private string GetGroupName(string securityGroupId, IEnumerable<SecurityGroup> SecurityGroupList)
        {
            if (string.IsNullOrEmpty(securityGroupId))
            {
                return string.Empty;
            }
            var sgObject = SecurityGroupList.Where(SG => SG.Id == securityGroupId.Trim()).FirstOrDefault();
            return sgObject != null ? sgObject.DisplayName.ToString() : string.Empty;
        }

        /// <summary>
        /// Create Delegated Access Assignment Object.
        /// </summary>
        /// <param name="SecurityGrpId"></param>
        /// <param name="roleList"></param>
        /// <returns>DelegatedAdminAccessAssignment</returns>
        private DelegatedAdminAccessAssignment GetAdminAccessAssignmentObject(string SecurityGrpId, IEnumerable<UnifiedRole> roleList, List<ADRole> ADRolesList, string? accessAssignmentId = null)
        {
            var validatedRoles = ValidateRole(roleList, ADRolesList);
            if (string.IsNullOrEmpty(accessAssignmentId))
            {
                return new DelegatedAdminAccessAssignment() { AccessContainer = new DelegatedAdminAccessContainer() { AccessContainerId = SecurityGrpId, AccessContainerType = DelegatedAdminAccessContainerType.SecurityGroup }, AccessDetails = new DelegatedAdminAccessDetails() { UnifiedRoles = validatedRoles } };
            }
            return new DelegatedAdminAccessAssignment() { Id = accessAssignmentId, AccessContainer = new DelegatedAdminAccessContainer() { AccessContainerId = SecurityGrpId, AccessContainerType = DelegatedAdminAccessContainerType.SecurityGroup }, AccessDetails = new DelegatedAdminAccessDetails() { UnifiedRoles = validatedRoles } };
        }

        /// <summary>
        /// Validate the roles.
        /// </summary>
        /// <param name="roles"></param>
        /// <param name="ADRoles"></param>
        /// <returns></returns>
        private IEnumerable<UnifiedRole?> ValidateRole(IEnumerable<UnifiedRole> roles, List<ADRole> ADRoles)
        {
            var unifiedRoleList = roles.ToList();
            var validateRoles = new List<UnifiedRole>();
            foreach (var role in unifiedRoleList)
            {
                var adRole = ADRoles.Where(item => item.Id == role.RoleDefinitionId || item.Name.ToLower() == role.RoleDefinitionId.ToLower().Trim()).FirstOrDefault();
                if (adRole != null)
                {
                    validateRoles.Add(new UnifiedRole() { RoleDefinitionId = adRole.Id });
                }
            }

            return validateRoles;
        }

        /// <summary>
        /// Update bulk Delegated Admin relationship access assignment.
        /// </summary>
        /// <returns>true </returns>
        public async Task<List<DelegatedAdminAccessAssignmentRequest>?> UpdateAccessAssignmentRequestAsync(
            ExportImport type,
            List<ADRole> roles,
            SecurityGroup securityGroup,
            IEnumerable<DelegatedAdminAccessAssignmentRequest> accessAssignmentList)
        {
            try
            {
                // var exportImportProvider = exportImportProviderFactory.Create(type);
                // var azureRoleFilePath = $"{Constants.InputFolderPath}/ADRoles.{Helper.GetExtenstion(type)}";
                // Console.WriteLine($"Reading file @ {azureRoleFilePath}");
                // var inputAdRole = await exportImportProvider.ReadAsync<ADRole>(azureRoleFilePath);

                // var securityRolepath = $"{Constants.InputFolderPath}/securityGroup.{Helper.GetExtenstion(type)}";
                // Console.WriteLine($"Reading file @ {securityRolepath}");
                // var accessAssignmentFilepath = $"{Constants.InputFolderPath}/accessAssignment/accessAssignment_update.{Helper.GetExtenstion(type)}";
                // var accessAssignmentList = await exportImportProvider.ReadAsync<DelegatedAdminAccessAssignmentRequest>(accessAssignmentFilepath);
                // Console.WriteLine($"Reading file @ {accessAssignmentFilepath}");
                //var inputRequest = accessAssignmentList?.Where(x => x.Status.ToLower() == "active").ToList();
                //var remainingDataList = accessAssignmentList?.Where(x => x.Status.ToLower() != "active").ToList();
                // var securityGroupList = await exportImportProvider.ReadAsync<SecurityGroup>(securityRolepath);

                //var assignment = await GetDelegatedAdminAccessAssignment(delegatedAdminAccessAssignmentRequest, delegatedAdminAccessAssignmentRequest.AccessAssignmentId, securityGroupList);
                //var accessAssignmentList = new List<DelegatedAdminAccessAssignmentRequest> { assignment };

                //if (!inputRequest.Any())
                //{
                //    throw new Exception("Error while processing the input. Incorrect data provided for processing. Please check the input file.");
                //}


                //if (inputRequest.Any(item => string.IsNullOrEmpty(item.CommaSeperatedRoles)))
                //{
                //    throw new Exception($"One or more security groups do not have roles mapped in GDAPBulkMigration/operations/accessAssignment_update.{Helper.GetExtenstion(type)}");
                //}

                //var option = Helper.UserConfirmation($"Waring: There are {inputRequest.Count()} access assignment for update, are you sure you want to continue?");
                //if (!option)
                //{
                //    return null;
                //}
                var list = new List<DelegatedAdminAccessAssignment>();
                // get the unique AD roles 
                //list.AddRange(from DelegatedAdminAccessAssignmentRequest? item in assignments select GetAdminAccessAssignmentObject(item.SecurityGroupId, item.Roles, roles, item.AccessAssignmentId));
                var responseList = new List<DelegatedAdminAccessAssignmentRequest>();
                Console.WriteLine("Updating Access Assignment..");
                foreach (var accessAssignment in accessAssignmentList)
                {
                    protectedApiCallHelper.setHeader(false);
                    var tasks = list?.Where(item => item.Id == accessAssignment.AccessAssignmentId)
                                     .Select(item => UpdateDelegatedAdminAccessAssignment(accessAssignment, item, new List<SecurityGroup> { securityGroup }));
                    DelegatedAdminAccessAssignmentRequest?[] collection = await Task.WhenAll(tasks);
                    responseList.AddRange(collection);
                }

                //if (remainingDataList.Any())
                //{
                //    responseList.AddRange(remainingDataList);
                //}

                //await exportImportProvider.WriteAsync(responseList, $"{Constants.InputFolderPath}/accessAssignment/accessAssignment_update.{Helper.GetExtenstion(type)}");
                Console.WriteLine($"Downloaded Access Assignment(s): {responseList.Count}");
                return responseList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Update the Delegated Admin relationship access assignment for a given Delegated Admin relationship ID.
        /// </summary>
        /// <param name="OldDelegatedAdminAccessAssignmentRequest"></param>
        /// <param name="newData"></param>
        /// <param name="SecurityGroupList"></param>
        /// <returns>DelegatedAdminAccessAssignmentRequest</returns>
        private async Task<DelegatedAdminAccessAssignmentRequest> UpdateDelegatedAdminAccessAssignment(DelegatedAdminAccessAssignmentRequest OldDelegatedAdminAccessAssignmentRequest, DelegatedAdminAccessAssignment newData, IEnumerable<SecurityGroup> SecurityGroupList)
        {
            try
            {
                //Merging Data
                newData.LastModifiedDateTime = OldDelegatedAdminAccessAssignmentRequest.LastModifiedDateTime;
                newData.CreatedDateTime = OldDelegatedAdminAccessAssignmentRequest.CreatedDateTime;
                newData.Id = OldDelegatedAdminAccessAssignmentRequest.AccessAssignmentId;

                var eTag = OldDelegatedAdminAccessAssignmentRequest.Etag;
                var gdapRelationshipId = OldDelegatedAdminAccessAssignmentRequest.GdapRelationshipId;

                var url = $"{WebApiUrlAllGdaps}/{gdapRelationshipId}/accessAssignments/{OldDelegatedAdminAccessAssignmentRequest.AccessAssignmentId}";

                logger.LogInformation($"Assignment Request:\n{gdapRelationshipId}\n{JsonConvert.SerializeObject(newData.AccessDetails)}");
                var token = await getToken(Resource.TrafficManager);
                HttpResponseMessage response = await protectedApiCallHelper.CallWebApiPatchAndProcessResultAsync(url, token, eTag, JsonConvert.SerializeObject(newData,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }).ToString());

                string userResponse = GetUserResponse(response.StatusCode, true);
                Console.WriteLine($"{gdapRelationshipId}-{userResponse}");

                logger.LogInformation($"Assignment Response:\n {gdapRelationshipId}-{response.StatusCode} \n {response.Content.ReadAsStringAsync().Result} \n");

                var reqObj = new DelegatedAdminAccessAssignmentRequest()
                {
                    GdapRelationshipId = gdapRelationshipId,
                    Customer = OldDelegatedAdminAccessAssignmentRequest.Customer,
                    AccessAssignmentId = newData.Id,
                    SecurityGroupId = newData.AccessContainer.AccessContainerId,
                    CreatedDateTime = newData.CreatedDateTime,
                    SecurityGroupName = GetGroupName(newData.AccessContainer.AccessContainerId, SecurityGroupList),
                    CommaSeperatedRoles = string.Join(",", newData.AccessDetails.UnifiedRoles.Select(item => item.RoleDefinitionId)),
                    LastModifiedDateTime = newData.LastModifiedDateTime
                };

                if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                {
                    reqObj.Status = response.StatusCode == HttpStatusCode.Accepted ? "pending" : response.StatusCode == HttpStatusCode.OK ? "active" : "Failed";
                    reqObj.Etag = response.StatusCode == HttpStatusCode.Accepted ? string.Empty : eTag;
                    return reqObj;
                }
                reqObj.Status = "Failed";
                return reqObj;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return new DelegatedAdminAccessAssignmentRequest()
                {
                    GdapRelationshipId = OldDelegatedAdminAccessAssignmentRequest.GdapRelationshipId,
                    AccessAssignmentId = OldDelegatedAdminAccessAssignmentRequest.AccessAssignmentId,
                    Customer = OldDelegatedAdminAccessAssignmentRequest.Customer,
                    Status = "Error"
                };
            }
        }

        /// <summary>
        /// Delete the Delegated Admin relationship access assignment for a given Delegated Admin relationship ID.
        /// </summary>
        /// <param name="delegatedAdminAccessAssignmentRequest"></param>
        /// <returns>DelegatedAdminAccessAssignmentRequest</returns>
        private async Task<DelegatedAdminAccessAssignmentRequest> DeleteDelegatedAdminAccessAssignment(DelegatedAdminAccessAssignmentRequest delegatedAdminAccessAssignmentRequest)
        {
            var gdapRelationshipId = delegatedAdminAccessAssignmentRequest.GdapRelationshipId;
            var accessAssignmentId = delegatedAdminAccessAssignmentRequest.AccessAssignmentId;
            var eTag = delegatedAdminAccessAssignmentRequest.Etag;
            try
            {
                var url = $"{WebApiUrlAllGdaps}/{gdapRelationshipId}/accessAssignments/{accessAssignmentId}";
                var token = await getToken(Resource.TrafficManager);
                var response = await protectedApiCallHelper.CallWebApiAndDeleteProcessResultAsync(url, token, eTag);

                string userResponse = GetDeletionUserResponse(response.StatusCode);
                Console.WriteLine($"{accessAssignmentId}-{userResponse}");

                var responseObj = delegatedAdminAccessAssignmentRequest;
                if (response != null && response.IsSuccessStatusCode)
                {
                    responseObj.Status = "Deleting";
                    return responseObj;
                }
                responseObj.Status = "Failed";
                return responseObj;

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                delegatedAdminAccessAssignmentRequest.Status = "Errored";
                return delegatedAdminAccessAssignmentRequest;
            }
        }

        /// <summary>
        /// Delete bulk Delegated Admin relationship access assignment.
        /// </summary>
        /// <returns>true </returns>
        public async Task<bool> DeleteAccessAssignmentRequestAsync(ExportImport type)
        {
            try
            {
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var accessAssignmentFilepath = $"{Constants.InputFolderPath}/accessAssignment/accessAssignment_delete.{Helper.GetExtenstion(type)}";
                var accessAssignmentList = await exportImportProvider.ReadAsync<DelegatedAdminAccessAssignmentRequest>(accessAssignmentFilepath);
                Console.WriteLine($"Reading file @ {accessAssignmentFilepath}");
                var inputRequest = accessAssignmentList?.Where(x => x.Status.ToLower() == "active").ToList();
                var remainingDataList = accessAssignmentList?.Where(x => x.Status.ToLower() != "active").ToList();

                if (!inputRequest.Any())
                {
                    throw new Exception("Error while Processing the input. Incorrect newData provided for processing. Please check the input file.");
                }
                var option = Helper.UserConfirmation($"Waring: There are {inputRequest?.Count()} access assignment(s) that will terminated, are you sure you want to continue?");
                if (!option)
                {
                    return true;
                }
                var responseList = new ConcurrentBag<DelegatedAdminAccessAssignmentRequest>();
                Console.WriteLine("Deleting access assignment..");
                protectedApiCallHelper.setHeader(false);

                if (inputRequest.Any())
                {
                    inputRequest.ForEach((delegatedAdminAccessAssignment) =>
                    {
                        responseList.Add(DeleteDelegatedAdminAccessAssignment(delegatedAdminAccessAssignment).Result);
                    });
                }

                if (remainingDataList.Any())
                {
                    foreach (var item in remainingDataList)
                    {
                        responseList.Add(item);
                    }
                }

                await exportImportProvider.WriteAsync(responseList, $"{Constants.InputFolderPath}/accessAssignment/accessAssignment_delete.{Helper.GetExtenstion(type)}");
                Console.WriteLine($"Deleted access assignment(s) at {Constants.InputFolderPath}/accessAssignment/accessAssignment_delete.{Helper.GetExtenstion(type)}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return true;
        }
    }
}