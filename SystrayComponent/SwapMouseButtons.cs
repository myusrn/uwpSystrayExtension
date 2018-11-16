using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SystrayComponent
{
    #region Constant, Structure and Delegate Definitions
    internal enum MouseButtonsSetting
    {
        RightHanded = 0,
        LeftHanded = 1
    }
    #endregion

    class SwapMouseButtons
    {
        #region Public Methods
        /// <summary>
        /// gets the mouse buttons setting currently in effect
        /// </summary>
        /// <returns>value indicating whether right or left handed setting is in place</returns>
        internal MouseButtonsSetting GetMouseButtonsSetting()
        {
            var mouseButtonsSetting = MouseButtonsSetting.RightHanded;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Mouse"))
            {
                if (key is null) throw new ApplicationException("unable to open mouse settings registry key");
                var kv = key.GetValue("SwapMouseButtons");
                if (kv is null) throw new ApplicationException("unable to open mouse settings registry key value");
                if (Convert.ToInt16(kv) == 1) mouseButtonsSetting = MouseButtonsSetting.LeftHanded;
            }

            return mouseButtonsSetting;
        }

        /// <summary>
        /// sets the mouse buttons setting currently in effect
        /// </summary>
        /// <returns>value indicating whether right or left handed setting is in place</returns>
        /// <remarks>TODO: see .net framework app write to registry hits for ideas as to why writing to registry is not working here in non-elevated process like it does
        /// in console app case</remarks>
        internal void SetMouseButtonsSetting(MouseButtonsSetting mouseButtonsSetting)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Mouse", true)) // open writable
            {
                if (key is null) throw new ApplicationException("unable to open mouse settings registry key");
                if (mouseButtonsSetting == MouseButtonsSetting.LeftHanded)
                {
                    Debug.WriteLine("swapping mouse buttons settings to left handed");
                    SwapMouseButton(true); // change runtime setting
                    try { key.SetValue("SwapMouseButtons", "1", RegistryValueKind.String); } // change persisted setting
                    catch (UnauthorizedAccessException) { Debug.WriteLine("unable to persist change execute from \"run as administrator\" environment"); }
                }
                else /* (mouseButtonsSetting == MouseButtonSettings.RightHanded) */
                {
                    Debug.WriteLine("swapping mouse buttons settings to right handed");
                    SwapMouseButton(false); // change runtime setting
                    try { key.SetValue("SwapMouseButtons", "0", RegistryValueKind.String); } // change persisted setting
                    catch (UnauthorizedAccessException) { Debug.WriteLine("unable to persist change execute from \"run as administrator\" environment"); }
                }
            }
        }
        #endregion

        #region Dll Imports
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SwapMouseButton([param: MarshalAs(UnmanagedType.Bool)] bool fSwap);
        #endregion
    }
}
