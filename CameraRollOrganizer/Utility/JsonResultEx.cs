using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace CameraRollOrganizer.Utility
{

    internal static class JsonResultExtensionMethods
    {
        public static IHttpActionResult JsonResponse<T>(this ApiController controller, HttpStatusCode statusCode, T body)
        {
            return new JsonResultEx(statusCode, body);
        }
    }

    internal class JsonResultEx : IHttpActionResult
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object _bodyObject;

        public JsonResultEx(HttpStatusCode statusCode, object body)
        {
            _statusCode = statusCode;
            _bodyObject = body;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            response.Content = new StringContent(JsonConvert.SerializeObject(_bodyObject));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
    
        

    }
}