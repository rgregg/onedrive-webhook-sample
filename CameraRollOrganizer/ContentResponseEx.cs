using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
namespace CameraRollOrganizer
{
    public class ContentResponseEx : IHttpActionResult
    {
        private readonly HttpStatusCode _statusCode;
        private readonly Stream _bodyContent;
        private readonly CookieHeaderValue _cookie;
        private readonly string _contentType;

        public static IHttpActionResult Create(HttpStatusCode statusCode, Stream bodyStream, string contentType = "application/octet-stream", CookieHeaderValue cookie = null)
        {
            return new ContentResponseEx(statusCode, bodyStream, contentType, cookie);
        }

        public static IHttpActionResult Create(HttpStatusCode statusCode, string bodyText, string contentType = "application/octet-stream", CookieHeaderValue cookie = null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(bodyText);
            MemoryStream stream = new MemoryStream(bytes);
            return new ContentResponseEx(statusCode, stream, contentType, cookie);
        }

        public ContentResponseEx(HttpStatusCode statusCode, Stream body, string contentType, CookieHeaderValue cookie = null)
        {
            _statusCode = statusCode;
            _bodyContent = body;
            _contentType = contentType;
            _cookie = cookie;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            response.Content = new StreamContent(_bodyContent);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(_contentType);

            if (null != _cookie)
            {
                response.Headers.AddCookies(new CookieHeaderValue[] { _cookie });
            }

            return Task.FromResult(response);
        }
    }
}