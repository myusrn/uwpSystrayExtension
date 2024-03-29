﻿# A Few Windows Niceties (afwn) 
last visited = 12/20/23, status = in-progress

Application provides global hotkey, aka keyboard shortcut, behaviors which i found myself wanting ever since moving to 25" and greater 16:9 aspect ratio displays and even more so with newer ultrawide 21:9 and super-ultrawide 32:9 aspect ratio displays. While windows provides Win+LeftArrow and Win+RightArrow keyboard shortcuts for creating initial 50/50 layout, that can then be dragged to any split you like, and newer min/max window sizing icon mouse hover for a set of predefined window placements, I found these didn't address the window positioning scenarios i was commonly wanting quick and easy keyboard shortcut access to when doing reading and development work on widescreen displays. 

It also includes non-window positioning and sizeing related hotkeys such as on for mouse button swapping which i regularly use to keep from getting stuck in mouse hand specific postures during long work sessions. See the app's system tray, aka notification area, "Open Usage Info" context menu option for details on full list of enabled hotkeys.

The Alt+C[enter] and Alt+Shift+[Center] hotkeys i find address making reading easier. For some science of what happens when you read, that may explain why widescreen reading is pita, see [Tim Ferriss' tricks for reading two times faster video](https://youtube.com/watch?v=CZU6G8EMUE4). In it he explains how using narrower page widths reduces number of eye refocusing events per line.

This application makes use of uwp app desktop bridge/extensions support, aka centennial program, to initiate a win32 process that runs in the system tray, aka notification area. This is accomplished via FullTrustProcessLauncher app. The communication between launched win32 app and parent uwp app is done with an in-proc AppService.

The current release is available in the microsoft store. The benefit of installing from there is the trusted source and sandbox execution experience.  This allows use on IT policy controlled machines where things like fixed whitelist based malware detection software might block directly executed win32 apps from being run. On my workstation when i want to focus on debugging core behavior i just launch systrayComponent win32 output  directly. It has logic to detect if was started by uwp store app or directly and behave accordingly, e.g. launching uwp or winform based show usage information view.   

# Find, Install and Run from Microsoft Store 
- for Microsoft Store web listing https://microsoft.com/en-us/p/a-few-windows-niceties/9mtw4cj7s276?activetab=pivot%3Aoverviewtab
- for Microsoft Store app listing search on "A Few Windows Niceties"

# Build, Deploy and Run from Sources
- In visual studio installer enable the '.NET desktop development' and 'Universal Windows Platform development' workloads and confirm individual components '.NET' -> '.NET 6.0 Runtime (Long Term Support), 
'.NET Framework 4.8 SDK', '.NET Framework 4.8 targeting pack' and 'SDK, libraries, and frameworks' -> 'Windows 11 SDK (10.0.22621.0)' are included
- In windows enable win+i [ settings ] | system | for developers | developer mode = off -> on [ which you can turn back off when build, run, debug/test work is done ]
- For SystrayExtension\SystrayExtension_TemporaryKey.pfx [ not password protected ] and Package\Package_TemporaryKey.pfx [ password protected and persisted in user certificates store ] see Package | Package.appxmanifest | Packaging | Choose Certificate | Create & How to create package signing certificate -&gt; https://docs.microsoft.com/en-us/windows/msix/package/create-certificate-package-signing.  Also
review 'Partner Center App Invalid package family name Invalid package publisher name' -&gt; https://stackoverflow.com/questions/40951570/uwp-app-invalid-package-family-name-after-update-certificate which outlines
how whenever you are having to recreate the Package_TemporaryKey.pfx you need to paste your publisher guid, e.g. in this case 97AB39F5-C8A8-4ED8-A44F-452C1110B98B, as the certificate common name. Both have Intended Purposes / Enhanced Key 
Usage assignments Code Signing (1.3.6.1.5.5.7.3.3), Unknown Key Usage (1.3.6.1.4.1.311.84.3.1) .
- For Package_StoreAssociation.xml see Package | Publish | Associate App with the Store | Include apps that already have packages = checked | Refresh | &lt;select existing published app entry&gt; | Asssociate -&gt; Package.StoreAssociation.xml and 'create or get uwp store pfx' -&gt; https://stackoverflow.com/questions/42209953/how-to-create-or-get-a-new-storekey-pfx-for-uwp-application   
- Select store app package project with build configuration Deploy enabled, in this case Package, or Uwp System Tray project, in this case SystrayComponent, and choose 'Set as Startup Project' 
- Press F5 to Start Debugging or Ctrl+F5 to Start Without Debugging

# Publish new Build to Store
- Package | Package.appxmanifest | Packaging | Version | Build increment by 1
- Change build target to Release | x86 [ or ARM ] and use Ctrl+F5 to Start Without Debugging to capture updates for Package | Images | Screenshot 1-3.png and paste in OneNote to capture update for [Screenshot Combined.png](./Package/Images/Screenshot%20combined.png)
- Package | Publish | Create App Packages | how will you distribute = Microsoft Store as &lt;existing published app name&gt; | generate app bundle = Always, packages to create = x86 Release (x86) [ and/or ARM Release (ARM) ], for help see [Devices and Architectures](https://docs.microsoft.com/en-us/windows/msix/package/device-architecture), included public symbol files, generate artifacts | Create | Launch App Certification w/o Automatically submit to store after -&gt; Overall Result Passed [ with Warnings ] | Finish  
- [Partner Center Apps and Games](https://partner.microsoft.com/dashboard/products) | &lt;existing published app name&gt; | &lt;existing last submission&gt; update which creates a new submission | packages | upload Package\AppPackages\Package_&lt;version&gt;_&lt;architecture&gt;_bundle.msixbundle and Save | Store listings - English (United States) | What's new in this version = &lt;details from readme&gt; + Product features = See screen shot for complete set of keyboard shortcuts provided. +  Screenshots - Desktop = &lt;delete and replace with updated 'Screenshot Combined.png' and caption = 'usage info and system tray icon' and Save | Submit to the Store  
- Then use &lt;new submission&gt; | status | view progress to track follow up. Once it shows as having been published visit Microsoft Store and confirm new version is present, installable, and has feature updates and issue fixes in place.  
- For Properties | Support info | Privacy policy URL and Website use anonymously accessible html and pdf links to azure storage account blob container as gdrive, icloud and onedrive anonymously accessible sharing links are not reachable from china  

# Samples, and comments q&a, that facilitated this work 
- https://stefanwick.com/2017/06/24/uwp-app-with-systray-extension/  
- https://stefanwick.com/2018/04/06/uwp-with-desktop-extension-part-1/ getting started, part-2 launching with params, part-3 communicating between components, part-4 submitting to the store  
- https://stefanwick.com/2018/05/15/global-hotkey-registration-in-uwp/  
- https://winhelponline.com/blog/clear-customize-notifications-tray-items-windows-7-vista-xp/ outlining how to clear system tray icons of no longer installed applications  
from admin prompt for %i in ( IconStreams PastIconsStream PromotedIconCache ) do ( reg delete "hkcu\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify" /v %i /f )  
then from non-admin prompt pskill explorer.exe & explorer.exe to refresh win+i [ settings ] | personalization | taskbar | other system tray icons list  
  
# Feature and Fix updates
12/20/23 - feature updates
1. updated alt+c, atl+t, alt+arrow, ctrl+arrow and their shift modifier behaviors to work when used on all displays of a multiple displays setup
2. plumbed out alt+s[tatus] keyboard shortcut modifier path to later implement toggling teams client between Appear Away and Reset Status presence mode 

01/25/23 - build updates and feature updates
1. updated phone resizing aspect ratio from 9:16 to 9:19.5 80% of screen height to align with modern phones and shift modifier to 19.5:9 landscape mode 50% of screen height
2. updated tablet window resizing aspect ratio from 16:9 to 16:10 80% of screen height to align with modern samsung tablets and left shift modifier as is using ipad 4:3 80%
3. updated comments on use of SetSuspendState api and the modern standby vs legacy and hibernate behaviors
4. updated notes about recreation of package build output signing certificate keys

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
6. changed 'close' button to instead read "minimize to system tray' and enabled alt+s[ystem tray] keyboard shortcut
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
