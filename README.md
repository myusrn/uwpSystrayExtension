# A Few Windows Niceties (afwn) 
11/27/2018

Application provides global hotkey, aka keyboard shortcut, behaviors which i found myself wanting ever since moving to wqhd 16:9 aspect ratio displays and even more so with recent use of the new ultrawide 21:9 aspect ratio displays. While windows provides Win+LeftArrow and Win+RightArrow for creating initial 50/50 layout, that can then be dragged to any split you like, I found these didn't address the window positioning scenarios i was commonly wanting quick and easy access to when doing reading and development work on widescreen displays. 

It also includes non-window positioning and sizeing related hotkeys such as on for mouse button swapping which i regularly use to keep from getting stuck in mouse hand specific postures during long work sessions. See the app's system tray, aka notification area, "Open Usage Info" context menu option for details on full list of enabled hotkeys.

The Alt+C[enter] and Alt+Shift+[Center] hotkeys i find address making reading easier.  For some science of what happens when you read, that may explain why widescreen reading is pita, see [Tim Ferriss' tricks for reading two times faster video](https://www.youtube.com/watch?v=CZU6G8EMUE4).  In it he explains how using narrower page widths reduces number of eye refocusing events per line.

This application makes use of uwp app desktop bridge/extensions support, aka centennial program, to initiate a win32 process that runs in the system tray, aka notification area. This is accomplished via FullTrustProcessLauncher app. The communication between launched win32 app and parent uwp app is done with an in-proc AppService.

On my workstation I just launch the SystrayExtension\Win32\AfwnSystrayComponent.exe directly, optionally at signin using hkcu\Software\Microsoft\Windows\CurrentVersion\Run registry entry or "%appdata%\Microsoft\Windows\Start Menu\Programs\Startup" folder shortcut. It has logic to detect if was started by uwp wrapper app or directly and behave accordingly, e.g. launching uwp or winform based show usage information view. I'm awaiting details on how to be enrolled in centennial program so that store app submission can be accepted. The benefit will be ability to make app accessible through store to be installed and run on IT policy controlled machines where things like fixed whitelist based malware detection software might now allow win32 executable to be launched directly versus from within store app sandbox.  
  
# Build/Deploy and Run the sample
 - Visual Studio 2017 and the Windows 10 October/April 2018s Update SDK (version 17134/16299)
 - Select Store app Package project as your starting project
 - Press F5 to run!
  
Samples, and comments q&a, that facilitated this work can be found at: https://stefanwick.com/2017/06/24/uwp-app-with-systray-extension/ and https://stefanwick.com/2018/05/15/global-hotkey-registration-in-uwp/.  
  
Current screenshots with usage information dialog: [./Package/Images/Screenshot Combined.png](./Package/Images/Screenshot%20combined.png)  

[comment]: # (![alt text](./Package/Images/Screenshot%20combined.png "Screenshot Combined Image"\))

# Feature and Fix updates
11/16/18 - made following feature update
1. updated icon used in systray, aka notfication area, and in legacy windows form usage info view making use of "png to icon free" -> https://convertico.com/ service
  
11/16/18 - fixed following issues found to exist with active window positioning keyboard shortcuts
1. when in alt+c[enter] state ctrl+leftarrow correctly moves to left 2/3rds but ctrl+rightarrow moves to right 1/3rd not 2/3rds
2. when in win+leftarrow state ctrl+leftarrow moves to right 2/3rds instead of left 2/3rds and likewise when in win+rightarrow state and enter ctrl+rightarrow
3. when in alt+shift+c state alt+leftarrow has to be hit twice to move to left 3rd but same isn't true for alt+rightarrow which correctly moves to right 1/3rd
4. when in win+leftarrow state alt+leftarrow moves to right 1/3rd instead of left 3rd and likewise when in win+rightarrow state end enter ctrl+rightarrow
5. when in ctrl+leftarrow 1/3rd state ctrl+rightarrow moves to right 2/3rds instead of left 2/3rds and likewise in ctrl+rightarrow 1/3rd state
