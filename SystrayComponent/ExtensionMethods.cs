using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace SystrayComponent
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// get process command line arguments c# -> https://stackoverflow.com/questions/2633628/can-i-get-command-line-arguments-of-other-processes-from-net-c
        /// and managementobjectsearcher get property c# -> https://stackoverflow.com/questions/3523844/how-to-read-managementobject-collection-in-wmi-using-c-sharp
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static string GetCommandLine(this Process process)
        {
            var result = string.Empty;
            try
            {
                //var mos = new ManagementObjectSearcher("root\\cimv2", "select * from win32_process where ProcessId = " + process.Id);
                var mos = new ManagementObjectSearcher("root\\cimv2", "select CommandLine from win32_process where ProcessId = " + process.Id);
                foreach (var mo in mos.Get()) { result = mo["CommandLine"].ToString(); /* break; */ }
                //foreach (var mo in mos.Get()) { foreach (var pd in mo.Properties) { result = pd.Value.ToString(); /* break; */ } /* break; */  }
            }
            catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
            {
                // Intentionally empty - no security access to the process.
            }
            catch (InvalidOperationException)
            {
                // Intentionally empty - the process exited before getting details.
            }

            return result;
        }

        public static bool IsUwpApp(this Process process)
        {
            var pcl = process.GetCommandLine();
// listc -> review known to be uwp app process entries, e.g. Edge, Calendar, Mail, OneNote, etc.
// Command Line: "<path to uwp app>.exe" -ServerName:Windows.Internal.WebRuntime.ContentProcessServer
// Command Line: "<path to uwp app>.exe" -ServerName:App.App<uwp app specific hash ???>.mca
            const string UwpAppProcessCommandLinePattern = ".exe\" -ServerName:";
            if (pcl.Contains(UwpAppProcessCommandLinePattern)) return true;
            else return false;
        }
    }
}
