using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace PhotoOrganizerWeb
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            PhotoOrganizerShared.AzureStorage.InitializeConnections(WebAppConfig.Default);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
