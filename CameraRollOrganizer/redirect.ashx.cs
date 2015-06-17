using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CameraRollOrganizer
{
    /// <summary>
    /// Summary description for redirect
    /// </summary>
    public class redirect : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            var authorizationCode = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(authorizationCode))
                

            
            context.Response.ContentType = "text/plain";
            context.Response.Write("Hello World");
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}