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
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Forms;

namespace SystrayComponent
{
    public partial class Form1 : Form
    {
        SystrayApplicationContext systrayApplicationContext = null;

        public Form1()
        {
            InitializeComponent();
        }

        public Form1(SystrayApplicationContext systrayApplicationContext) : this()
        {
            this.systrayApplicationContext = systrayApplicationContext;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {            
            this.linkLabel1.LinkVisited = true; // specify that the link was visited
            Process.Start("https://paypal.me/ob1cot"); // navigate to a url
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel1.LinkVisited = true; // specify that the link was visited
            Process.Start("https://github.com/randyrants/sharpkeys/releases"); // navigate to a url
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            systrayApplicationContext.Exit(sender, e);
        }

        private void button3_Click(object sender, System.EventArgs e)
        {
//https://winhelponline.com/blog/clear-customize-notifications-tray-items-windows-7-vista-xp/
//from admin prompt for %i in ( IconStreams PastIconsStream PromotedIconCache ) do (
//  reg delete "hkcu\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify" /v %i /f )
//then from non-admin prompt pskill explorer & explorer to refresh win+i [ settings ] | personalization | taskbar | other system tray icons list
//can execute from SystrayComponent .net framework winforms app and not SystrayExtension uwp xaml app as it blocks uac permission escalation prompts
            string keyName = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key != null)
                {
                    if (key.GetValue("IconStreams") != null) key.DeleteValue("IconStreams");
                    if (key.GetValue("PastIconStream") != null) key.DeleteValue("PastIconsStream");
                    if (key.GetValue("PromotedIconCache") != null) key.DeleteValue("PromotedIconCache");
                    foreach (var process in Process.GetProcessesByName("explorer")) process.Kill(); // typically only one instance
                    //Process.Start("%windir%\\explorer.exe"); // restarted automatically unlike when you use pskill on command line
                }
            }

        }
    }
}
