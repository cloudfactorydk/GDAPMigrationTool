using Microsoft.Identity.Client;
using PartnerLed.Model;
using System.Net;
using System.Net.Http.Headers;

namespace PartnerLed.Providers
{
    internal class TokenProvider : ITokenProvider
    {

        // <summary>
        /// TokenProvider constructor.
        /// </summary>
        /// <param name="appSettings">The app settings.</param>
        public TokenProvider(AppSetting appSetting)
        {
            tokenAcquisitionHelper = new PublicAppUsingInteractive(appSetting.InteractiveApp);
            protectedApiCallHelper = new ProtectedApiCallHelper(appSetting.Client);
            graphendpoint = appSetting.MicrosoftGraphBaseEndpoint;
        }

        protected PublicAppUsingInteractive tokenAcquisitionHelper;
        private AuthenticationResult authenticationResult, graphAuthenticationResult, authenticationResultPartnerCenter;
        private ProtectedApiCallHelper protectedApiCallHelper;
        private string graphendpoint;

        private string partnerTenantId { get; set; }

        /// <summary>
        /// Scopes to request access to the protected Web API
        /// </summary>
        private static string[] Scopes { get; } = new string[] {
            "https://api.partnercustomeradministration.microsoft.com/PartnerCustomerDelegatedAdministration.ReadWrite.All" ,
        };
        
        /// <summary>
        /// Scopes to request access to the protected Web API (Here Partner Center)
        /// </summary>
        private static string[] PartnerCenterScopes { get; } = new string[] {
            "https://api.partnercenter.microsoft.com/user_impersonation" ,
        };

        /// <summary>
        /// Scopes to request access to the protected Web API (here Microsoft Graph)
        /// </summary>
        private static string[] ScopesGraph { get; } = new string[] {
            "https://graph.microsoft.com/Group.Read.All", "https://graph.microsoft.com/Application.ReadWrite.All" };


        public string getPartnertenantId() => partnerTenantId;

        private bool validateToken(AuthenticationResult result)
        {
            if (result != null && result.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult?> GetTokenAsync(Resource type)
        {
            AuthenticationResult? result = null;
            try
            {
                switch (type)
                {
                    case Resource.TrafficManager:
                        if (validateToken(authenticationResult))
                        {
                            return authenticationResult;
                        }
                        result = await AcquireTokenAsync(Scopes);
                        authenticationResult = result;
                        break;

                    case Resource.GraphManager:
                        if (validateToken(graphAuthenticationResult))
                        {
                            return graphAuthenticationResult;
                        }

                        result = await AcquireTokenAsync(ScopesGraph);
                        graphAuthenticationResult = result;
                        break;
                    case Resource.PartnerCenter:
                        if (validateToken(authenticationResultPartnerCenter))
                        {
                            return authenticationResultPartnerCenter;
                        }
                        result = await AcquireTokenAsync(PartnerCenterScopes);
                        authenticationResultPartnerCenter = result;
                        break;
                }
            }
            catch
            {
                Console.WriteLine("Failed to authenticate.");
            }

            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Access token missing.");
                Console.ResetColor();
            }

            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
        {

            AuthenticationResult result;
            try
            {
                // Console.WriteLine("Authenticating: Login via Web browser");
                result = await tokenAcquisitionHelper.AcquireATokenFromCacheOrInteractivelyAsync(scope);
            }
            catch
            {
                Console.WriteLine($"Exception while generating token");
                throw;
            }
            partnerTenantId = result.TenantId;
            return result;
        }

        public async Task<bool> CheckPrerequisite()
        {
            try
            {
                var gdapTokenResult = await AcquireTokenAsync(Scopes);
                if (validateToken(gdapTokenResult))
                {
                    return true;
                }
            }
            catch { }

            try
            {
                Console.WriteLine("Attempting to provision Partner Customer Delegated Administration API..");
                graphAuthenticationResult = await AcquireTokenAsync(ScopesGraph);
                protectedApiCallHelper.setHeader(true);

                HttpResponseMessage response = await protectedApiCallHelper
                    .CallWebApiPostAndProcessResultAsync($"{graphendpoint}/v1.0/servicePrincipals", graphAuthenticationResult.AccessToken,
                    "{'appId': '2832473f-ec63-45fb-976f-5d45a7d4bb91'}");

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                    case HttpStatusCode.Conflict:
                        return true;
                    default: return false;
                }
            }
            catch { return false; }
        }
    }
}
