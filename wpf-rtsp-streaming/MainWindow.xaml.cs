using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                this.Title = App.AppName;

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

                this.AppVersion = $"v{fileInfo.FileVersion}";

                //Task.Run(new Action(async () =>
                //{
                //    try
                //    {
                //        Streaming streaming1 = new Streaming();

                //        string filePath1 = "C:\\\\Users\\\\min.hsieh\\\\Desktop\\\\RTSP Simple Server\\\\merge video\\\\cleanroom\\\\cleanroom.mp4";
                //        string rtspPath1 = "cleanroom";

                //        streaming1.onMessage.Subscribe((x) =>
                //        {
                //            this.Dispatcher.Invoke(new Action(() =>
                //            {
                //                App.PrintService.Log($"Streaming_{rtspPath1} [{streaming1.processId}], {x}", Print.EMode.message);
                //            }));
                //        });
                //        streaming1.onError.Subscribe((x) =>
                //        {
                //            this.Dispatcher.Invoke(new Action(() =>
                //            {
                //                x = ExceptionHelper.GetReal(x);
                //                string message = x.Message;

                //                App.PrintService.Log($"Streaming_{rtspPath1} [{streaming1.processId}], {message}", Print.EMode.error);
                //            }));
                //        });

                //        streaming1.Connect(filePath1, rtspPath1);

                //        Streaming streaming2 = new Streaming();

                //        string filePath2 = "C:\\\\Users\\\\min.hsieh\\\\Desktop\\\\RTSP Simple Server\\\\merge video\\\\workbench\\\\workbench.mp4";
                //        string rtspPath2 = "workbench";

                //        streaming2.onMessage.Subscribe((x) =>
                //        {
                //            this.Dispatcher.Invoke(new Action(() =>
                //            {
                //                App.PrintService.Log($"Streaming_{rtspPath2} [{streaming2.processId}], {x}", Print.EMode.message);
                //            }));
                //        });
                //        streaming2.onError.Subscribe((x) =>
                //        {
                //            this.Dispatcher.Invoke(new Action(() =>
                //            {
                //                x = ExceptionHelper.GetReal(x);
                //                string message = x.Message;

                //                App.PrintService.Log($"Streaming_{rtspPath2} [{streaming2.processId}], {message}", Print.EMode.error);
                //            }));
                //        });

                //        streaming2.Connect(filePath2, rtspPath2);
                //    }
                //    catch (Exception ex)
                //    {
                //        ex = ExceptionHelper.GetReal(ex);
                //        string message = ex.Message;

                //        App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);
                //    }
                //}));
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

                await Task.Run(new Action(() =>
                {
                    try
                    {
                        App.Mediamtx = new Mediamtx();

                        App.Mediamtx.onMessage.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                App.PrintService.Log($"Mediamtx [{App.Mediamtx.processId}], {x}", Print.EMode.message);
                            }));
                        });
                        App.Mediamtx.onError.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                App.Mediamtx.Dispose();

                                this.IsStart = false;

                                x = ExceptionHelper.GetReal(x);
                                string message = x.Message;

                                App.PrintService.Log($"Mediamtx [{App.Mediamtx.processId}], {message}", Print.EMode.error);

                                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
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

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Mediamtx.Dispose();

                this.IsStart = false;
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonAddStreaming_Click(object sender, RoutedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
