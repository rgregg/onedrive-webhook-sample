using Microsoft.OneDrive.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizerWebJob
{
    internal static class OneDriveExtensionMethods
    {
        public static bool IsMatchCode(this OneDriveException ex, OneDriveErrorCode code)
        {
            return ex.IsMatch(code.ToString());
        }
    }


}
