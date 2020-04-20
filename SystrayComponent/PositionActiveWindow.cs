using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.UI.ViewManagement;

namespace SystrayComponent
{
    /// <summary>
    /// The System.Windows.Forms implementation of ArrangeDirection assigns same flag value to Left/Right and Down/Up as shown in following excerpt
    /// [Flags]public enum ArrangeDirection { Left = 0, Right = 0, Down = 4, Up = 4 }
    /// which doesn't work for this use case so creating local implementation that provides only options currently supported with differentiation
    /// </summary>
    public enum ArrangeDirection
    {
        Left,
        Right /* ,
        Down,
        Up */
    }

    public enum ScreenPositions
    {
        LeftCenterRight,
        OneThirdAndTwoThirds
    }

    enum MoveToPosition
    {
        Left,
        LeftTwoThirds,
        Center,
        RightTwoThirds,
        Right,
        Undecided
    }

    public class PositionActiveWindow
    {
        #region Constant, Structure and Delegate Definitions
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        const Int32 MONITOR_DEFAULTTOPRIMERTY = 0x00000001;
        const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct NativeRectangle
        {
            public Int32 Left;
            public Int32 Top;
            public Int32 Right;
            public Int32 Bottom;


            public NativeRectangle(Int32 left, Int32 top, Int32 right, Int32 bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class NativeMonitorInfo
        {
            public Int32 Size = Marshal.SizeOf(typeof(NativeMonitorInfo));
            public NativeRectangle Monitor;
            public NativeRectangle Work;
            public Int32 Flags;
        }

        const short SWP_NOMOVE = 0X2;
        const short SWP_NOSIZE = 1;
        const short SWP_NOZORDER = 0X4;
        const int SWP_SHOWWINDOW = 0x0040;

        const short SW_HIDE = 0;
        const short SW_MAXIMIZE = 3;
        const short SW_MINIMIZE = 6;
        const short SW_SHOW = 5;
        const short SW_RESTORE = 9;

        // https://docs.microsoft.com/en-us/windows/desktop/menurc/wm-syscommand
        const int WM_SYSCOMMAND = 0x0112;
        //const ulong SC_MOVE = 0xF010;
        readonly UIntPtr SC_MOVE = new UIntPtr(0xF010);
        readonly UIntPtr SC_MAXIMIZE = new UIntPtr(0xF030);
        #endregion

        #region Public Methods
        public IntPtr GetActiveWindowHandle()
        {
            //return GetActiveWindow(); // == 0x0 why ???
            return GetForegroundWindow();
        }

        public string GetActiveWindowTitle()
        {
            var awh = GetActiveWindowHandle();

            const int nChars = 256;
            var Buff = new StringBuilder(nChars);
            if (GetWindowText(awh, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        public Rect GetActiveWindowRectangle()
        {
            //Process[] processes = Process.GetProcessesByName("notepad");
            //Process lol = processes[0];
            //IntPtr awh = lol.MainWindowHandle;
            var awh = GetActiveWindowHandle();
            Rect activeWindowRectangle = new Rect();
            GetWindowRect(awh, ref activeWindowRectangle);
            return activeWindowRectangle;
        }

        public Rect GetScreenRectangle()
        {
            //var winformControl = Control.FromHandle(GetActiveWindowHandle());
            //var wfcr = Screen.FromControl(winformControl).Bounds;
            //return new Rect() { Left = wfcr.Left, Top = wfcr.Top, Right = (wfcr.Right - wfcr.Left), Bottom = (wfcr.Bottom - wfcr.Top)};

            //return new Rect() { Left = 0, Top = 0, Right = SystemParameters.PrimaryScreenWidth, Bottom = SystemParameters.PrimaryScreenHeight };

            var awh = GetActiveWindowHandle();
            var monitor = MonitorFromWindow(awh, MONITOR_DEFAULTTONEAREST);
            var screenRectangle = new Rect();
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new NativeMonitorInfo();
                GetMonitorInfo(monitor, monitorInfo);

                screenRectangle.Left = monitorInfo.Monitor.Left;
                screenRectangle.Top = monitorInfo.Monitor.Top;
                screenRectangle.Right = (monitorInfo.Monitor.Right - monitorInfo.Monitor.Left);
                screenRectangle.Bottom = (monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top);
            }
            return screenRectangle;
        }

        public bool IsWindowInMaximizeState(IntPtr windowHandle)
        {
            var swState = SW_HIDE;
            if (!IsWindowVisible(windowHandle)) swState = SW_HIDE;
            else if (IsIconic(windowHandle)) swState = SW_MINIMIZE;
            else if (IsZoomed(windowHandle)) swState = SW_MAXIMIZE;
            else swState = SW_SHOW; // not hidden, minimized or maximized so a normal visible window that could be SW_SHOW, _SHOWNA, _RESTORE, etc

            if (swState != SW_MAXIMIZE) return false;
            else return true;
        }

        public bool IsWindowInNormalState(IntPtr windowHandle)
        {
            var swState = SW_HIDE;
            if (!IsWindowVisible(windowHandle)) swState = SW_HIDE;
            else if (IsIconic(windowHandle)) swState = SW_MINIMIZE;
            else if (IsZoomed(windowHandle)) swState = SW_MAXIMIZE;
            else swState = SW_SHOW; // not hidden, minimized or maximized so a normal visible window that could be SW_SHOW, _SHOWNA, _RESTORE, etc

            if (swState != SW_SHOW) return false;
            else return true;
        }

        public void PutWindowIntoNormalState(IntPtr windowHandle)
        {
            ShowWindow(windowHandle, SW_RESTORE);
        }

        public void PutActiveWindowsIntoMoveMode()
        {   
            var awh = GetActiveWindowHandle();

            // change window to normal state if currently hidden, minimized or maximized state where enabling move operation makes no sense 
            //if (!IsWindowInNormalState(awh)) PutWindowIntoNormalState(awh);

            if (awh != IntPtr.Zero && IsWindowInNormalState(awh))  // only act on active window that is currently in normal state where enabling move operation makes sense
            {
                // https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-sendmessage, https://www.bing.com/search?q=c%23+wparam+lparam ->
                // https://stackoverflow.com/questions/2515261/what-are-the-definitions-for-lparam-and-wparam and c# types reference summary ->
                // short [ / Int16 ] -> ushort [ / UInt16 ]-> int [ / Int32 ] -> uint [ / UInt32 ] -> long -> [ / Int64 ] -> ulong [ / UInt64 ] and float -> double -> decimal
                //SendMessage(awh, WM_SYSCOMMAND, SC_MOVE, IntPtr.Zero); // if using signature (IntPtr hWnd, int msg, ulong wParam, long lParam); generates PInvokeStackImbalance upon return
                SendMessage(awh, WM_SYSCOMMAND, SC_MOVE, IntPtr.Zero); // if using signature (IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);
            }
        }

        public void ToggleActiveWindowsBetweenMaximizeNormalState()
        {   
            var awh = GetActiveWindowHandle();

            if (awh != IntPtr.Zero && !IsWindowInMaximizeState(awh))  // only act on active window that is not currently in maximize state where maximizing it makes sense
            {
                //SendMessage(awh, WM_SYSCOMMAND, SC_MAXIMIZE, IntPtr.Zero); // if using signature (IntPtr hWnd, int msg, ulong wParam, long lParam); generates PInvokeStackImbalance upon return
                //SendMessage(awh, WM_SYSCOMMAND, SC_MAXIMIZE, IntPtr.Zero); // if using signature (IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);
                ShowWindow(awh, SW_MAXIMIZE);
            }
            else if (awh != IntPtr.Zero && !IsWindowInNormalState(awh))  // only act on active window that is not currently in normal state where normal/restore it makes sense
            {
                //SendMessage(awh, WM_SYSCOMMAND, SW_RESTORE, IntPtr.Zero); // if using signature (IntPtr hWnd, int msg, ulong wParam, long lParam); generates PInvokeStackImbalance upon return
                //SendMessage(awh, WM_SYSCOMMAND, SW_RESTORE, IntPtr.Zero); // if using signature (IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);
                ShowWindow(awh, SW_RESTORE);
            }
        }

        public void SetActiveWindowPosition(int left, int top, int right, int bottom)
        {
            //IntPtr awh = process.MainWindowHandle;
            var awh = GetActiveWindowHandle();
            if (awh != IntPtr.Zero)
            {
                SetWindowPos(awh, 0, left, top, right, bottom, SWP_NOZORDER /* | SWP_NOSIZE */ | SWP_SHOWWINDOW);
            }
        }

        /// <summary>
        /// Take current active window and place it in center percentage of screen.
        /// </summary>
        /// <param name="percentageOfTotalWidth">Percentage of total width to use when centering window, default is 60.</param>
        /// <param name="topBottomBorder">Number of pixels to use as border across top and bottom, default is 0.</param>
        public void CenterActiveWindowPosition(int percentageOfTotalWidth = 60, int topBottomBorder = 0, bool minimizeAllOtherWindows = false)
        {
// if you resize active window that is currently in SW_MAXIMIZE state it ends up not resizing it at all, e.g. in case of chrome, or
// resizing it but with an appx 7px space across top and bottom of window and if you minimize it and then restore it comes back as
// SW_MAXIMIZE state not expected SW_SHOW[NORMAL] state. so we check for SW_MAXIMIZE state and change to SW_SHOW[NORMAL] before
// resizing

            var awh = GetActiveWindowHandle();

            if (minimizeAllOtherWindows)
            {
// minimimize all other windows, relevant if you want to eliminate distractions for creating a focused reading window experience, before ensuring active window is in restored state
// the do that first and then restore active window before sizing it 
                //DesktopHideAndRestore(hide: true); // works but introduces a jarring all windows minimize, 1sec wait, then restore and resize active window
                //MinimizeAllWindowsExceptActiveOne(awh); // works only if you stop on breakpoint just before MinimizeAllWindowsExceptActiveOne SendKeyDown/Press/Up is issued
            }

            var swState = SW_HIDE;
            if (!IsWindowVisible(awh)) swState = SW_HIDE;
            else if (IsIconic(awh)) swState = SW_MINIMIZE;
            else if (IsZoomed(awh)) swState = SW_MAXIMIZE;
            else swState = SW_SHOW; // not hidden, minimized or maximized so a normal visible window that could be SW_SHOW, _SHOWNA, _RESTORE, etc
            if (swState != SW_SHOW) ShowWindow(awh, SW_RESTORE);

            var primaryScreen = Screen.PrimaryScreen;
//#if DEBUG
//            var allScreens = Screen.AllScreens; var heightOfTaskbar = primaryScreen.Bounds.Bottom - primaryScreen.WorkingArea.Bottom;
//#endif
            //var sr = GetScreenRectangle(); // doesn't account for taskbar which you'd have to hardcode depending on display | scale and layout | size of . . .  + resolution settings
            var sr = primaryScreen.WorkingArea; // accounts for taskbar

            var leftRightBorder = Convert.ToDecimal(100 - percentageOfTotalWidth) / 2 / 100;
            
            var position = new Rect() { Left = Convert.ToInt16(sr.Right * leftRightBorder), Top = sr.Top + topBottomBorder /*, 
                Right = Convert.ToInt16(sr.Right * (1 - leftRightBorder)) - Left, Bottom = sr.Bottom - topBottomBorder - Top */
            };
            position.Right = Convert.ToInt16(sr.Right * (1 - leftRightBorder)) - position.Left; // change in x not absolute x position
            position.Bottom = sr.Bottom - topBottomBorder - position.Top; // change in y not absolute y position

            // here is a a couple ways to add/subtract from top/bottom position settings in special app cases or uwp vs desktop app cases
            //var awt = GetActiveWindowTitle();
            //if (awt.EndsWith("- Special App Case 1") || awt.EndsWith("- Special App Case 2")) position.Bottom += 7;  
            //var awh = GetActiveWindowHandle(); // this approach throws exceptions in IsUwpApp cases not seen in unit test runs that needs to be debugged
            //foreach (var process in Process.GetProcesses()) { if (process.MainWindowHandle == awh && process.IsUwpApp()) { position.Bottom += 7; break; } }

            SetActiveWindowPosition(position.Left, position.Top, position.Right, position.Bottom);
        }

        /// <summary>
        /// Take current active window and place it in center using specified percentage of total screen height and aspect ratio to determine width.
        /// </summary>
        /// <param name="percentageOfTotalHeight">Percentage of total screen height to use when determining when centering window, default is 80.</param>
        /// <param name="aspectRatio">Aspect ratio to determine width of centered window after height has been calculated, default is current generation mobile device 9x19.</param>
        public void CenterActiveWindowPositionHeightAndAspectRatio(int percentageOfTotalHeight = 80, decimal aspectRatio = (decimal)9/19)
        {
// if you resize active window that is currently in SW_MAXIMIZE state it ends up not resizing it at all, e.g. in case of chrome, or
// resizing it but with an appx 7px space across top and bottom of window and if you minimize it and then restore it comes back as
// SW_MAXIMIZE state not expected SW_SHOW[NORMAL] state. so we check for SW_MAXIMIZE state and change to SW_SHOW[NORMAL] before
// resizing
            var awh = GetActiveWindowHandle(); var swState = SW_HIDE;
            if (!IsWindowVisible(awh)) swState = SW_HIDE;
            else if (IsIconic(awh)) swState = SW_MINIMIZE;
            else if (IsZoomed(awh)) swState = SW_MAXIMIZE;
            else swState = SW_SHOW; // not hidden, minimized or maximized so a normal visible window that could be SW_SHOW, _SHOWNA, _RESTORE, etc
            if (swState != SW_SHOW) ShowWindow(awh, SW_RESTORE);

            var primaryScreen = Screen.PrimaryScreen;
//#if DEBUG
//            var allScreens = Screen.AllScreens; var heightOfTaskbar = primaryScreen.Bounds.Bottom - primaryScreen.WorkingArea.Bottom;
//#endif
            //var sr = GetScreenRectangle(); // doesn't account for taskbar which you'd have to hardcode depending on display | scale and layout | size of . . .  + resolution settings
            var sr = primaryScreen.WorkingArea; // accounts for taskbar

            var topBottomBorderPercent = Convert.ToDecimal(100 - percentageOfTotalHeight) / 2 / 100;
            var leftRightBorder = Convert.ToInt16((sr.Right - sr.Left - ((sr.Bottom - sr.Top) * (Convert.ToDecimal(percentageOfTotalHeight) / 100) * aspectRatio)) / 2);

            var position = new Rect() { Left = sr.Left + leftRightBorder, Top = Convert.ToInt16(sr.Bottom * topBottomBorderPercent) /*, 
                Right = sr.Right - leftRightBorder - Left, Bottom = Convert.ToInt16(sr.Top * (1 - topBottomBorder)) - Top */
            };
            position.Right = sr.Right - leftRightBorder - position.Left; // change in x not absolute x position            
            position.Bottom = Convert.ToInt16(sr.Bottom * (1 - topBottomBorderPercent)) - position.Top; // change in y not absolute y position

            // here is a a couple ways to add/subtract from top/bottom position settings in special app cases or uwp vs desktop app cases
            //var awt = GetActiveWindowTitle();
            //if (awt.EndsWith("- Special App Case 1") || awt.EndsWith("- Special App Case 2")) position.Bottom += 7;  
            //var awh = GetActiveWindowHandle(); // this approach throws exceptions in IsUwpApp cases not seen in unit test runs that needs to be debugged
            //foreach (var process in Process.GetProcesses()) { if (process.MainWindowHandle == awh && process.IsUwpApp()) { position.Bottom += 7; break; } }

            SetActiveWindowPosition(position.Left, position.Top, position.Right, position.Bottom);
        }

        /// <summary>
        /// Execute winkey+d[esktop all windows hide and restore] keyboard shortcut not the winkey+m[inimize desktop] keyboard shortcut
        /// </summary>
        /// <param name="display"></param>
        public void DesktopHideAndRestore(bool hide = true)
        {
// c# Shell.IShellDispatch -> https://mike-ward.net/2008/09/02/a-lean-method-for-invoking-com-in-c/
// IShellDispatch header -> https://docs.microsoft.com/en-us/windows/win32/shell/ishelldispatch
            var shell = new Shell32();
            var shellDispatch = (IShellDispatch)shell;
            shellDispatch.MinimizeAll();
            Thread.Sleep(1000); // introduce a 1000ms sleep to allow minimize desktop processing to complete before any subsequent window restore and sizing commands are issued

// win32 api for minimizing all windows -> https://stackoverflow.com/questions/13942765/minimize-all-open-windows
// WPARAM MIN_ALL -> https://stackoverflow.com/questions/785054/minimizing-all-open-windows-in-c-sharp
            //var stwh = FindWindow("Shell_TrayWnd", null);
            //if (hide) /* var res = */ SendMessage(stwh, WM_COMMAND, MIN_ALL, IntPtr.Zero);
            //else /* var res = */ SendMessage(stwh, WM_COMMAND, MIN_ALL_UNDO, IntPtr.Zero);
            //Thread.Sleep(1000); // introduce a 1000ms sleep to allow minimize desktop processing to complete before any subsequent window restore and sizing commands are issued
        }

        /// <summary>
        /// Enumerate and minimize all windows except for currently active one, i.e. active window title bar mouse click + shake/wiggle or winkey+home keyboard shortcut
        /// </summary>
        public void MinimizeAllWindowsExceptActiveOne(IntPtr awh)
        {
// minimize all windows except the active one -> http://www.zeigen.com/shortcuts/2015/07/16/min-all-except-active/ and
// https://www.labnol.org/software/minimize-open-windows-quickly/9985/

// win32 api sendinput c# -> https://stackoverflow.com/questions/12761169/send-keys-through-sendinput-in-user32-dll
// win32 api virtual key codes -> https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes 
            //SetActiveWindow(awh); // doesn't appear to make a difference
            SendKeyDown(KeyCode.LWIN);
            SendKeyPress(KeyCode.HOME);
            SendKeyUp(KeyCode.LWIN);

// c# sendkeys windows key combination -> c# sendkeys Keys.LWin combination -> 
// https://stackoverflow.com/questions/10366152/sending-windows-key-using-sendkeys
// https://stackoverflow.com/questions/12877547/vbscript-sendkeys-ctrllwintab
// https://stackoverflow.com/questions/48277076/how-to-send-keyboard-combination-shiftwinleft-using-sendkeys-in-c
// ** https://stackoverflow.com/questions/32077050/sending-a-keypress-to-the-active-window-that-doesnt-handle-windows-message-in-c
// ** https://stackoverflow.com/questions/15536980/send-keystroke-to-application-in-c-sharp-sendkeys-postmessage-sendmessage-all
// your post for help https://stackoverflow.com/questions/59278021/using-c-sharp-sendkeys-to-send-winkeyhome-for-minimize-all-windows-except-activ
// for ctrl use ^ | for winkey use ctrl+esc ^{Esc} | for alt use % | for shift use + 
            //SendKeys.SendWait("{" + Keys.LWin + "}{" + Keys.Home + "}"); // System.ArgumentException { "Keyword \"LWin\" is not valid." }
            //SendKeys.SendWait("{" + Keys.RWin + "}{" + Keys.Home + "}"); // System.ArgumentException { "Keyword \"RWin\" is not valid." }
            //SendKeys.SendWait("{LWin}{Home}"); // System.ArgumentException { "Keyword \"LWin\" is not valid." }
            //SendKeys.SendWait("^{Esc}{Home}"); // launches start menu as if just winkey was pressed
            //SendKeys.SendWait("(^{Esc}{Home})"); // launches start menu as if just winkey was pressed and then dismisses it
            //SendKeys.SendWait("^({Esc}{Home})"); // launches start menu as if just winkey was pressed and then dismisses it
            //SendKeys.SendWait("(^{Esc}){Home}"); // launches start menu as if just winkey was pressed and then dismisses it
            //SendKeys.SendWait("((^{Esc}){Home})"); // launches start menu as if just winkey was pressed and then dismisses it
            //SendKeys.SendWait("(^({Esc}{Home}))"); // launches start menu as if just winkey was pressed and then dismisses it
        }

        /// <summary>
        /// Take current active window and place it in left or right 3rd of screen area.
        /// </summary>
        /// <param name="centerPercentageOfTotalWidth">Percentage of total width to use when moving window, default is 40, with suggested alternatives being 36-38-40.</param>
        /// <param name="topBottomBorder">Number of pixels to use as border across top and bottom, default is 0.</param>
        /// <param name="screenPostions">defines type of screen positions scenario to base action on, default is LeftCenterRight.</param>
        public void PlaceActiveWindowPosition(ArrangeDirection arrangeDirection, int centerPercentageOfTotalWidth = 40, int topBottomBorder = 0, ScreenPositions screenPositions = ScreenPositions.LeftCenterRight)
        {
            //if (arrangeDirection == ArrangeDirection.Up || arrangeDirection == ArrangeDirection.Down)
            //{
            //        throw new ApplicationException("unsupported arrange direction specified");
            //}

            var awh = GetActiveWindowHandle();

// if you resize active window that is currently in SW_MAXIMIZE state it ends up not resizing it at all, e.g. in case of chrome, or
// resizing it but with an appx 7px space across top and bottom of window and if you minimize it and then restore it comes back as
// SW_MAXIMIZE state not expected SW_SHOW[NORMAL] state. so we check for SW_MAXIMIZE state and change to SW_SHOW[NORMAL] before
// resizing
            /* if (!IsWindowInNormalState(awh)) */ PutWindowIntoNormalState(awh);

            var primaryScreen = Screen.PrimaryScreen;
//#if DEBUG
//            var allScreens = Screen.AllScreens; var heightOfTaskbar = primaryScreen.Bounds.Bottom - primaryScreen.WorkingArea.Bottom;
//#endif
            //var sr = GetScreenRectangle(); // doesn't account for taskbar which you'd have to hardcode depending on display | scale and layout | size of . . .  + resolution settings
            var sr = primaryScreen.WorkingArea; // accounts for taskbar

            // splitting screen into 3rds is always going to require center 3rd to be rounded up to even number to end up with integer/whole numbers for left and right 3rds
            var centerWindowSize = (sr.Right - sr.Left) * centerPercentageOfTotalWidth / 100;
            var leftRightPercentageOfTotalWidth = (100 - centerPercentageOfTotalWidth) / 2; 
            var leftRightWindowSize = (sr.Right - sr.Left) * leftRightPercentageOfTotalWidth / 100;
            //var centerWindowSize = (sr.Right - sr.Left) - (leftRightWindowSize * 2);

            var position = new Rect() { Top = sr.Top + topBottomBorder };
            position.Bottom = sr.Bottom - topBottomBorder - position.Top; // change in y not absolution y position 

            MoveToPosition mtp = GetMoveToPosition(arrangeDirection, sr, centerWindowSize, leftRightWindowSize, screenPositions);
            if (mtp == MoveToPosition.Left)
            {
                position.Left = 0;
                position.Right = leftRightWindowSize; // change in x not absolution x position
            }
            else if (mtp == MoveToPosition.LeftTwoThirds)
            {
                position.Left = 0;
                position.Right = leftRightWindowSize + centerWindowSize; // change in x not absolution x position
            }
            else if (mtp == MoveToPosition.Center)
            {
                position.Left = sr.Left + leftRightWindowSize;
                //position.Left = sr.Right - leftRightWindowSize - centerWindowSize; // alternative calculation
                position.Right = centerWindowSize; // change in x not absolution x position
            }
            else if (mtp == MoveToPosition.RightTwoThirds)
            {
                position.Left = sr.Right - leftRightWindowSize - centerWindowSize;
                //position.Left = sr.Left + leftRightWindowSize; // alternative calculation
                position.Right = centerWindowSize + leftRightWindowSize; // change in x not absolution x position
            }
            else if (mtp == MoveToPosition.Right)
            {
                position.Left = sr.Right - leftRightWindowSize;
                //position.Left = sr.Left + leftRightWindowSize + centerWindowSize; // alternative calculation
                position.Right = leftRightWindowSize; // change in x not absolution x position
            }

            // here is a a couple ways to add/subtract from top/bottom position settings in special app cases or uwp vs desktop app cases
            //var awt = GetActiveWindowTitle();
            //if (awt.EndsWith("- Special App Case 1") || awt.EndsWith("- Special App Case 2")) position.Bottom += 7;  
            //var awh = GetActiveWindowHandle(); // this approach throws exceptions in IsUwpApp cases not seen in unit test runs that needs to be debugged
            //foreach (var process in Process.GetProcesses()) { if (process.MainWindowHandle == awh && process.IsUwpApp()) { position.Bottom += 7; break; } }

            SetActiveWindowPosition(position.Left, position.Top, position.Right, position.Bottom);
        }

        MoveToPosition GetMoveToPosition(ArrangeDirection arrangeDirection, System.Drawing.Rectangle screenRectangle, int centerWindowSize, int leftRightWindowSize, ScreenPositions screenPostions)
        {
            MoveToPosition mtp = MoveToPosition.Undecided;
            var awr = GetActiveWindowRectangle();
// needed 1px on 34" uwqhd 21:9 3440x1440 @ 100%, 2px on 25" wqhd 16:9 2560x1440 @ 125% [ , 3px on 13" wqhd @ 175% scale ] and ease of access [ win+u ] | display | make text bigger = 100% (default)
// using 3px appeared to break behavior back on 25" display where win+leftarrow state ctrl+leftarrow cycled to right 3rd instead of left 2/3rds and visa versa for starting with win+rightarrow state
            const int pixelError = 2; 
            if (arrangeDirection == ArrangeDirection.Left)
            {
                if (screenPostions == ScreenPositions.LeftCenterRight)
                { 
                    if (awr.Left - pixelError > screenRectangle.Left + leftRightWindowSize + centerWindowSize) mtp = MoveToPosition.Right;
                    else if (awr.Left > screenRectangle.Left + leftRightWindowSize) mtp = MoveToPosition.Center;
                    else if (awr.Left == screenRectangle.Left + leftRightWindowSize && Math.Abs(awr.Right - (screenRectangle.Left + leftRightWindowSize + centerWindowSize)) > pixelError) mtp = MoveToPosition.Center;
                    else if (awr.Left == screenRectangle.Left + leftRightWindowSize && awr.Right <= screenRectangle.Left + leftRightWindowSize + centerWindowSize) mtp = MoveToPosition.Left;
                    else if (awr.Left > screenRectangle.Left) mtp = MoveToPosition.Left;
                    else if (awr.Left <= screenRectangle.Left && awr.Right > screenRectangle.Left + leftRightWindowSize) mtp = MoveToPosition.Left; // store apps repro the <=
                    else /* if (awr.Left <= screeRectangle.Left && awr.Right <= screenRectangle.Left + leftRightWindowSize) */ mtp = MoveToPosition.Right; // cycle around to other side
                }
                else /* (screenPostions == ScreenPositions.OneThirdAndTwoThirds) */
                {
                    if (awr.Left - pixelError > screenRectangle.Left + leftRightWindowSize + centerWindowSize) mtp = MoveToPosition.Right;
                    else if (Math.Abs(awr.Left - (screenRectangle.Left + leftRightWindowSize + centerWindowSize)) < pixelError && awr.Right <= screenRectangle.Right) mtp = MoveToPosition.RightTwoThirds;
                    else if (awr.Left > screenRectangle.Left + leftRightWindowSize) mtp = MoveToPosition.LeftTwoThirds;
                    else if (awr.Left > screenRectangle.Left && awr.Right >= screenRectangle.Left + leftRightWindowSize + centerWindowSize) mtp = MoveToPosition.LeftTwoThirds;
                    else if (awr.Left >= screenRectangle.Left && awr.Right <= screenRectangle.Left + leftRightWindowSize + centerWindowSize &&
                        Math.Abs(awr.Right - (screenRectangle.Left + leftRightWindowSize)) > pixelError) mtp = MoveToPosition.Left;
                    else if (awr.Left > screenRectangle.Left) mtp = MoveToPosition.Left;
                    else if (awr.Left <= screenRectangle.Left && awr.Right > screenRectangle.Left + leftRightWindowSize) mtp = MoveToPosition.Left; // store apps repro the <=
                    else /* if (awr.Left <= screeRectangle.Left && awr.Right <= screenRectangle.Left + leftRightWindowSize) */ mtp = MoveToPosition.Right; // cycle around to other side
                }
            }
            else /* if (arrangeDirection == ArrangeDirection.Right) */
            {
                if (screenPostions == ScreenPositions.LeftCenterRight)
                {
                    if (awr.Right + pixelError < screenRectangle.Right - leftRightWindowSize - centerWindowSize) mtp = MoveToPosition.Left;
                    else if (awr.Right < screenRectangle.Right - centerWindowSize) mtp = MoveToPosition.Center;
                    else if (awr.Right == screenRectangle.Right - leftRightWindowSize && Math.Abs(awr.Left - (screenRectangle.Left + leftRightWindowSize)) > pixelError) mtp = MoveToPosition.Center;
                    else if (awr.Right == screenRectangle.Right - leftRightWindowSize && awr.Left >= screenRectangle.Left + leftRightWindowSize) mtp = MoveToPosition.Right;
                    else if (awr.Right < screenRectangle.Right) mtp = MoveToPosition.Right;
                    else if (awr.Right >= screenRectangle.Right && awr.Left < screenRectangle.Right - leftRightWindowSize) mtp = MoveToPosition.Right; // store apps repro the >=
                    else /* if (awr.Right >= screeRectangle.Right && awr.Left >= screenRectangle.Right - leftRightWindowSize) */ mtp = MoveToPosition.Left; // cycle around to other side
                }
                else /* (screenPostions == ScreenPositions.OneThirdAndTwoThirds) */
                {
                    if (awr.Right + pixelError < screenRectangle.Right - centerWindowSize - leftRightWindowSize) mtp = MoveToPosition.Left;
                    else if (Math.Abs(awr.Right - (screenRectangle.Right - leftRightWindowSize - centerWindowSize)) < pixelError && awr.Left >= screenRectangle.Left) mtp = MoveToPosition.LeftTwoThirds;
                    else if (awr.Right < screenRectangle.Right - centerWindowSize) mtp = MoveToPosition.RightTwoThirds;
                    else if (awr.Right < screenRectangle.Right && awr.Left <= screenRectangle.Left + leftRightWindowSize) mtp = MoveToPosition.RightTwoThirds;
                    else if (awr.Right <= screenRectangle.Right && awr.Left >= screenRectangle.Left + leftRightWindowSize + centerWindowSize && 
                        Math.Abs(awr.Left - (screenRectangle.Left + leftRightWindowSize + centerWindowSize)) > pixelError) mtp = MoveToPosition.Right;
                    else if (awr.Right < screenRectangle.Right) mtp = MoveToPosition.Right;
                    else if (awr.Right >= screenRectangle.Right && awr.Left < screenRectangle.Right - leftRightWindowSize) mtp = MoveToPosition.Right; // store apps repro the >=
                    else /* if (awr.Right >= screeRectangle.Right && awr.Left >= screenRectangle.Right - leftRightWindowSize) */ mtp = MoveToPosition.Left; // cycle around to other side
                }
            }
            return mtp;
        }
        #endregion

        #region PInvoke dll imports, structs, enums and const
        /// <summary>
        /// Retrieves the window handle to the active window attached to the calling thread's message queue.
        /// </summary>
        /// <returns>Handle to the active window attached to the calling thread's message queue. Otherwise, the return value is NULL.</returns>
        //[DllImport("user32.dll")]
        //public static extern IntPtr GetActiveWindow();

        /// <summary>
        /// Activates a window. The window must be attached to the calling thread's message queue.
        /// </summary>
        /// <returns>Handle to the active window attached to the calling thread's message queue. Otherwise, the return value is NULL.</returns>
        [DllImport("user32.dll", SetLastError=true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working). 
        /// </summary>
        /// <returns>Handle to the foreground window. The foreground window can be NULL in certain circumstances, such as when a window is losing activation.</returns>
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Copies the text of the specified window's title bar (if it has one) into a buffer. 
        /// </summary>
        /// <param name="hWnd">A handle to the window or control containing the text.</param>
        /// <param name="text">The buffer that will receive the text. If the string is as long or longer than the buffer, the string is truncated and terminated with a null character.</param>
        /// <param name="count">The maximum number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated.</param>
        /// <returns>If the function succeeds, the return value is the length, in characters, of the copied string, not including the terminating null character. If the window has no title bar or text, if the title bar is empty, or if the window or control handle is invalid, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder stringBuffer, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hWnd, Int32 flags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, NativeMonitorInfo lpmi);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        [DllImport("user32.dll")]
        //public static extern IntPtr SendMessage(IntPtr hWnd, int msg, ulong wParam, long lParam);  
// 'PInvokeStackImbalance' : 'A call to PInvoke function 'SystrayComponent!SystrayComponent.PositionActiveWindow::SendMessage' has unbalanced the stack. 
// This is likely because the managed PInvoke signature does not match the unmanaged target signature. 
// Check that the calling convention and parameters of the PInvoke signature match the target unmanaged signature.
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);

        [ComImport, Guid("13709620-C279-11CE-A49E-444553540000")]
        class Shell32
        {
        }

        [ComImport, Guid("D8F015C0-C278-11CE-A49E-444553540000")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IShellDispatch
        {
            [DispId(0x60020007)]
            void MinimizeAll();
        }

        const int WM_COMMAND = 0x0111;
        UIntPtr MIN_ALL = new UIntPtr(0x01A3); // == 419 decimal
        UIntPtr MIN_ALL_UNDO = new UIntPtr(0x01A0); // == 416 decimal

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646270(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/Forums/en/csharplanguage/thread/f0e82d6e-4999-4d22-b3d3-32b25f61fb2a
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646310(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646310(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/forums/en-US/netfxbcl/thread/2abc6be8-c593-4686-93d2-89785232dacd
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        public enum KeyCode : ushort
        {
            #region Media

            /// <summary>
            /// Next track if a song is playing
            /// </summary>
            MEDIA_NEXT_TRACK = 0xb0,

            /// <summary>
            /// Play pause
            /// </summary>
            MEDIA_PLAY_PAUSE = 0xb3,

            /// <summary>
            /// Previous track
            /// </summary>
            MEDIA_PREV_TRACK = 0xb1,

            /// <summary>
            /// Stop
            /// </summary>
            MEDIA_STOP = 0xb2,

            #endregion

            #region math

            /// <summary>Key "+"</summary>
            ADD = 0x6b,
            /// <summary>
            /// "*" key
            /// </summary>
            MULTIPLY = 0x6a,

            /// <summary>
            /// "/" key
            /// </summary>
            DIVIDE = 0x6f,

            /// <summary>
            /// Subtract key "-"
            /// </summary>
            SUBTRACT = 0x6d,

            #endregion

            #region Browser
            /// <summary>
            /// Go Back
            /// </summary>
            BROWSER_BACK = 0xa6,
            /// <summary>
            /// Favorites
            /// </summary>
            BROWSER_FAVORITES = 0xab,
            /// <summary>
            /// Forward
            /// </summary>
            BROWSER_FORWARD = 0xa7,
            /// <summary>
            /// Home
            /// </summary>
            BROWSER_HOME = 0xac,
            /// <summary>
            /// Refresh
            /// </summary>
            BROWSER_REFRESH = 0xa8,
            /// <summary>
            /// browser search
            /// </summary>
            BROWSER_SEARCH = 170,
            /// <summary>
            /// Stop
            /// </summary>
            BROWSER_STOP = 0xa9,
            #endregion

            #region Numpad numbers
            /// <summary>
            /// 
            /// </summary>
            NUMPAD0 = 0x60,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD1 = 0x61,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD2 = 0x62,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD3 = 0x63,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD4 = 100,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD5 = 0x65,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD6 = 0x66,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD7 = 0x67,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD8 = 0x68,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD9 = 0x69,

            #endregion

            #region Fkeys
            /// <summary>
            /// F1
            /// </summary>
            F1 = 0x70,
            /// <summary>
            /// F10
            /// </summary>
            F10 = 0x79,
            /// <summary>
            /// 
            /// </summary>
            F11 = 0x7a,
            /// <summary>
            /// 
            /// </summary>
            F12 = 0x7b,
            /// <summary>
            /// 
            /// </summary>
            F13 = 0x7c,
            /// <summary>
            /// 
            /// </summary>
            F14 = 0x7d,
            /// <summary>
            /// 
            /// </summary>
            F15 = 0x7e,
            /// <summary>
            /// 
            /// </summary>
            F16 = 0x7f,
            /// <summary>
            /// 
            /// </summary>
            F17 = 0x80,
            /// <summary>
            /// 
            /// </summary>
            F18 = 0x81,
            /// <summary>
            /// 
            /// </summary>
            F19 = 130,
            /// <summary>
            /// 
            /// </summary>
            F2 = 0x71,
            /// <summary>
            /// 
            /// </summary>
            F20 = 0x83,
            /// <summary>
            /// 
            /// </summary>
            F21 = 0x84,
            /// <summary>
            /// 
            /// </summary>
            F22 = 0x85,
            /// <summary>
            /// 
            /// </summary>
            F23 = 0x86,
            /// <summary>
            /// 
            /// </summary>
            F24 = 0x87,
            /// <summary>
            /// 
            /// </summary>
            F3 = 0x72,
            /// <summary>
            /// 
            /// </summary>
            F4 = 0x73,
            /// <summary>
            /// 
            /// </summary>
            F5 = 0x74,
            /// <summary>
            /// 
            /// </summary>
            F6 = 0x75,
            /// <summary>
            /// 
            /// </summary>
            F7 = 0x76,
            /// <summary>
            /// 
            /// </summary>
            F8 = 0x77,
            /// <summary>
            /// 
            /// </summary>
            F9 = 120,

            #endregion

            #region Other
            /// <summary>
            /// 
            /// </summary>
            OEM_1 = 0xba,
            /// <summary>
            /// 
            /// </summary>
            OEM_102 = 0xe2,
            /// <summary>
            /// 
            /// </summary>
            OEM_2 = 0xbf,
            /// <summary>
            /// 
            /// </summary>
            OEM_3 = 0xc0,
            /// <summary>
            /// 
            /// </summary>
            OEM_4 = 0xdb,
            /// <summary>
            /// 
            /// </summary>
            OEM_5 = 220,
            /// <summary>
            /// 
            /// </summary>
            OEM_6 = 0xdd,
            /// <summary>
            /// 
            /// </summary>
            OEM_7 = 0xde,
            /// <summary>
            /// 
            /// </summary>
            OEM_8 = 0xdf,
            /// <summary>
            /// 
            /// </summary>
            OEM_CLEAR = 0xfe,
            /// <summary>
            /// 
            /// </summary>
            OEM_COMMA = 0xbc,
            /// <summary>
            /// 
            /// </summary>
            OEM_MINUS = 0xbd,
            /// <summary>
            /// 
            /// </summary>
            OEM_PERIOD = 190,
            /// <summary>
            /// 
            /// </summary>
            OEM_PLUS = 0xbb,

            #endregion

            #region KEYS

            /// <summary>
            /// 
            /// </summary>
            KEY_0 = 0x30,
            /// <summary>
            /// 
            /// </summary>
            KEY_1 = 0x31,
            /// <summary>
            /// 
            /// </summary>
            KEY_2 = 50,
            /// <summary>
            /// 
            /// </summary>
            KEY_3 = 0x33,
            /// <summary>
            /// 
            /// </summary>
            KEY_4 = 0x34,
            /// <summary>
            /// 
            /// </summary>
            KEY_5 = 0x35,
            /// <summary>
            /// 
            /// </summary>
            KEY_6 = 0x36,
            /// <summary>
            /// 
            /// </summary>
            KEY_7 = 0x37,
            /// <summary>
            /// 
            /// </summary>
            KEY_8 = 0x38,
            /// <summary>
            /// 
            /// </summary>
            KEY_9 = 0x39,
            /// <summary>
            /// 
            /// </summary>
            KEY_A = 0x41,
            /// <summary>
            /// 
            /// </summary>
            KEY_B = 0x42,
            /// <summary>
            /// 
            /// </summary>
            KEY_C = 0x43,
            /// <summary>
            /// 
            /// </summary>
            KEY_D = 0x44,
            /// <summary>
            /// 
            /// </summary>
            KEY_E = 0x45,
            /// <summary>
            /// 
            /// </summary>
            KEY_F = 70,
            /// <summary>
            /// 
            /// </summary>
            KEY_G = 0x47,
            /// <summary>
            /// 
            /// </summary>
            KEY_H = 0x48,
            /// <summary>
            /// 
            /// </summary>
            KEY_I = 0x49,
            /// <summary>
            /// 
            /// </summary>
            KEY_J = 0x4a,
            /// <summary>
            /// 
            /// </summary>
            KEY_K = 0x4b,
            /// <summary>
            /// 
            /// </summary>
            KEY_L = 0x4c,
            /// <summary>
            /// 
            /// </summary>
            KEY_M = 0x4d,
            /// <summary>
            /// 
            /// </summary>
            KEY_N = 0x4e,
            /// <summary>
            /// 
            /// </summary>
            KEY_O = 0x4f,
            /// <summary>
            /// 
            /// </summary>
            KEY_P = 80,
            /// <summary>
            /// 
            /// </summary>
            KEY_Q = 0x51,
            /// <summary>
            /// 
            /// </summary>
            KEY_R = 0x52,
            /// <summary>
            /// 
            /// </summary>
            KEY_S = 0x53,
            /// <summary>
            /// 
            /// </summary>
            KEY_T = 0x54,
            /// <summary>
            /// 
            /// </summary>
            KEY_U = 0x55,
            /// <summary>
            /// 
            /// </summary>
            KEY_V = 0x56,
            /// <summary>
            /// 
            /// </summary>
            KEY_W = 0x57,
            /// <summary>
            /// 
            /// </summary>
            KEY_X = 0x58,
            /// <summary>
            /// 
            /// </summary>
            KEY_Y = 0x59,
            /// <summary>
            /// 
            /// </summary>
            KEY_Z = 90,

            #endregion

            #region volume
            /// <summary>
            /// Decrese volume
            /// </summary>
            VOLUME_DOWN = 0xae,

            /// <summary>
            /// Mute volume
            /// </summary>
            VOLUME_MUTE = 0xad,

            /// <summary>
            /// Increase volue
            /// </summary>
            VOLUME_UP = 0xaf,

            #endregion


            /// <summary>
            /// Take snapshot of the screen and place it on the clipboard
            /// </summary>
            SNAPSHOT = 0x2c,

            /// <summary>Send right click from keyboard "key that is 2 keys to the right of space bar"</summary>
            RightClick = 0x5d,

            /// <summary>
            /// Go Back or delete
            /// </summary>
            BACKSPACE = 8,

            /// <summary>
            /// Control + Break "When debuging if you step into an infinite loop this will stop debug"
            /// </summary>
            CANCEL = 3,
            /// <summary>
            /// Caps lock key to send cappital letters
            /// </summary>
            CAPS_LOCK = 20,
            /// <summary>
            /// Ctlr key
            /// </summary>
            CONTROL = 0x11,

            /// <summary>
            /// Alt key
            /// </summary>
            ALT = 18,

            /// <summary>
            /// "." key
            /// </summary>
            DECIMAL = 110,

            /// <summary>
            /// Delete Key
            /// </summary>
            DELETE = 0x2e,


            /// <summary>
            /// Arrow down key
            /// </summary>
            DOWN = 40,

            /// <summary>
            /// End key
            /// </summary>
            END = 0x23,

            /// <summary>
            /// Escape key
            /// </summary>
            ESC = 0x1b,

            /// <summary>
            /// Home key
            /// </summary>
            HOME = 0x24,

            /// <summary>
            /// Insert key
            /// </summary>
            INSERT = 0x2d,

            /// <summary>
            /// Open my computer
            /// </summary>
            LAUNCH_APP1 = 0xb6,
            /// <summary>
            /// Open calculator
            /// </summary>
            LAUNCH_APP2 = 0xb7,

            /// <summary>
            /// Open default email in my case outlook
            /// </summary>
            LAUNCH_MAIL = 180,

            /// <summary>
            /// Opend default media player (itunes, winmediaplayer, etc)
            /// </summary>
            LAUNCH_MEDIA_SELECT = 0xb5,

            /// <summary>
            /// Left control
            /// </summary>
            LCONTROL = 0xa2,

            /// <summary>
            /// Left arrow
            /// </summary>
            LEFT = 0x25,

            /// <summary>
            /// Left shift
            /// </summary>
            LSHIFT = 160,

            /// <summary>
            /// left windows key
            /// </summary>
            LWIN = 0x5b,


            /// <summary>
            /// Next "page down"
            /// </summary>
            PAGEDOWN = 0x22,

            /// <summary>
            /// Num lock to enable typing numbers
            /// </summary>
            NUMLOCK = 0x90,

            /// <summary>
            /// Page up key
            /// </summary>
            PAGE_UP = 0x21,

            /// <summary>
            /// Right control
            /// </summary>
            RCONTROL = 0xa3,

            /// <summary>
            /// Return key
            /// </summary>
            ENTER = 13,

            /// <summary>
            /// Right arrow key
            /// </summary>
            RIGHT = 0x27,

            /// <summary>
            /// Right shift
            /// </summary>
            RSHIFT = 0xa1,

            /// <summary>
            /// Right windows key
            /// </summary>
            RWIN = 0x5c,

            /// <summary>
            /// Shift key
            /// </summary>
            SHIFT = 0x10,

            /// <summary>
            /// Space back key
            /// </summary>
            SPACE_BAR = 0x20,

            /// <summary>
            /// Tab key
            /// </summary>
            TAB = 9,

            /// <summary>
            /// Up arrow key
            /// </summary>
            UP = 0x26,

        }

        /// <summary>
        /// simulate key press
        /// </summary>
        /// <param name="keyCode"></param>
        public static void SendKeyPress(KeyCode keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT()
            {
                Vk = (ushort)keyCode,
                Scan = 0,
                Flags = 0,
                Time = 0,
                ExtraInfo = IntPtr.Zero,
            };

            INPUT input2 = new INPUT
            {
                Type = 1
            };
            input2.Data.Keyboard = new KEYBDINPUT()
            {
                Vk = (ushort)keyCode,
                Scan = 0,
                Flags = 2,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };
            INPUT[] inputs = new INPUT[] { input, input2 };
            if (SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new Exception();
        }

        /// <summary>
        /// Send a key down and hold it down until sendkeyup method is called
        /// </summary>
        /// <param name="keyCode"></param>
        public static void SendKeyDown(KeyCode keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT();
            input.Data.Keyboard.Vk = (ushort)keyCode;
            input.Data.Keyboard.Scan = 0;
            input.Data.Keyboard.Flags = 0;
            input.Data.Keyboard.Time = 0;
            input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            INPUT[] inputs = new INPUT[] { input };
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Release a key that is being hold down
        /// </summary>
        /// <param name="keyCode"></param>
        public static void SendKeyUp(KeyCode keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT();
            input.Data.Keyboard.Vk = (ushort)keyCode;
            input.Data.Keyboard.Scan = 0;
            input.Data.Keyboard.Flags = 2;
            input.Data.Keyboard.Time = 0;
            input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            INPUT[] inputs = new INPUT[] { input };
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new Exception();

        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);
        #endregion
    }
}
