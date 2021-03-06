﻿using bilibili2.Class;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace bilibili2.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class DownloadPage : Page
    {
        public delegate void GoBackHandler();
        public event GoBackHandler BackEvent;
        public DownloadPage()
        {
            this.InitializeComponent();
            //NavigationCacheMode = NavigationCacheMode.Enabled;
        }
        private void btn_back_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
            else
            {
                BackEvent();
            }
        }
        List<DownloadManage.HandleModel> downlingModel = new List<DownloadManage.HandleModel>();
        List<string> HandleList = new List<string>();
        SettingHelper settings = new SettingHelper();
        //跳转，开始监视，需要判断是否已经监视，否则会出现N个通知
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode== NavigationMode.Back)
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                pivot.SelectedIndex = 1;
            }
            //messShow.Show("Tips：如果下载完成后应用不显示\r\n请点击已完成右下角的'导入已完成'", 5000);
            bg.Color = ((SolidColorBrush)this.Frame.Tag).Color;
            if (e.Parameter!=null)
            {
                pivot.SelectedIndex = (int)e.Parameter;
            }
            try
            {
                if (settings.SettingContains("HoldLight"))
                {
                    tw_Light.IsChecked = (bool)settings.GetSettingValue("HoldLight");
                }
                else
                {
                    tw_Light.IsChecked = false;
                }

                list_Downing.Items.Clear();
                downlingModel.Clear();
                IReadOnlyList<DownloadOperation> downloads = null;
               
            GetDownOk_New();
                // 获取所有后台下载任务
                downloads = await BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(DownloadManage.DownModel.group);
                if (downloads.Count > 0)
                {
                    foreach (var item in downloads)
                    {
                        //ls.Add(item.Guid);
                        list_Downing.Items.Add(new DownloadManage.HandleModel()
                        {
                            downOp = item,
                            Size = item.Progress.BytesReceived.ToString(),
                            downModel = await GetInfo(item.Guid.ToString())
                        });
                    }
                    //list.ItemsSource = downlingModel;
                }
                if (downloads.Count > 0)
                {
                    List<Task> tasks = new List<Task>();
                    foreach (DownloadManage.HandleModel model in list_Downing.Items)
                    {
                        //bool test = HandleList.Contains(model.downOp.Guid.ToString());
                        if (!HandleList.Contains(model.downOp.Guid.ToString()))
                        {
                            // 监视指定的后台下载任务
                            HandleList.Add(model.downOp.Guid.ToString());
                            tasks.Add(HandleDownloadAsync(model));
                        }
                    }
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (list_Downing.Items.Count!=0)
                {
                    txt_NoDown.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txt_NoDown.Visibility = Visibility.Visible;
                }
            }
        }

        //读取文件
        public async Task<DownloadManage.DownModel> GetInfo(string guid)
        {
           // StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder DowFolder = await KnownFolders.VideosLibrary.CreateFolderAsync("Bili-Down", CreationCollisionOption.OpenIfExists);
            StorageFile file = await DowFolder.GetFileAsync(guid + ".bili");
            //用Url编码是因为不支持读取中文名
            //含会出现：在多字节的目标代码页中，没有此 Unicode 字符可以映射到的字符。错误
            string path = WebUtility.UrlDecode(await FileIO.ReadTextAsync(file)) + @"\" + guid + ".json";
            StorageFile files = await StorageFile.GetFileFromPathAsync(path);
            string json = await FileIO.ReadTextAsync(files);
            return JsonConvert.DeserializeObject<DownloadManage.DownModel>(json);
        }
       

        private async void GetDownOk_New()
        {
            try
            {
                list_Downed.ItemsSource = null;
                DownloadManage.Downloaded.Clear();
                await Task.Run(async () =>
                {
                    StorageFolder DownFolder = await KnownFolders.VideosLibrary.CreateFolderAsync("Bili-Down", CreationCollisionOption.OpenIfExists);
                    List<DownloadManage.FolderModel> list = new List<DownloadManage.FolderModel>();
                    foreach (var item in await DownFolder.GetFoldersAsync())
                    {
                        DownloadManage.FolderModel model = new DownloadManage.FolderModel() {
                            title = item.Name,
                             count=0,
                             downedCount=0,
                        };
                        List<DownloadManage.DownModel> list_file = new List<DownloadManage.DownModel>();
                        foreach (var item1 in await item.GetFoldersAsync())
                        {
                            foreach (var item2 in await item1.GetFilesAsync())
                            {
                                try
                                {
                                    if (item2.FileType == ".json")
                                    {
                                        StorageFile files = item2;
                                        string json = await FileIO.ReadTextAsync(item2);
                                        DownloadManage.DownModel model123 = JsonConvert.DeserializeObject<DownloadManage.DownModel>(json);
                                        if (model123.downloaded == true)
                                        {
                                            list_file.Add(model123);
                                            model.downedCount++;
                                            DownloadManage.Downloaded.Add(model123.mid);
                                        }
                                        model.IsBangumi = model123.isBangumi;
                                        model.aid = model123.aid;
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            model.count++;
                        }
                        model.path = item.Path;
                        model.downModel = list_file;
                        list.Add(model);
                    }
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { list_Downed.ItemsSource = list; });
                });
            }
            catch (Exception ex)
            {
                messShow.Show("读取已下载失败\r\n"+ex.Message,2000);
            }

        }
        /// <summary>
        /// 监视指定的后台下载任务
        /// </summary>
        /// <param name="download">后台下载任务</param>
        private async Task HandleDownloadAsync(DownloadManage.HandleModel model)
        {
            try
            {
                DownloadProgress(model.downOp);
                //进度监控
                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                await model.downOp.AttachAsync().AsTask(model.cts.Token, progressCallback);

                //保存任务信息
                //  StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder DowFolder = await KnownFolders.VideosLibrary.CreateFolderAsync("Bili-Down", CreationCollisionOption.OpenIfExists);
                StorageFile file = await DowFolder.GetFileAsync(model.Guid + ".bili");
                //用Url编码是因为不支持读取中文名
                //含会出现：在多字节的目标代码页中，没有此 Unicode 字符可以映射到的字符。错误
                string path = WebUtility.UrlDecode(await FileIO.ReadTextAsync(file)) ;
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                StorageFile files = await folder.CreateFileAsync(model.Guid + ".json", CreationCollisionOption.OpenIfExists); //await StorageFile.GetFileFromPathAsync(path+@"\" + model.Guid + ".json");
                string json = await FileIO.ReadTextAsync(files);
                DownloadManage.DownModel info = JsonConvert.DeserializeObject<DownloadManage.DownModel>(json);
                info.downloaded = true;
                string jsonInfo = JsonConvert.SerializeObject(info);
                StorageFile fileWrite = await folder.CreateFileAsync(info.Guid + ".json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(fileWrite, jsonInfo);
                //移除正在监控
                HandleList.Remove(model.downModel.Guid);
                GetDownOk_New();
            }
            catch (TaskCanceledException)
            {
                //取消通知
                //ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
                //XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
                //XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
                //IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                //((XmlElement)toastNode).SetAttribute("duration", "short");
                //toastTextElements[0].AppendChild(toastXml.CreateTextNode(String.Format("取消任务{0}", model.downModel.title)));
                //ToastNotification toast = new ToastNotification(toastXml);
                //ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch (Exception ex)
            {
                WebErrorStatus error = BackgroundTransferError.GetStatus(ex.HResult);
                return;
            }
            finally
            {
                list_Downing.Items.Remove(model);
            }
        }
        //进度发生变化时,通知更新UI
        private void DownloadProgress(DownloadOperation op)
        {
            try
            {
                DownloadManage.HandleModel test = null;
                foreach (DownloadManage.HandleModel item in list_Downing.Items)
                {
                    if (item.downOp.Guid == op.Guid)
                    {
                        test = item;
                    }
                }
                if (list_Downing.Items.Contains(test))
                {
                    ((DownloadManage.HandleModel)list_Downing.Items[list_Downing.Items.IndexOf(test)]).Size = op.Progress.BytesReceived.ToString();
                    ((DownloadManage.HandleModel)list_Downing.Items[list_Downing.Items.IndexOf(test)]).Status = op.Progress.Status.ToString();
                    ((DownloadManage.HandleModel)list_Downing.Items[list_Downing.Items.IndexOf(test)]).Progress = ((double)op.Progress.BytesReceived / op.Progress.TotalBytesToReceive) * 100;
                }
            }
            catch (Exception)
            {
                return;
            }

        }
        //继续下载
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            // list_Downing.SelectionMode = ListViewSelectionMode.Multiple;
            if (list_Downing.SelectedItems.Count != 0)
            {
                foreach (DownloadManage.HandleModel item in list_Downing.SelectedItems)
                {
                    if (item.downOp.Progress.Status == BackgroundTransferStatus.PausedByApplication)
                    {
                        item.downOp.Resume();
                        //GetDownInfo();
                    }
                }
            }
        }
        //暂停
        private void btn_Pause_Click(object sender, RoutedEventArgs e)
        {
            if (list_Downing.SelectedItems.Count != 0)
            {
                foreach (DownloadManage.HandleModel item in list_Downing.SelectedItems)
                {
                    if (item.downOp.Progress.Status == BackgroundTransferStatus.Running)
                    {
                        item.downOp.Pause();
                    }
                }
            }
        }
        //取消
        private async void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (list_Downing.SelectedItems.Count != 0)
            {
                foreach (DownloadManage.HandleModel item in list_Downing.SelectedItems)
                {
                    item.cts.Cancel(false);
                    item.cts.Dispose();
                    try
                    {
                       
                        StorageFolder DowFolder = await KnownFolders.VideosLibrary.CreateFolderAsync("Bili-Down", CreationCollisionOption.OpenIfExists);
                        StorageFile file = await DowFolder.GetFileAsync(item.Guid + ".bili");
                        //用Url编码是因为不支持读取中文名
                        //含会出现：在多字节的目标代码页中，没有此 Unicode 字符可以映射到的字符。错误
                        string path = WebUtility.UrlDecode(await FileIO.ReadTextAsync(file));
                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                        await folder.DeleteAsync(StorageDeleteOption.Default);
                         await file.DeleteAsync( StorageDeleteOption.Default);
                    }
                    catch (Exception)
                    {
                        //throw;
                    }
                }
            }
            //   _cancelToken.Cancel(false);
            //    _cancelToken.Dispose();

        }
        //设置
        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingPage), 2);
        }
        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txt_Hea_0.FontWeight = FontWeights.Normal;
            txt_Hea_1.FontWeight = FontWeights.Normal;
            switch ((sender as Pivot).SelectedIndex)
            {
                case 0:
                    bar_down.Visibility = Visibility.Visible;
                    bar_down_OK_New.Visibility = Visibility.Collapsed;
                    txt_Hea_0.FontWeight = FontWeights.Bold;
                    break;
                case 1:
                    bar_down.Visibility = Visibility.Collapsed;
                    bar_down_OK_New.Visibility = Visibility.Visible;
                    txt_Hea_1.FontWeight = FontWeights.Bold;
                    break;

                default:
                    break;
            }

        }
       
       

        private void btn_Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetDownOk_New();
        }


        private void list_Downed_ItemClick(object sender, ItemClickEventArgs e)
        {
            sp_Video.IsPaneOpen = true;
            DownloadManage.FolderModel folder = (e.ClickedItem as DownloadManage.FolderModel);
            txt_Title.Tag = folder.IsBangumi;
            btn_Go.Tag = folder.aid;
            if (folder.IsBangumi)
            {
                txt_Title.Text = folder.title + "(SID" + folder.aid + ")";
            }
            else
            {
                txt_Title.Text = folder.title + "(AV" + folder.aid + ")";
            }
            list_DownOk_View.ItemsSource = folder.downModel;
            //var a = (e.ClickedItem as DownloadManage.FolderModel).downModel;
            //foreach (var item in (e.ClickedItem as DownloadManage.FolderModel).downModel)
            //{
            //    list_DownOk.Items.Add(item);
            //}
       
            // 
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
          
           if (this.ActualWidth<=500)
            {
                sp_Video.OpenPaneLength = this.ActualWidth;
            }
            else
            {
                sp_Video.OpenPaneLength = 350;
            }
        }

        private void cb_btn_Select_Checked(object sender, RoutedEventArgs e)
        {
            list_DownOk_View.IsItemClickEnabled = false;
             list_DownOk_View.SelectionMode = ListViewSelectionMode.Multiple;

        }

        private void list_DownOk_View_ItemClick(object sender, ItemClickEventArgs e)
        {
            //DownloadManage.DownModel model = (DownloadManage.DownModel)e.ClickedItem;
            List<VideoModel> ls = new List<VideoModel>();
            var list = list_DownOk_View.ItemsSource as List<DownloadManage.DownModel>;
            foreach (var item in list)
            {
                ls.Add(new VideoModel
                {
                    path = item.path,
                    title = item.title ?? "",
                    page=item.part,
                    cid = item.mid,
                    IsOld = false
                });
            }
            KeyValuePair<List<VideoModel>, int> Par = new KeyValuePair<List<VideoModel>, int>(ls, list.IndexOf(e.ClickedItem as DownloadManage.DownModel));
            this.Frame.Navigate(typeof(PlayerPage), Par);

        }

        private void cb_btn_Select_Unchecked(object sender, RoutedEventArgs e)
        {
            list_DownOk_View.IsItemClickEnabled = true;
            list_DownOk_View.SelectionMode = ListViewSelectionMode.None;
        }

        private void btn_Select_Unchecked(object sender, RoutedEventArgs e)
        {
            list_Downed.IsItemClickEnabled = true;
            list_Downed.SelectionMode = ListViewSelectionMode.None;
        }

        private void btn_Select_Checked(object sender, RoutedEventArgs e)
        {
            list_Downed.IsItemClickEnabled = false;
            list_Downed.SelectionMode = ListViewSelectionMode.Multiple;
        }

        private void btn_Go_Click(object sender, RoutedEventArgs e)
        {
            //btn_Go.Tag
            if ((bool)txt_Title.Tag)
            {
                this.Frame.Navigate(typeof(BanInfoPage), btn_Go.Tag.ToString());
            }
            else
            {
                this.Frame.Navigate(typeof(VideoInfoPage), btn_Go.Tag.ToString());
            }
        }

        private async void btn_Delete_Folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DownloadManage.FolderModel item in list_Downed.SelectedItems)
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(item.path);
                    
                    await folder.DeleteAsync(StorageDeleteOption.Default);
                }
                await new MessageDialog("成功删除！").ShowAsync();
            }
            catch (Exception ex)
            {
                await new MessageDialog("删除时发生错误\r\n"+ex.Message).ShowAsync();
            }
            finally
            {
                GetDownOk_New();
            }
        }

        private async void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DownloadManage.DownModel item in list_DownOk_View.SelectedItems)
                {
                    StorageFolder folder = await (await StorageFile.GetFileFromPathAsync(item.path)).GetParentAsync();
                    StorageFile file=await (await KnownFolders.VideosLibrary.CreateFolderAsync("Bili-Down", CreationCollisionOption.OpenIfExists)).CreateFileAsync(item.Guid+".bili", CreationCollisionOption.OpenIfExists);
                    await folder.DeleteAsync(StorageDeleteOption.Default);
                    await file.DeleteAsync(StorageDeleteOption.Default);
                }
                await new MessageDialog("成功删除！").ShowAsync();
            }
            catch (Exception ex)
            {
                await new MessageDialog("删除时发生错误\r\n" + ex.Message).ShowAsync();
            }
            finally
            {
                GetDownOk_New();
                sp_Video.IsPaneOpen = false;
            }
        }

        private DisplayRequest dispRequest = null;
        private void AppBarToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (dispRequest == null)
            {
                // 用户观看视频，需要保持屏幕的点亮状态
                dispRequest = new DisplayRequest();
                dispRequest.RequestActive(); // 激活显示请求
            }
            settings.SetSettingValue("HoldLight", tw_Light.IsChecked.Value);
        }

        private void AppBarToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (dispRequest != null)
            {
                dispRequest = null;
            }
            settings.SetSettingValue("HoldLight", tw_Light.IsChecked.Value);
        }

        private async void btn_Input_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog("此功能能修复一些已经下载完成，但应用里不显示的问题\r\n只需要选择视频库\\Bili-Down\\[视频标题]\\[集数]\\[很长一段ID].josn，然后稍等片刻即可").ShowAsync();
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            filePicker.FileTypeFilter.Add(".json");
            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file==null)
            {
                return;
            }
            if (file.FileType!=".json")
            {
                await new MessageDialog("错误的文件格式！").ShowAsync();
                
            }
            string json = await FileIO.ReadTextAsync(file);
            DownloadManage.DownModel info = JsonConvert.DeserializeObject<DownloadManage.DownModel>(json);
            info.downloaded = true;
            string jsonInfo = JsonConvert.SerializeObject(info);
            //StorageFile fileWrite = await folder.CreateFileAsync(info.Guid + ".json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, jsonInfo);
            GetDownOk_New();
            messShow.Show("导入完成",3000);
        }

        private void btn_Pause_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                ((sender as AppBarButton).DataContext as DownloadManage.HandleModel).downOp.Pause();
            }
            catch (Exception)
            {
                messShow.Show("操作失败", 2000);
            }
          
        }

        private void btn_Download_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ss = ((sender as AppBarButton).DataContext as DownloadManage.HandleModel);
                if (ss.downOp.Progress.Status == BackgroundTransferStatus.PausedByApplication)
                {
                    ss.downOp.Resume();
                }
            }
            catch (Exception)
            {
                messShow.Show("操作失败", 2000);
            }
           
        }

        private async void btn_Canacel_Click(object sender, RoutedEventArgs e)
        {
            var ss = ((sender as AppBarButton).DataContext as DownloadManage.HandleModel);
            ss.cts.Cancel(false);
            ss.cts.Dispose();
            try
            {

                StorageFolder DowFolder = await KnownFolders.VideosLibrary.CreateFolderAsync("ZF-Down", CreationCollisionOption.OpenIfExists);
                StorageFile file = await DowFolder.GetFileAsync(ss.Guid + ".bili");
                //用Url编码是因为不支持读取中文名
                //含会出现：在多字节的目标代码页中，没有此 Unicode 字符可以映射到的字符。错误
                string path = WebUtility.UrlDecode(await FileIO.ReadTextAsync(file));
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                await folder.DeleteAsync(StorageDeleteOption.Default);
                await file.DeleteAsync(StorageDeleteOption.Default);
            }
            catch (Exception)
            {
                messShow.Show("操作失败",2000);
                //throw;
            }
        }
    }
}
