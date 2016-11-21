﻿using bilibili2.Class;
using JYAnalyticsUniversal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace bilibili2
{
    public enum LoadType
    {
        OpenAvNum=0,
        OpenVideo=1,
        OpenLive = 2,
        OpenWeb=3
    }

    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }
     
        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            //if (System.Diagnostics.Debugger.IsAttached)
            //{
            //    this.DebugSettings.EnableFrameRateCounter = true;
            //}
#endif
            SqlHelper sql = new SqlHelper();
            sql.CreateTable();
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    // StatusBar.GetForCurrentView().HideAsync();
                    StatusBar statusBar = StatusBar.GetForCurrentView();
                    statusBar.ForegroundColor = Colors.White;
                    statusBar.BackgroundColor = Color.FromArgb(255, 223, 133, 160);
                    statusBar.BackgroundOpacity = 100;
                }

                //电脑标题栏颜色
                var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;

                Color co = Color.FromArgb(255, 223, 133, 160);
                titleBar.BackgroundColor = co;
                titleBar.ForegroundColor = Color.FromArgb(255, 254, 254, 254);//Colors.White纯白用不了。。。
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 223, 133, 180);
                titleBar.ButtonBackgroundColor = co;
                titleBar.ButtonForegroundColor = Color.FromArgb(255, 254, 254, 254);
                titleBar.InactiveBackgroundColor = co;
                titleBar.ButtonInactiveBackgroundColor = co;

                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // 当导航堆栈尚未还原时，导航到第一页，
                // 并通过将所需信息作为导航参数传入来配置
                // 参数

                rootFrame.Navigate(typeof(LoadPage), e.Arguments);
            }
            // 确保当前窗口处于活动状态
            Window.Current.Activate();
            await JYAnalytics.StartTrackAsync(ApiHelper.JyAppkey);
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await JYAnalytics.StartTrackAsync(ApiHelper.JyAppkey);
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
           
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
           
            //new MessageDialog("卧槽").ShowAsync();
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;
                string ban3 = Regex.Match(eventArgs.Uri.AbsoluteUri, @"^bilibili://video/(.*?)$").Groups[1].Value;
                string live = Regex.Match(eventArgs.Uri.AbsoluteUri, @"^bilibili://live/(.*?)$").Groups[1].Value;
                if (live.Length!=0)
                {
                    //args.Handled = true;
                    object[] par = new object[2];
                    par[0] = LoadType.OpenLive;
                    par[1] = live;
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (rootFrame == null)
                    {
                        rootFrame = new Frame();
                        Window.Current.Content = rootFrame;
                    }
                    if (rootFrame.Content == null)
                    {
                        rootFrame.Navigate(typeof(MainPage));
                    }
                    // 确保当前窗口处于活动状态
                    Window.Current.Activate();
                    rootFrame.Navigate(typeof(MainPage), par);
                    return;
                }

                if (ban3.Length != 0)
                {
                    //args.Handled = true;
                    object[] par = new object[2];
                    par[0] = LoadType.OpenAvNum;
                    par[1] = ban3;
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (rootFrame == null)
                    {
                        rootFrame = new Frame();
                        Window.Current.Content = rootFrame;
                    }
                    if (rootFrame.Content == null)
                    {
                        rootFrame.Navigate(typeof(MainPage));
                    }
                    // 确保当前窗口处于活动状态
                    Window.Current.Activate();
                    rootFrame.Navigate(typeof(MainPage), par);
                }
                else
                {
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (rootFrame == null)
                    {
                        rootFrame = new Frame();
                        Window.Current.Content = rootFrame;
                    }
                    if (rootFrame.Content == null)
                    {
                        rootFrame.Navigate(typeof(MainPage));
                    }
                    // 确保当前窗口处于活动状态
                    Window.Current.Activate();
                    rootFrame.Navigate(typeof(MainPage));
                }

            }
            await JYAnalytics.StartTrackAsync(ApiHelper.JyAppkey);
        

        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            object[] par = new object[2];
            par[0] = LoadType.OpenVideo;
            //DownloadManage.DownModel model = new DownloadManage.DownModel()
            //{
            //     path = args.Files[0].Path,
            //     title = args.Files[0].Name,
            //     mid= Regex.Match(args.Files[0].Name, @"(.*?)\.").Groups[1].Value,
            //};
            List<VideoModel> ls = new List<VideoModel>() {
                    new VideoModel {
                     path = args.Files[0].Path,
                    title = args.Files[0].Name??"",
                    cid =Regex.Match(args.Files[0].Name, @"(.*?)\.").Groups[1].Value,
                     IsOld=false
                    }
                };
            KeyValuePair<List<VideoModel>, int> Par = new KeyValuePair<List<VideoModel>, int>(ls, 0);
            // this.Frame.Navigate(typeof(PlayerPage), Par);
            par[1] = Par;
           Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();
                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // 当导航堆栈尚未还原时，导航到第一页，
                // 并通过将所需信息作为导航参数传入来配置
                // 参数

                rootFrame.Navigate(typeof(MainPage));
            }
            // 确保当前窗口处于活动状态
            Window.Current.Activate();
            rootFrame.Navigate(typeof(MainPage), par);
            //args.Files[0].Path;

        }

    }
}
