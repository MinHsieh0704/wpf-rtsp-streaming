using Min_Helpers;
using Min_Helpers.LogHelper;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using wpf_rtsp_streaming.Helpers;

namespace wpf_rtsp_streaming
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static Print PrintService { get; set; } = null;

        public static Log LogService { get; set; } = null;

        public static readonly TimeSpan LogExpiredDate = TimeSpan.FromDays(3);
        public static readonly Subject<bool> LogExpiredStop = new Subject<bool>();

        public static string CommonPath { get; } = AppDomain.CurrentDomain.BaseDirectory;
        public static string AppName { get; } = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        public static Mediamtx Mediamtx { get; set; }
        public static List<Streaming> Streamings { get; set; }

        private static DirectoryInfo _logPath = new DirectoryInfo($"{App.CommonPath}\\logs");
        public static DirectoryInfo LogPath
        {
            get
            {
                _logPath.Refresh();

                if (!_logPath.Exists)
                {
                    _logPath.Create();
                }

                return _logPath;
            }
        }

        private static FileInfo _streamingFile = new FileInfo($"{App.CommonPath}\\streaming.json");
        public static FileInfo StreamingFile
        {
            get
            {
                _streamingFile.Refresh();

                return _streamingFile;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            try
            {
                bool isDebugMode = false;
                if (e.Args != null && e.Args.Length != 0)
                {
                    isDebugMode = e.Args[0].ToLower() == "debug";
                }

                LogService = new Log();
                LogService.LogPath = LogPath.FullName;

                PrintService = new Print(LogService);

                Process currentProcess = Process.GetCurrentProcess();
                List<Process> processes = Process.GetProcesses().Where((n) => n.ProcessName == currentProcess.ProcessName).Where((n) => n.Id != currentProcess.Id).ToList();
                if (processes.Count() > 0 && !isDebugMode)
                {
                    PrintService.Log("App was opened repeatedly", Print.EMode.warning, "startup");

                    App.Current.Shutdown();
                    return;
                }

                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;

                LogService.Write("");
                PrintService.Log("App, Start", Print.EMode.info);

                DataCenter.DataCenter.StreamingInfo.FilePath = StreamingFile.FullName;
                DataCenter.DataCenter.StreamingInfo.Read();

                App.StartLogExpiredService();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                PrintService.Log($"App, Error, {ex.Message}", Print.EMode.info);

                App.Current.Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (App.Streamings != null)
            {
                foreach (var streaming in App.Streamings)
                {
                    streaming.Dispose();
                }
            }
            if (App.Mediamtx != null)
            {
                App.Mediamtx.Dispose();
            }

            App.LogExpiredStop.OnNext(true);

            PrintService.Log("App, Exit", Print.EMode.info);

            base.OnExit(e);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = ExceptionHelper.GetReal(e.Exception);
            PrintService.Log($"Current_DispatcherUnhandledException: {ex.ToString()}", Print.EMode.warning);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = ExceptionHelper.GetReal(e.ExceptionObject as Exception);
            PrintService.Log($"CurrentDomain_UnhandledException: {ex.ToString()}", Print.EMode.warning);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var ex = ExceptionHelper.GetReal(e.Exception);
            PrintService.Log($"TaskScheduler_UnobservedTaskException: {ex.ToString()}", Print.EMode.warning);
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = ExceptionHelper.GetReal(e.Exception);
            PrintService.Log($"Dispatcher_UnhandledException: {ex.ToString()}", Print.EMode.warning);
        }

        private static void StartLogExpiredService()
        {
            try
            {
                Observable
                    .Interval(TimeSpan.FromHours(1))
                    .TakeUntil(App.LogExpiredStop)
                    .StartWith(0)
                    .Delay(TimeSpan.FromSeconds(0))
                    .ObserveOn(NewThreadScheduler.Default)
                    .Select((x) => Observable.FromAsync(async () =>
                    {
                        DateTime expiredDate = DateTime.Now - App.LogExpiredDate;

                        FileInfo[] files = App.LogPath.GetFiles();
                        foreach (var file in files)
                        {
                            try
                            {
                                if (DateTime.Compare(file.LastWriteTime, expiredDate) < 0)
                                {
                                    file.Delete();
                                }
                            }
                            catch (Exception ex)
                            {
                            }
                        }

                        return x;
                    }))
                    .Concat()
                    .Subscribe();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
