
using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SystrayExtension
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            SystemNavigationManagerPreview mgr =
                SystemNavigationManagerPreview.GetForCurrentView();
            mgr.CloseRequested += SystemNavigationManager_CloseRequested;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Package | Package.appxmanifest | Display Name = A Few Windows Niceties [afwn] controls title and can use following for runtime override
            // Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = "A Few Windows Niceties [afwn] Test";

            // these modifications modify all but the minimize, maximize and close section of title bar -- not sure what to alter to include that section
            //Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = Windows.UI.Colors.Black;
            //Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar.InactiveBackgroundColor = Windows.UI.Colors.Black;
            //Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar.ForegroundColor = Windows.UI.Colors.White;
            //Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar.InactiveForegroundColor = Windows.UI.Colors.White;

            //this.Width = 916; this.Height = 662; // overrides Page xaml defined Width="916" Height="662" and read-only ActualWidth/Height values
            // if we set these view properties here in code or in xaml for some reason it breaks page/grid/stackpanel/textblock[@TextWrapping=Wrap] behavior

            //octopusMove.Begin();

            //#if DEBUG
            // if launched using uwp store app package, vs desktop extensions launched win32 process, then automatically start systray component and close uwp usage information view
            //if (Process.GetProcessesByName("afwnsystraycomponent") == null) // System.PlatformNotSupportedException: 'Retrieving information about local processes is not supported on this platform.'
            //if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("systrayComponentRunning") || 
            //    Convert.ToBoolean(ApplicationData.Current.LocalSettings.Values["systrayComponentRunning"]) != true)
            //{
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                {
                    ApplicationData.Current.LocalSettings.Values["processId"] = Process.GetCurrentProcess().Id;
                    //App.AppServiceConnected += AppServiceConnected; // this was used in newer GlobalHotkey desktop extensions sample to facilitate uwp app processing of hotkeys
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    ApplicationData.Current.LocalSettings.Values["systrayComponentRunning"] = true;
                }
                //Window.Current.Close(); // "System.Runtime.InteropServices.COMException: 'A method was called at an unexpected time. Closing main window is not allowed.'
                //App.Current.Exit(); // the SystemNavigationManager_CloseRequested is only fired for user initiated close gestures such as clicking the X button
            //}
//#endif
        }

        private async void SystemNavigationManager_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            Deferral deferral = e.GetDeferral();
            //ConfirmCloseDialog dlg = new ConfirmCloseDialog();
            //ContentDialogResult result = await dlg.ShowAsync();
            //if (result == ContentDialogResult.Secondary)
            //{
            //    // user cancelled the close operation
            //    e.Handled = true;
            //    deferral.Complete();
            //}
            //else
            //{
            //    switch (dlg.Result)
            //    {
            //        case CloseAction.Terminate:
            //            e.Handled = false;
            //            deferral.Complete();
            //            break;

            //        case CloseAction.Systray:
                        if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                        {
                            ApplicationData.Current.LocalSettings.Values["processId"] = Process.GetCurrentProcess().Id;
                            //App.AppServiceConnected += AppServiceConnected; // this was used in newer GlobalHotkey desktop extensions sample to facilitate uwp app processing of hotkeys
                            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                            ApplicationData.Current.LocalSettings.Values["systrayComponentRunning"] = true;
                        }
                        e.Handled = false;
                        deferral.Complete();
            //            break;
            //    }
            //}
        }

        private void AppServiceConnected(object sender, AppServiceTriggerDetails e)
        {
            e.AppServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
        }

        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();
            int id = (int)args.Request.Message["id"];
            switch (id)
            {                
                case 1001:
                case 1002://center active window position
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        //paw.CenterActiveWindowPosition(); // using default parameter settings when none provided
                    });
                    break;
                case 1003:
                case 1004://swap mouse buttons
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        //var currentSetting = smb.GetMouseButtonsSetting();
                        //if (currentSetting == MouseButtonsSetting.RightHanded) smb.SetMouseButtonsSetting(MouseButtonsSetting.LeftHanded);
                        //else /* (currentSetting == MouseButtonSettings.LeftHanded) */ smb.SetMouseButtonsSetting(MouseButtonsSetting.RightHanded);
                    });
                    break;
                //case 1005:
                //case 1006://lock and engage modern standby vs legacy [s3] suspend
                //    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //    {
                //        // TODO: implement lock and engage modern standby vs legacy [s3] suspend
                //    });
                //    break;
                default:
                    break;
            }
            await args.Request.SendResponseAsync(new ValueSet());
            messageDeferral.Complete();

            // we no longer need the connection
            App.AppServiceDeferral.Complete();
            App.Connection = null;
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            //Window.Current.Close(); // "System.Runtime.InteropServices.COMException: 'A method was called at an unexpected time. Closing main window is not allowed.'
            App.Current.Exit(); // the SystemNavigationManager_CloseRequested is only fired for user initiated close gestures such as clicking the X button
        }

        // Quick Actions (Alt+Enter or Ctrl+.) | Use expression body for methods . . . but under what circumstances other than single line implementations should you not do this?
        // search "quick actions use expression body for methods" -> https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/expression-bodied-members
        // Expression body definitions let you provide a member's implementation in a very concise, readable form. You can use an expression body definition whenever the logic for any supported 
        // member, such as a method or property, consists of a single expression.
        private void exit_Click(object sender, RoutedEventArgs e) => App.Current.Exit();
    }
}
