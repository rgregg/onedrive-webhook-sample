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