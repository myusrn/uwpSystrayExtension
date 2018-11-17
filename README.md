# A Few Windows Niceties (afwn)

Application provides global hotkey, aka keyboard shortcut, behaviors which i found myself constantly wanting ever since moving to wqhd 16:9 aspect ratio displays and more recently the new ultrawide 21:9 aspect ratio displays. While windows provides Win+LeftArrow and Win+RightArrow for 50/50 and customizable two window layouts I found these didn't address the window positioning scenarios i was commonly want quick easy access to when doing reading and development work on widescreen displays. It also includes keyboard shortcut for mouse button swapping which i regularly use to keep from getting stuck in mouse hand specific postures during long work sessions. See the app's system tray, aka notification area, "Open Usage Info" context menu option for enabled hotkey details.

Note: The Alt+C[enter] and Alt+Shift+[Center] hotkeys i find address making reading easier supported by science of what happens when you read covered in Tim Ferriss' trick for reading two times faster video [ https://www.youtube.com/watch?v=CZU6G8EMUE4 ] explaining how using narrower page widths reduces number of eye refocusing events per line.

This application makes use of uwp app desktop extensions support to initiate a win32 process that runs in the system tray, aka notification area. This is accomplished via FullTrustProcessLauncher app. The communication between launched win32 app and parent uwp app is done with an in-proc AppService.

submission options
I use confirmAppClose capability to ensure that system tray, aka notification area, process has been launched that is necessary for functionality that remains in place when uwp app view is no longer directly accessible via task bar icon.  
I use runFullTrust capability as part of new desktop extensions capability to facilitate launching of a win32 process which is required in this case to register for system tray, aka notification area, exposed process.
  
# Build/Deploy and Run the sample
 - Visual Studio 2017 and the Windows 10 October/Aprial 2018s Update SDK (version 17134/16299)
 - Select UWP Package project as your starting project
 - Press F5 to run!
  
Related information: https://github.com/myusrn/uwpSystrayExtension/README.md 

# feature and fix updates
11/16/18 - fixed following issues found to exist with active window positioning keyboard shortcuts
1. when in alt+c[enter] state ctrl+leftarrow correctly moves to left 2/3rds but ctrl+rightarrow moves to right 1/3rd not 2/3rds
2. when in win+leftarrow state ctrl+rightarrow moves to right 2/3rds instead of left 2/3rds and likewise when in win+rightarrow state and enter ctrl+leftarrow
3. when in alt+shift+c state alt+leftarrow has to be hit twice to move to left 3rd but same isn't true for alt+rightarrow which correctly moves to right 1/3rd
4. when in win+leftarrow state alt+leftarrow moves to right 1/3rd instead of left 3rd and likewise when in win+rightarrow state end enter ctrl+rightarrow
5. when in ctrl+leftarrow 1/3rd state ctrl+rightarrow moves to right 2/3rds instead of left 2/3rds and likewise in ctrl+rightarrow 1/3rd state
