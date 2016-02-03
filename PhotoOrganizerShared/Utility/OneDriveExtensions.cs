using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;

namespace PhotoOrganizerShared.Utility
{
    public static class OneDriveExtensions
    {

        public static async Task<T> SendRequestAsync<T>(this IOneDriveClient client, string httpMethod, string requestUrl, object requestBody = null)
        {
            Uri requestUri = new Uri(new Uri(client.BaseUrl), requestUrl);
            BaseRequest request = new BaseRequest(requestUri.AbsoluteUri, client);
            request.Method = httpMethod;
            if (requestBody != null)
                request.ContentType = "application/json";

            if (requestBody != null)
            {
                return await request.SendAsync<T>(requestBody);
            }
            else
            {
                await request.SendAsync(null);
                return default(T);
            }
        }

    }
}
