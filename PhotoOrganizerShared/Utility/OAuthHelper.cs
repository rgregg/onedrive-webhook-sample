using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace PhotoOrganizerShared.Utility
{
    public class OAuthHelper
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public Uri TokenService { get; set; }

        public OAuthHelper(string tokenService, string clientId, string clientSecret = null, string redirectUri = null)
        {
            this.TokenService = new Uri(tokenService);
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.RedirectUri = redirectUri;
        }

        public async Task<OAuthToken> RedeemRefreshTokenAsync(string refreshToken)
        {
            var queryBuilder = new QueryStringBuilder { StartCharacter = null };
            
            queryBuilder.Add("grant_type", "refresh_token");
            queryBuilder.Add("refresh_token", refreshToken);
            queryBuilder.Add("client_id", this.ClientId);
            
            if (!string.IsNullOrEmpty(this.RedirectUri))
            {
                queryBuilder.Add("redirect_uri", this.RedirectUri);
            }
            if (!string.IsNullOrEmpty(this.ClientSecret))
            {
                queryBuilder.Add("client_secret", this.ClientSecret);
            }            

            return await PostToTokenEndPoint(queryBuilder);
        }

        public async Task<OAuthToken> RedeemAuthorizationCodeAsync(string authCode)
        {
            var queryBuilder = new QueryStringBuilder { StartCharacter = null };

            queryBuilder.Add("grant_type", "authorization_code");
            queryBuilder.Add("code", authCode);
            queryBuilder.Add("client_id", this.ClientId);
            
            if (!string.IsNullOrEmpty(this.RedirectUri))
            {
                queryBuilder.Add("redirect_uri", this.RedirectUri);
            }
            if (!string.IsNullOrEmpty(this.ClientSecret))
            {
                queryBuilder.Add("client_secret", this.ClientSecret);
            }

            return await PostToTokenEndPoint(queryBuilder);
        }

        private async Task<OAuthToken> PostToTokenEndPoint(QueryStringBuilder queryBuilder)
        {
            HttpWebRequest request = WebRequest.CreateHttp(this.TokenService);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            using (StreamWriter requestWriter = new StreamWriter(await request.GetRequestStreamAsync()))
            {
                await requestWriter.WriteAsync(queryBuilder.ToString());
                await requestWriter.FlushAsync();
            }

            HttpWebResponse httpResponse;
            try
            {
                var response = await request.GetResponseAsync();
                httpResponse = response as HttpWebResponse;
            }
            catch (WebException webex)
            {
                httpResponse = webex.Response as HttpWebResponse;
            }
            catch (Exception)
            {
                return null;
            }

            if (httpResponse == null)
            {
                return null;
            }

            try
            {
                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                using (var responseBodyStreamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseBody = await responseBodyStreamReader.ReadToEndAsync();
                    var tokenResult = Newtonsoft.Json.JsonConvert.DeserializeObject<OAuthToken>(responseBody);

                    httpResponse.Dispose();
                    return tokenResult;
                }
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                httpResponse.Dispose();
            }
        }
    }

    public class OAuthToken
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int AccessTokenExpirationDuration { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("scope")]
        public string Scopes { get; set; }

        [JsonProperty("authentication_token")]
        public string AuthenticationToken { get; set; }
    }
}