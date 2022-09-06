# A Few Windows Niceties (afwn) 
last visited = 09/05/22, status = in-progress

Application provides global hotkey, aka keyboard shortcut, behaviors which i found myself wanting ever since moving to wqhd 16:9 aspect ratio displays and even more so with recent use of the new ultrawide 21:9 aspect ratio displays. While windows provides Win+LeftArrow and Win+RightArrow for creating initial 50/50 layout, that can then be dragged to any split you like, I found these didn't address the window positioning scenarios i was commonly wanting quick and easy access to when doing reading and development work on widescreen displays. 

It also includes non-window positioning and sizeing related hotkeys such as on for mouse button swapping which i regularly use to keep from getting stuck in mouse hand specific postures during long work sessions. See the app's system tray, aka notification area, "Open Usage Info" context menu option for details on full list of enabled hotkeys.

The Alt+C[enter] and Alt+Shift+[Center] hotkeys i find address making reading easier. For some science of what happens when you read, that may explain why widescreen reading is pita, see [Tim Ferriss' tricks for reading two times faster video](https://youtube.com/watch?v=CZU6G8EMUE4). In it he explains how using narrower page widths reduces number of eye refocusing events per line.

This application makes use of uwp app desktop bridge/extensions support, aka centennial program, to initiate a win32 process that runs in the system tray, aka notification area. This is accomplished via FullTrustProcessLauncher app. The communication between launched win32 app and parent uwp app is done with an in-proc AppService.

The current release is available in the microsoft store. The benefit of installing from there is the trusted source and sandbox execution experience.  This allows use on IT policy controlled machines where things like fixed whitelist based malware detection software might block directly executed win32 apps from being run. On my workstation when i want to focus on debugging core behavior i just launch systrayComponent win32 output  directly. It has logic to detect if was started by uwp store app or directly and behave accordingly, e.g. launching uwp or winform based show usage information view.   

# Find, Install and Run from Microsoft Store 
- for Microsoft Store web listing https://microsoft.com/en-us/p/a-few-windows-niceties/9mtw4cj7s276?activetab=pivot%3Aoverviewtab
- for Microsoft Store app listing search on "A Few Windows Niceties"

# Build, Deploy and Run from Sources
- In visual studio installer enable the '.NET desktop development' and 'Universal Windows Platform development' workloads and confirm individual components '.NET' -> '.NET 6.0 Runtime (LTS), 
'.NET Framework 4.8[.1] SDK', '.NET Framework 4.8[.1] targeting pack' and 'SDK, libraries, and frameworks' -> 'Windows 11 SDK (10.0.22621.0)' are included
- In windows enable win+i [ settings ] | privacy & security | for developers | developer mode = off -> on [ which you can turn back off when build, run, debug/test work is done ]
- For Package_TemporaryKey.pfx [ not password protected ], Package_StorePublishing.pfx [ password protected and persisted in user certificates store ] see Packages | Package.appxmanifest | Packaging | Choose Certificate | Create & How to create package signing certificate -&gt; https://docs.microsoft.com/en-us/windows/msix/package/create-certificate-package-signing and 
- For Package_StoreAssociation.xml see Package | Publish | Associate App with the Store | Include apps that already have packages = checked | Refresh | &lt;select existing published app entry&gt; | Asssociate -&gt; Package.StoreAssociation.xml and 'create or get uwp store pfx' -&gt; https://stackoverflow.com/questions/42209953/how-to-create-or-get-a-new-storekey-pfx-for-uwp-application   
- Select store app package project, SystrayExtension (Universal Windows), and choose 'Set as Startup Project'
- Press F5 to Start Debugging or Ctrl+F5 to Start Without Debugging
  
Samples, and comments q&a, that facilitated this work can be found at: https://stefanwick.com/2017/06/24/uwp-app-with-systray-extension/   
https://stefanwick.com/2018/04/06/uwp-with-desktop-extension-part-1/ getting started, part-2 launching with params, part-3 communicating between components, part-4 submitting to the store  
https://stefanwick.com/2018/05/15/global-hotkey-registration-in-uwp/   
  
Current screenshots with usage information dialog: [./Package/Images/Screenshot Combined.png](./Package/Images/Screenshot%20combined.png)  

[comment]: # (![alt text](./Package/Images/Screenshot%20combined.png "Screenshot Combined Image"\))

# Feature and Fix updates
09/04/22 - build updates and feature updates
1. Package | Properties | Package | Target Version updated to current 10.0.22621 [ 22h2 ] from 10.0.17663.0 [ 1809 oct 2018 update ] and Min Version updated to 10.0.17663.0 from 16299.0 [ 1709 fall 2017 creators update ]  
and Package.appxmanifest | open in notepad | TargetDeviceFamily Name="Windows.Universal/Desktop" MinVersion and MaxVersionTested from 10.0.16299.0 and 10.0.17663.0 to 10.0.0.0 for both
2. SystrayExtension | Properties | Application | Target Version updated to current 10.0.22621 [ 22h2 ] vs 19041.0 [ 2004 may 2020 update ] from 10.0.17663.0 [ 1809 oct 2018 update ]
and Min Version updated to 10.0.17663.0 from 16299.0 [ 1709 fall 2017 creators update ]
and Package.appxmanifest | open in notepad | TargetDeviceFamily Name="Windows.Universal/Desktop" MinVersion and MaxVersionTested from 10.0.16299.0 and 10.0.17663.0 to 10.0.0.0 for both
3. SystrayComponent project & App.config + UnitTests project TargetFrameworkVersion setting update to current 4.8[.1] from 4.6.1
and SystrayComponent.csproj Reference Include="Windows" &lt;HintPath&gt;$(MSBuildProgramFiles32)\Windows Kits\10\UnionMetadata\&lt;Facade | 10.0.22621.0&gt;)\Windows.winmd&lt;/HintPath&gt;
4. xUnitTests project TargetFramework setting update to current net6.0 from netcoreapp2.1
5. updated text about how to add shortcut to startup folder to use win+r | shell:startup reference instead
6. changed 'close' button to instead read "minimize to system tray' and enabled alt+m[inimize] keyboard shortcut
7. added window left/right and up/down scroll bars -- todo: look into this next time
  
04/19/20 - feature updates and issue fixes
1. enabled textbox and slider control width control for alt+c and alt+shift+c window resizing behavior
2. enabled textbox and slider control width control for alt+arrow window repositioning and resizing behavior
3. enabled textbox and slider control width control for ctrl+arrow window repositioning and resizing behavior
4. fixed alt+arrow and ctrl+arrow actions when defined widths don't produce even screen partitioning sizes
  
06/04/18 - feature updates
1. added alt+p[hone] to center active window to 80% of screen height with modern smartphone 19:9 aspect ratio controlling the width
2. updated appxmanifest, waproj and csproj files to make use of rs5 17763 october 2018 sdk update
  
12/08/18 - feature updates
1. updated store app assets using visual assets generator and systray component notification area and usage info form to all derive from same updated source image
  
11/16/18 - feature updates
1. updated icon used in systray, aka notfication area, and in legacy windows form usage info view making use of "png to icon free" -> https://convertico.com/ service
  
11/16/18 - issue fixes
1. when in alt+c[enter] state ctrl+leftarrow correctly moves to left 2/3rds but ctrl+rightarrow moves to right 1/3rd not 2/3rds
2. when in win+leftarrow state ctrl+leftarrow moves to right 2/3rds instead of left 2/3rds and likewise when in win+rightarrow state and enter ctrl+rightarrow
3. when in alt+shift+c state alt+leftarrow has to be hit twice to move to left 3rd but same isn't true for alt+rightarrow which correctly moves to right 1/3rd
4. when in win+leftarrow state alt+leftarrow moves to right 1/3rd instead of left 3rd and likewise when in win+rightarrow state end enter ctrl+rightarrow
5. when in ctrl+leftarrow 1/3rd state ctrl+rightarrow moves to right 2/3rds instead of left 2/3rds and likewise in ctrl+rightarrow 1/3rd state
