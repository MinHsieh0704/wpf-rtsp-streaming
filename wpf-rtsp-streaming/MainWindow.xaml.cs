using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using wpf_rtsp_streaming.Helpers;

namespace wpf_rtsp_streaming
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public string AppVersion
        {
            get { return (string)GetValue(AppVersionProperty); }
            set { SetValue(AppVersionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AppVersion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AppVersionProperty =
            DependencyProperty.Register("AppVersion", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public bool IsStart
        {
            get { return (bool)GetValue(IsStartProperty); }
            set { SetValue(IsStartProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsStart.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsStartProperty =
            DependencyProperty.Register("IsStart", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsShowLoading
        {
            get { return (bool)GetValue(IsShowLoadingProperty); }
            set { SetValue(IsShowLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsShowLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsShowLoadingProperty =
            DependencyProperty.Register("IsShowLoading", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsShowAddSteamingForm
        {
            get { return (bool)GetValue(IsShowAddSteamingFormProperty); }
            set { SetValue(IsShowAddSteamingFormProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsShowAddSteamingForm.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsShowAddSteamingFormProperty =
            DependencyProperty.Register("IsShowAddSteamingForm", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                this.Title = App.AppName;

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

                this.AppVersion = $"v{fileInfo.FileVersion}";
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);

                App.Current.Shutdown(1);
            }
        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsShowLoading = true;

                FirewallRule firewallRule = new FirewallRule();
                if (await firewallRule.Search($"{App.AppName}_{App.RTSPPort}"))
                {
                    await firewallRule.Add($"{App.AppName}_{App.RTSPPort}", App.RTSPPort);
                }

                await Task.Run(new Action(() =>
                {
                    try
                    {
                        App.Mediamtx = new Mediamtx();

                        App.Mediamtx.onMessage.Subscribe((x) =>
                        {
                            try
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    App.PrintService.Log($"Mediamtx [{App.Mediamtx.processId}], {x}", Print.EMode.message, "Mediamtx");
                                }));
                            }
                            catch (TaskCanceledException ex)
                            {
                            }
                        });
                        App.Mediamtx.onError.Subscribe((x) =>
                        {
                            try
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    this.StopMediamtx();

                                    x = ExceptionHelper.GetReal(x);
                                    string message = x.Message;

                                    App.PrintService.Log($"Mediamtx [{App.Mediamtx.processId}], {message}", Print.EMode.error, "Mediamtx");

                                    MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                                }));
                            }
                            catch (TaskCanceledException ex)
                            {
                            }
                        });

                        App.Mediamtx.Connect();
                    }
                    catch (Exception ex)
                    {
                        ex = ExceptionHelper.GetReal(ex);
                        string message = ex.Message;

                        App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                        MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));

                App.Streamings = new List<Streaming>();
                foreach (var streamingInfo in DataCenter.DataCenter.StreamingInfo.StreamingInfos)
                {
                    this.InitStreaming(streamingInfo);
                }

                this.IsStart = true;
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsShowLoading = false;
            }
        }

        private async void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsShowLoading = true;

                await Task.Run(new Action(() =>
                {
                    try
                    {
                        this.StopMediamtx();
                    }
                    catch (Exception ex)
                    {
                        ex = ExceptionHelper.GetReal(ex);
                        string message = ex.Message;

                        App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                        MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsShowLoading = false;
            }
        }

        private void AddSteaming_Save(object sender, Components.AddSteaming.StreamingEventArgs e)
        {
            try
            {
                DataCenter.StreamingInfo.IStreamingInfo streamingInfo = new DataCenter.StreamingInfo.IStreamingInfo()
                {
                    FilePath = e.FilePath,
                    RTSPPath = e.RTSPPath,
                    IsStart = false,
                };
                DataCenter.DataCenter.StreamingInfo.Add(streamingInfo);

                this.InitStreaming(streamingInfo);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void AddSteaming_Cancel(object sender, EventArgs e)
        {
            try
            {
                this.IsShowAddSteamingForm = false;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ButtonAddStreaming_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsShowAddSteamingForm = true;
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ButtonStreamingStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsShowLoading = true;

                Button element = sender as Button;
                if (element == null)
                {
                    return;
                }

                DataCenter.StreamingInfo.IStreamingInfo streamingInfo = element.DataContext as DataCenter.StreamingInfo.IStreamingInfo;
                if (streamingInfo == null)
                {
                    return;
                }

                foreach (var item in DataCenter.DataCenter.StreamingInfo.StreamingInfos)
                {
                    if (item.Index != streamingInfo.Index)
                    {
                        continue;
                    }

                    item.IsStart = true;

                    if (App.Streamings != null)
                    {
                        await App.Streamings[item.Index - 1].Connect();
                    }
                }

                this.Streaming.Items.Refresh();
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsShowLoading = false;
            }
        }

        private async void ButtonStreamingStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsShowLoading = true;

                Button element = sender as Button;
                if (element == null)
                {
                    return;
                }

                DataCenter.StreamingInfo.IStreamingInfo streamingInfo = element.DataContext as DataCenter.StreamingInfo.IStreamingInfo;
                if (streamingInfo == null)
                {
                    return;
                }

                await Task.Run(new Action(() =>
                {
                    try
                    {
                        this.StopStreaming(streamingInfo);
                    }
                    catch (Exception ex)
                    {
                        ex = ExceptionHelper.GetReal(ex);
                        string message = ex.Message;

                        App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                        MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsShowLoading = false;
            }
        }

        private void ButtonStreamingDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button element = sender as Button;
                if (element == null)
                {
                    return;
                }

                DataCenter.StreamingInfo.IStreamingInfo streamingInfo = element.DataContext as DataCenter.StreamingInfo.IStreamingInfo;
                if (streamingInfo == null)
                {
                    return;
                }

                DataCenter.DataCenter.StreamingInfo.Delete(streamingInfo);

                if (App.Streamings != null)
                {
                    Streaming streaming = App.Streamings.Where((n) => n.rtspPath == streamingInfo.RTSPPath).FirstOrDefault();
                    App.Streamings.Remove(streaming);
                }
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopMediamtx()
        {
            try
            {
                if (App.Streamings != null)
                {
                    foreach (var streaming in App.Streamings)
                    {
                        streaming.Dispose();
                    }
                }

                this.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (var item in DataCenter.DataCenter.StreamingInfo.StreamingInfos)
                    {
                        item.IsStart = false;
                    }
                }));

                App.Mediamtx.Dispose();

                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.IsStart = false;

                    this.Streaming.Items.Refresh();
                }));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void InitStreaming(DataCenter.StreamingInfo.IStreamingInfo streamingInfo)
        {
            try
            {
                Streaming streaming = new Streaming();
                streaming.filePath = streamingInfo.FilePath;
                streaming.rtspPath = streamingInfo.RTSPPath;

                streaming.onMessage.Subscribe((x) =>
                {
                    try
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            App.PrintService.Log($"Streaming_{streamingInfo.RTSPPath} [{streaming.processId}], {x}", Print.EMode.message, $"Streaming_{streamingInfo.RTSPPath.Replace("/", "_")}");
                        }));
                    }
                    catch (TaskCanceledException ex)
                    {
                    }
                });
                streaming.onError.Subscribe((x) =>
                {
                    try
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.StopStreaming(streamingInfo);

                            x = ExceptionHelper.GetReal(x);
                            string message = x.Message;

                            App.PrintService.Log($"Streaming_{streamingInfo.RTSPPath} [{streaming.processId}], {message}", Print.EMode.error, $"Streaming_{streamingInfo.RTSPPath.Replace("/", "_")}");

                            MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);

                        }));
                    }
                    catch (TaskCanceledException ex)
                    {
                    }
                });

                if (App.Streamings != null)
                {
                    App.Streamings.Add(streaming);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void StopStreaming(DataCenter.StreamingInfo.IStreamingInfo streamingInfo)
        {
            try
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (var item in DataCenter.DataCenter.StreamingInfo.StreamingInfos)
                    {
                        if (item.Index != streamingInfo.Index)
                        {
                            continue;
                        }

                        item.IsStart = false;

                        if (App.Streamings != null)
                        {
                            App.Streamings[item.Index - 1].Disconnect();
                        }
                    }

                    this.Streaming.Items.Refresh();
                }));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
