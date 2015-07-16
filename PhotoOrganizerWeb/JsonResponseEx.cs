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
    internal class JsonResponseEx : IHttpActionResult
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object _bodyObject;
        private readonly CookieHeaderValue _cookie;

        public static IHttpActionResult Create(HttpStatusCode statusCode, object body, CookieHeaderValue cookie = null)
        {
            return new JsonResponseEx(statusCode, body, cookie);
        }

        public JsonResponseEx(HttpStatusCode statusCode, object body, CookieHeaderValue cookie = null)
        {
            _statusCode = statusCode;
            _bodyObject = body;
            _cookie = cookie;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            response.Content = new StringContent(JsonConvert.SerializeObject(_bodyObject));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            if (null != _cookie)
            {
                response.Headers.AddCookies(new CookieHeaderValue[] { _cookie });
            }

            return Task.FromResult(response);
        }
    }
}