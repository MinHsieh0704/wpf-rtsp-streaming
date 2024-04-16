using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Diagnostics;
using System.Reflection;
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

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                this.Title = App.AppName;

                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

                this.AppVersion = $"v{fileInfo.FileVersion}";

                Task.Run(new Action(async () =>
                {
                    try
                    {
                        Mediamtx mediamtx = new Mediamtx();

                        mediamtx.onMessage.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                App.PrintService.Log($"Mediamtx [{mediamtx.processId}], {x}", Print.EMode.message);
                            }));
                        });
                        mediamtx.onError.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                x = ExceptionHelper.GetReal(x);
                                string message = x.Message;

                                App.PrintService.Log($"Mediamtx [{mediamtx.processId}], {message}", Print.EMode.error);
                            }));
                        });

                        mediamtx.Connect();

                        await Task.Delay(1000);

                        Streaming streaming1 = new Streaming();

                        string filePath1 = "C:\\\\Users\\\\min.hsieh\\\\Desktop\\\\RTSP Simple Server\\\\merge video\\\\cleanroom\\\\cleanroom.mp4";
                        string rtspPath1 = "cleanroom";

                        streaming1.onMessage.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                App.PrintService.Log($"Streaming_{rtspPath1} [{streaming1.processId}], {x}", Print.EMode.message);
                            }));
                        });
                        streaming1.onError.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                x = ExceptionHelper.GetReal(x);
                                string message = x.Message;

                                App.PrintService.Log($"Streaming_{rtspPath1} [{streaming1.processId}], {message}", Print.EMode.error);
                            }));
                        });

                        streaming1.Connect(filePath1, rtspPath1);

                        Streaming streaming2 = new Streaming();

                        string filePath2 = "C:\\\\Users\\\\min.hsieh\\\\Desktop\\\\RTSP Simple Server\\\\merge video\\\\workbench\\\\workbench.mp4";
                        string rtspPath2 = "workbench";

                        streaming2.onMessage.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                App.PrintService.Log($"Streaming_{rtspPath2} [{streaming2.processId}], {x}", Print.EMode.message);
                            }));
                        });
                        streaming2.onError.Subscribe((x) =>
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                x = ExceptionHelper.GetReal(x);
                                string message = x.Message;

                                App.PrintService.Log($"Streaming_{rtspPath2} [{streaming2.processId}], {message}", Print.EMode.error);
                            }));
                        });

                        streaming2.Connect(filePath2, rtspPath2);
                    }
                    catch (Exception ex)
                    {
                        ex = ExceptionHelper.GetReal(ex);
                        string message = ex.Message;

                        App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);
                    }
                }));
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
    }
}
