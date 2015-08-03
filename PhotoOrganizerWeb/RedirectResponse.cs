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

namespace PhotoOrganizerWeb
{
    internal class RedirectResponse : IHttpActionResult
    {
        private readonly string _location;
        private readonly CookieHeaderValue _cookie;

        public static IHttpActionResult Create(string location, CookieHeaderValue cookie = null)
        {
            return new RedirectResponse(location, cookie);
        }

        public RedirectResponse(string location, CookieHeaderValue cookie = null)
        {
            _location = location;
            _cookie = cookie;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Add("Location", _location);

            if (null != _cookie)
            {
                response.Headers.AddCookies(new CookieHeaderValue[] { _cookie });
            }

            return Task.FromResult(response);
        }
    }
}