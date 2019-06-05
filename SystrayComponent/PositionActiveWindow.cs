using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
                //SendMessage(awh, WM_SYSCOMMAND, SC_MOVE, 0); // if using signature (IntPtr hWnd, int msg, ulong wParam, long lParam); generates PInvokeStackImbalance upon return
                SendMessage(awh, WM_SYSCOMMAND, SC_MOVE, new IntPtr(0)); // if using signature (IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);
            }
        }

        public void ToggleActiveWindowsBetweenMaximizeNormalState()
        {   
            var awh = GetActiveWindowHandle();

            if (awh != IntPtr.Zero && !IsWindowInMaximizeState(awh))  // only act on active window that is not currently in maximize state where maximizing it makes sense
            {
                //SendMessage(awh, WM_SYSCOMMAND, SC_MAXIMIZE, 0); // if using signature (IntPtr hWnd, int msg, ulong wParam, long lParam); generates PInvokeStackImbalance upon return
                //SendMessage(awh, WM_SYSCOMMAND, SC_MAXIMIZE, new IntPtr(0)); // if using signature (IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);
                ShowWindow(awh, SW_MAXIMIZE);
            }
            else if (awh != IntPtr.Zero && !IsWindowInNormalState(awh))  // only act on active window that is not currently in normal state where normal/restore it makes sense
            {
                //SendMessage(awh, WM_SYSCOMMAND, SW_RESTORE, 0); // if using signature (IntPtr hWnd, int msg, ulong wParam, long lParam); generates PInvokeStackImbalance upon return
                //SendMessage(awh, WM_SYSCOMMAND, SW_RESTORE, new IntPtr(0)); // if using signature (IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);
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
        public void CenterActiveWindowPosition(int percentageOfTotalWidth = 60, int topBottomBorder = 0)
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
        /// Take current active window and place it in center percentage of screen height with phone aspect ratio.
        /// </summary>
        /// <param name="percentageOfTotalHeight">Percentage of total height to use when centering phone aspect ratio sized window, default is 80.</param>
        /// <param name="aspectRatio">Aspect ratio to use controlling how wide window will be, default is current generation mobile device 19x9.</param>
        public void PhoneCenterActiveWindowPosition(int percentageOfTotalHeight = 80, decimal aspectRatio = (decimal)19/9)
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
            var leftRightBorder = Convert.ToInt16((sr.Right - sr.Left - ((sr.Bottom - sr.Top - (2 * topBottomBorderPercent)) / aspectRatio)) / 2);
            
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
        /// Take current active window and place it in left or right 3rd of screen area.
        /// </summary>
        /// <param name="centerPercentageOfTotalWidth">Percentage of total width to use when moving window, default is 34, with suggested alternatives being 36-38-40.</param>
        /// <param name="topBottomBorder">Number of pixels to use as border across top and bottom, default is 0.</param>
        /// <param name="screenPostions">defines type of screen positions scenario to base action on, default is LeftCenterRight.</param>
        public void PlaceActiveWindowPosition(ArrangeDirection arrangeDirection, int centerPercentageOfTotalWidth = 34, int topBottomBorder = 0, ScreenPositions screenPositions = ScreenPositions.LeftCenterRight)
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

        #region Dll Imports
        /// <summary>
        /// Retrieves the window handle to the active window attached to the calling thread's message queue.
        /// </summary>
        /// <returns>Handle to the active window attached to the calling thread's message queue. Otherwise, the return value is NULL.</returns>
        //[DllImport("user32.dll")]
        //static extern IntPtr GetActiveWindow();

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
        static extern int GetWindowText(IntPtr hWnd, StringBuilder stringBuffer, int maxCount);

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

        #endregion
    }
}
