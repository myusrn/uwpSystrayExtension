using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SystrayComponent
{
    public enum Modifiers
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 16384
    }

    class RegisterHotKeyWindow : NativeWindow
    {
        #region Constant, Structure and Delegate Definitions
        const int WM_HOTKEY = 0x0312;
        const int WM_DESTROY = 0x0002;

        public delegate void HotkeyDelegate(int ID);
        #endregion

        #region Instance Variables
        List<Int32> IDs = new List<int>();
        #endregion

        #region Events
        public event HotkeyDelegate HotkeyPressed;
        #endregion

        #region Constructors and Destructors
        /// <summary>
        /// Creates a headless Window to register for and handle WM_HOTKEY.
        /// </summary>
        public RegisterHotKeyWindow()
        {
            this.CreateHandle(new CreateParams());
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
        }
        #endregion

        #region Public Methods
        public void RegisterCombo(Int32 ID, Modifiers fsModifiers, Keys vlc)
        {
            if (RegisterHotKey(this.Handle, ID, (int)fsModifiers, (int)vlc))
            {
                IDs.Add(ID);
            }
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            this.DestroyHandle();
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_HOTKEY: //raise the HotkeyPressed event
                    HotkeyPressed?.Invoke(m.WParam.ToInt32());
                    break;

                case WM_DESTROY: //unregister all hot keys
                    foreach (int ID in IDs)
                    {
                        UnregisterHotKey(this.Handle, ID);
                    }
                    break;
            }
            base.WndProc(ref m);
        }
        #endregion

        #region Dll imports
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
    }
}
