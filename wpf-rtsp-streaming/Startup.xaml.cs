using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace wpf_rtsp_streaming
{
    /// <summary>
    /// Startup.xaml 的互動邏輯
    /// </summary>
    public partial class Startup : Window
    {
        public Startup()
        {
            try
            {
                InitializeComponent();
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

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                Task.Delay(1000).Wait();

                Process currentProcess = Process.GetCurrentProcess();
                List<Process> processes = Process.GetProcesses().Where((n) => n.ProcessName == currentProcess.ProcessName).Where((n) => n.Id != currentProcess.Id).ToList();
                if (processes.Count() > 0)
                {
                    App.PrintService.Log("App, Opened Repeatedly", Print.EMode.warning);

                    App.Current.Shutdown();
                    return;
                }

                App.DeletePID();

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                this.Close();
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
