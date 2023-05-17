// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;

namespace PartnerLed.Providers
{
    /// <summary>
    /// Helper class to call a protected API and process its result
    /// </summary>
    public class ProtectedApiCallHelper
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HttpClient used to call the protected API</param>
        public ProtectedApiCallHelper(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; private set; }

        private static object objLock = new Object();

        public void setHeader(bool isGraph)
        {
            lock (objLock)
            {
                var defaultRequestHeaders = HttpClient.DefaultRequestHeaders;
                // clearing headers
                defaultRequestHeaders.Clear();
                if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                }
                if (!isGraph)
                {
                    HttpClient.DefaultRequestHeaders.Add("Accept-Encoding", new List<string> { "gzip", "deflate", "br" });
                }
            }
        }

        private void setToken(string token)
        {
            lock (objLock)
            {
                var defaultRequestHeaders = HttpClient.DefaultRequestHeaders;
                defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private void setEtag(string eTag)
        {
            var defaultRequestHeaders = HttpClient.DefaultRequestHeaders;
            defaultRequestHeaders.Remove(HeaderNames.IfMatch);
            defaultRequestHeaders.Add(HeaderNames.IfMatch, eTag);
        }

        /// <summary>
        /// Get call the protected web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        public async Task<HttpResponseMessage> CallWebApiAndProcessResultAsync(string webApiUrl, string? accessToken)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                setToken(accessToken);
            }
            HttpResponseMessage response = await HttpClient.GetAsync(webApiUrl);
            return response;
        }

        /// <summary>
        /// Delete call the protected web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        /// <param name="eTag">For OData validation</param>
        public async Task<HttpResponseMessage> CallWebApiAndDeleteProcessResultAsync(string webApiUrl, string accessToken, string eTag)
        {
            setToken(accessToken);
            setEtag(eTag);
            return await HttpClient.DeleteAsync(webApiUrl);
        }

        /// <summary>
        /// Post call the protected web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        /// <param name="data">JSON data</param>
        /// <param name="eTag">Odata validation</param>
        public async Task<HttpResponseMessage> CallWebApiPostAndProcessResultAsync(string webApiUrl, string accessToken, string data, string? eTag = null)
        {
            setToken(accessToken);
            if (!string.IsNullOrEmpty(eTag))
            {
                setEtag(eTag);
            }
            var httpContent = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
            return await HttpClient.PostAsync(webApiUrl, httpContent);
        }

        /// <summary>
        /// Patch call the protected web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        /// <param name="eTag">Odata validation</param>
        /// <param name="data">JSON data</param>
        public async Task<HttpResponseMessage> CallWebApiPatchAndProcessResultAsync(string webApiUrl, string accessToken, string eTag, string data)
        {
            setToken(accessToken);
            if (!string.IsNullOrEmpty(eTag))
            {
                setEtag(eTag);
            }
            var httpContent = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
            return await HttpClient.PatchAsync(webApiUrl, httpContent);
        }

        /// <summary>
        /// Download stream call
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        /// <returns></returns>
        public async Task<Stream> CallWebApiProcessSteamAsync(string webApiUrl, string accessToken)
        {
            setHeader(false);
            setToken(accessToken);
            return await HttpClient.GetStreamAsync(webApiUrl);
        }
    }
}
