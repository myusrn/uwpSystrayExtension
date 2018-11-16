using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SystrayComponent;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ProcessIsUwpAppTrue()
        {
            //var awh = paw.GetActiveWindowHandle();
            bool processIsUwpApp = false;
            foreach (var process in Process.GetProcesses())
            {
                //if (process.MainWindowHandle == awh)
                if (process.MainWindowTitle.EndsWith("Edge") || process.MainWindowTitle.EndsWith("Calendar"))
                {
                    processIsUwpApp = process.IsUwpApp();
                    break;
                }
            }
            Assert.IsTrue(processIsUwpApp);
        }

        [TestMethod]
        public void CenterActiveWindowPositionNoException()
        {
            var paw = new PositionActiveWindow();
            paw.CenterActiveWindowPosition(); // using default parameter settings when none provided
            Assert.IsTrue(true);
        }
    }
}
