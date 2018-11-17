//*********************************************************  
//  
// Copyright (c) Microsoft. All rights reserved.  
// This code is licensed under the MIT License (MIT).  
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY  
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR  
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.  
//  
//********************************************************* 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using System.Diagnostics;

namespace SystrayComponent
{
    public class SystrayApplicationContext : ApplicationContext
    {
        private bool win32appMode = false;
        private AppServiceConnection connection = null;
        private NotifyIcon notifyIcon = null;
        //private Form1 configWindow = new Form1(); // unused aspect of sample

        // option 1 - low level global keyboard hook, which allows hooking win+<A-Z> keyboard shortcuts but has the 
        // potential to destabilize the users system
        //GlobalKeyboardHook gkh = null; 

        // option 2 - recommended RegistryHotkey api, which does not allow hooking win+<A-Z> keyboard shortcuts but is
        // supported mechanism not expected to destabalize the users system
        RegisterHotKeyWindow rhw = null; Process process = null; bool hotkeyInProgress = false;

        PositionActiveWindow paw = null; SwapMouseButtons smb = null;

        public SystrayApplicationContext()
        {
            try
            {
                var pkg = Package.Current; /* var pkgId = Package.Current.Id; var pkgIdFn = Package.Current.Id.FamilyName; */
            }
            catch (InvalidOperationException) // 'The process has no package identity. (Exception from HRESULT: 0x80073D54)'
            {
                win32appMode = true;  // global to determine if we are running in win32app or storeapp [ uwp + desktopbridge launch of winform ] mode
            }

            // if running as uwp package with desktopbridge invoked win32 systray/notifcation area process display uwp and if not display winform menu options

            MenuItem openMenuItem = new MenuItem("Open Usage Info", new EventHandler(OpenApp)); openMenuItem.DefaultItem = true;
            MenuItem openMenuItemWin32appMode = new MenuItem("Open Usage Info", new EventHandler(OpenLegacy)); openMenuItemWin32appMode.DefaultItem = true;
            MenuItem sendMenuItem = new MenuItem("Send message to UWP app", new EventHandler(SendToUWP));
            MenuItem legacyMenuItem = new MenuItem("Open legacy Usage Info", new EventHandler(OpenLegacy));
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));

            notifyIcon = new NotifyIcon();
            if (!win32appMode) notifyIcon.DoubleClick += new EventHandler(OpenApp);
            else /* if (win32appMode) */ notifyIcon.DoubleClick += new EventHandler(OpenLegacy);
            notifyIcon.Icon = SystrayComponent.Properties.Resources.Icon2;
#if DEBUG
            if (!win32appMode) notifyIcon.ContextMenu = new ContextMenu(new MenuItem[]{ openMenuItem, sendMenuItem, legacyMenuItem, exitMenuItem });
#else
            if (!win32appMode) notifyIcon.ContextMenu = new ContextMenu(new MenuItem[]{ openMenuItem, exitMenuItem });
#endif
            else notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { openMenuItemWin32appMode, exitMenuItem });
            notifyIcon.Visible = true;

            if (!win32appMode)
            {
                int processId = (int)ApplicationData.Current.LocalSettings.Values["processId"];
                process = Process.GetProcessById(processId); process.EnableRaisingEvents = true;
                //process.Exited += UwpFullTrustProcessLauncher_Exited;  // this was only necessary in newer GlobalHotkey desktop extensions sample where this process had no UI
            }

            // option 1 - low level global keyboard hook, which allows hooking win+<A-Z> keyboard shortcuts but has the 
            // potential to destabilize the users system
            //gkh = new GlobalKeyboardHook();
            //gkh.KeyDown += new SystrayComponent.KeyEventHandler(Gkh_KeyDown);
            //gkh.KeyUp += new SystrayComponent.KeyEventHandler(Gkh_KeyUp);

            // option 2 - recommended RegistryHotkey api, which does not allow hooking alt/ctrl[+shift]+<A-Z> keyboard 
            // shortcuts and is supported mechanism not expected to destabalize the users system
            rhw = new RegisterHotKeyWindow(); // use Modifiers.Alt/.Control | Modifiers.Shift for registering hotkey with multiple modifier prefixes
            rhw.HotkeyPressed += new RegisterHotKeyWindow.HotkeyDelegate(Rhw_HotkeyPressed);
            rhw.RegisterCombo(1001, Modifiers.Alt, Keys.C); // Alt+C = center active window default/60%
            rhw.RegisterCombo(1002, Modifiers.Alt | Modifiers.Shift, Keys.C); // alt+c+shift = center active window 40%
            rhw.RegisterCombo(1003, Modifiers.Alt, Keys.Left); // Alt+L[eft Arrow] = place active window to left 3rd which stomps on existing browser previous page behavior
            rhw.RegisterCombo(1004, Modifiers.Alt, Keys.Right); // Alt+R[ight Arrow] = place active window to right 3rd which stomps on existing browser previous page behavior
            //rhw.RegisterCombo(1005, Modifiers.Alt | Modifiers.Shift, Keys.Left); // Alt+Shift+L[eft Arrow] = place active window to left 2/3rd or 3rd
            //rhw.RegisterCombo(1006, Modifiers.Alt | Modifiers.Shift, Keys.Right); // Alt+Shift+R[ight Arrow] = place active window to right 2/3rd or 3rd
            rhw.RegisterCombo(1005, Modifiers.Control, Keys.Left); // Ctrl+L[eft Arrow] = place active window to left 2/3rd or 3rd which stomps on existing ide move left shortcut
            rhw.RegisterCombo(1006, Modifiers.Control, Keys.Right); // Ctrl+R[ight Arrow] = place active window to right 2/3rd or 3rd which stomps on existing ide move left shortcut
            rhw.RegisterCombo(1007, Modifiers.Alt, Keys.A); // Alt+A = show a few windows niceties usage info [ and settings customization ] window
            rhw.RegisterCombo(1008, Modifiers.Alt, Keys.B); // Alt+B = swap mouse buttons
            rhw.RegisterCombo(1009, Modifiers.Alt, Keys.M); // Alt+M = put active window into move mode vs oob Alt+Space+M sequence
            rhw.RegisterCombo(1010, Modifiers.Alt, Keys.X); // Alt+X = exit this application including closing systray/notification area process 
            rhw.RegisterCombo(1011, Modifiers.Alt, Keys.Z); // Alt+Z = hibernate/sleep computer, given vs17 Alt+H[elp] and Alt+[Te]S[t] existing assignments, vs oob Win+X,U,S sequence

            paw = new PositionActiveWindow(); smb = new SwapMouseButtons();
        } 

        private void UwpFullTrustProcessLauncher_Exited(object sender, EventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values.Remove("systrayComponentRunning");
            Application.Exit();
        }

        private void Gkh_KeyDown(object sender, KeyEventArgsEx e)
        {
//#if DEBUG
//            var awt = paw.GetActiveWindowTitle();
//            MessageBox.Show("Active Window Title is " + awt);
//#endif

            if (e.KeyCode == Keys.C && (e.Modifiers == Keys.LWin || e.Modifiers == Keys.RWin)) // center active window position
            {
                paw.CenterActiveWindowPosition(); // using default parameter settings when none provided
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.B && (e.Modifiers == Keys.LWin || e.Modifiers == Keys.RWin)) // swap mouse buttons
            {
                var currentSetting = smb.GetMouseButtonsSetting();
                if (currentSetting == MouseButtonsSetting.RightHanded) smb.SetMouseButtonsSetting(MouseButtonsSetting.LeftHanded);
                else /* (currentSetting == MouseButtonSettings.LeftHanded) */ smb.SetMouseButtonsSetting(MouseButtonsSetting.RightHanded);
                e.Handled = true; // this stomps on default that sets mouse/keyboard input focus on "Show hidden icons" up arrow
            }
            //else if (e.KeyCode == Keys.L && ((e.Modifiers & Keys.LWin) == Keys.LWin || (e.Modifiers & Keys.RWin) == Keys.RWin) && 
            //    ((e.Modifiers & Keys.LShiftKey) == Keys.LShiftKey || (e.Modifiers & Keys.RShiftKey) == Keys.RShiftKey))  // lock and engage modern standby vs legacy [s3] suspend
            //{
            //    // TODO: implement lock and engage modern standby vs legacy [s3] suspend
            //      e.Handled = true;
            //}
        }

        private void Gkh_KeyUp(object sender, KeyEventArgsEx e)
        {
            e.Handled = true;
        }

        private /* async */ void Rhw_HotkeyPressed(int id)
        {
            if (hotkeyInProgress) return; hotkeyInProgress = true; // prevent reentrancy            

            // option 1 - send the hotkey message id to the uwp app for processing
            //IEnumerable<AppListEntry> appListEntries = await Package.Current.GetAppListEntriesAsync();
            //await appListEntries.First().LaunchAsync(); // optionally bring the UWP to the foreground
            //ValueSet hotkeyPressed = new ValueSet(); hotkeyPressed.Add("id", id);
            //connection = new AppServiceConnection();
            //connection.PackageFamilyName = Package.Current.Id.FamilyName;
            //connection.AppServiceName = "SystrayExtensionService";
            //connection.ServiceClosed += Connection_ServiceClosed;
            //AppServiceConnectionStatus connectionStatus = await connection.OpenAsync();
            //if (connectionStatus != AppServiceConnectionStatus.Success)
            //{
            //    //Debug.WriteLine(status); 
            //    //if (!win32AppMode) ApplicationData.Current.LocalSettings.Values.Remove("systrayComponentRunning"); 
            //    //Application.Exit();
            //    MessageBox.Show("Status: " + connectionStatus.ToString()); return;
            //}
            //var response = await connection.SendMessageAsync(hotkeyPressed);

            // option 2 - process the hotkey message id here in systray component process
            //#if DEBUG
            //            var awt = paw.GetActiveWindowTitle();
            //            MessageBox.Show("Active Window Title is " + awt);
            //#endif
            if (id == 1001) // center active window position default/60%
            {
                paw.CenterActiveWindowPosition(); // using default 60% parameter settings when none provided
                hotkeyInProgress = false;
            }
            else if (id == 1002) // center active window position 40%
            {
                paw.CenterActiveWindowPosition(40); // using 40% vs default parameter settings
                hotkeyInProgress = false;
            }
            else if (id == 1003) // place active window position to left using 34-36-38-40% for center 3rd
            {
                paw.PlaceActiveWindowPosition(ArrangeDirection.Left, 40); // uses default parameter settings when none provided
                hotkeyInProgress = false;
            }
            else if (id == 1004) // place active window position to right using 34-36-38-40% for center 3rd
            {
                paw.PlaceActiveWindowPosition(ArrangeDirection.Right, 40); // uses default parameter settings when none provided
                hotkeyInProgress = false;
            }
            else if (id == 1005) // place active window position to left using 67-68-69-70% for 2/3rds calculation
            {
                paw.PlaceActiveWindowPosition(ArrangeDirection.Left, 40, 0, ScreenPositions.OneThirdAndTwoThirds);
                hotkeyInProgress = false;
            }
            else if (id == 1006) // place active window position to right using 67-68-69-70% for 2/3rds calculation
            {
                paw.PlaceActiveWindowPosition(ArrangeDirection.Right, 40, 0, ScreenPositions.OneThirdAndTwoThirds);
                hotkeyInProgress = false;
            }
            else if (id == 1007) // launch show usage information [ and customizable settings ] window
            {
                if (!win32appMode) this.OpenApp(this, null);
                else this.OpenLegacy(this, null);
                hotkeyInProgress = false;
            }
            else if (id == 1008) // swap mouse buttons
            {
                var currentSetting = smb.GetMouseButtonsSetting();
                if (currentSetting == MouseButtonsSetting.RightHanded) smb.SetMouseButtonsSetting(MouseButtonsSetting.LeftHanded);
                else smb.SetMouseButtonsSetting(MouseButtonsSetting.RightHanded);
                hotkeyInProgress = false;
            }
            else if (id == 1009)
            {
                paw.PutActiveWindowsIntoMoveMode();
                hotkeyInProgress = false;
            }
            else if (id == 1010) // exit this application including closing systray/notification area process 
            {
                this.Exit(this, null);
                hotkeyInProgress = false;
            }
            else if (id == 1011) // initiate modern standby hibernate/sleep vs legacy [s3] suspend
            {
                var result = Application.SetSuspendState(PowerState.Suspend, true, false);
                hotkeyInProgress = false;
            }
        }

        private async void OpenApp(object sender, EventArgs e)
        {
            IEnumerable<AppListEntry> appListEntries = await Package.Current.GetAppListEntriesAsync();
            await appListEntries.First().LaunchAsync();

            // TODO: switch to protocol [ activation ] launch, e.g. https://docs.microsoft.com/en-us/windows/uwp/xbox-apps/automate-launching-uwp-apps and
            // https://docs.microsoft.com/en-us/previous-versions/windows/apps/hh464906(v=win.10)#_uri_activation___extension_
            // which allows for parameters to tell app that it was launched by desktop extensions sdk sibling win32 systray component            
            // powershell get-appxpackage *systray* -> UWPwithSystrayextension declared as package name in SystrayExtension project manifest ???
            // powershell get-appxpackage *niceties* -> 14136RobertOBrien.AFewWindowsNiceties -> FewWindowsNiceties declared as package name in Package project manifest ???
            // https://docs.microsoft.com/en-us/windows/uwp/xbox-apps/automate-launching-uwp-apps 
            // "%programfiles(x86)%\Windows Kits\10\App Certification Kit\microsoft.windows.softwarelogo.appxlauncher.exe" -> d:\prd\Apf\utils\AppxLauncher.exe
            // appXlauncher.exe AFewWindowsNiceties_8n0z5tqffjzxc!AFewWindowsNiceties
            // appXlauncher.exe AFewWindowsNiceties_1.1.1.0_x86__8n0z5tqffjzxc!AFewWindowsNiceties
            //var appUri = new Uri("afwn://param1");  // Package project manifest | declarations | add | protocol | name = afwn
            //var success = await Windows.System.Launcher.LaunchUriAsync(appUri /* , new ValueSet { ??? } */ );
        }

        private async void SendToUWP(object sender, EventArgs e)
        {
            ValueSet message = new ValueSet();
            message.Add("content", "Message from Systray Extension");
            await SendToUWP(message);
        }

        private void OpenLegacy(object sender, EventArgs e)
        {
            Form1 form = new Form1(this);
            //form.StartPosition = FormStartPosition.CenterScreen; // alternative to Form1.cs [Design] | properties | layout | startpostion = WindowsDefaultLocation -> CenterScreen
            form.Show();
        }

        internal async void Exit(object sender, EventArgs e)
        {
            if (!win32appMode)
            {
                ApplicationData.Current.LocalSettings.Values.Remove("systrayComponentRunning");
                var message = new ValueSet(); message.Add("exit", ""); await SendToUWP(message);
            }
            Application.Exit();
        }

        private async Task SendToUWP(ValueSet message)
        { 
            if (connection == null)
            {
                connection = new AppServiceConnection();
                connection.PackageFamilyName = Package.Current.Id.FamilyName;
                connection.AppServiceName = "SystrayExtensionService";
                connection.ServiceClosed += Connection_ServiceClosed;
                AppServiceConnectionStatus connectionStatus = await connection.OpenAsync();
                if (connectionStatus != AppServiceConnectionStatus.Success)
                {
                    MessageBox.Show("Status: " + connectionStatus.ToString());
                    return;
                }
            }

            await connection.SendMessageAsync(message);
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Debug.WriteLine("Connection_ServiceClosed");
            hotkeyInProgress = false;
            connection.ServiceClosed -= Connection_ServiceClosed;
            connection = null;
        }
    }
}
