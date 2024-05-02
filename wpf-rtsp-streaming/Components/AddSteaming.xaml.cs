using Microsoft.Win32;
using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace wpf_rtsp_streaming.Components
{
    /// <summary>
    /// AddSteaming.xaml 的互動邏輯
    /// </summary>
    public partial class AddSteaming : UserControl
    {
        public string FilePath
        {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FilePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register("FilePath", typeof(string), typeof(AddSteaming), new PropertyMetadata(null));

        public string RTSPPath
        {
            get { return (string)GetValue(RTSPPathProperty); }
            set { SetValue(RTSPPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RTSPPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RTSPPathProperty =
            DependencyProperty.Register("RTSPPath", typeof(string), typeof(AddSteaming), new PropertyMetadata(null));

        public bool IsSaveEnabled
        {
            get { return (bool)GetValue(IsSaveEnabledProperty); }
            set { SetValue(IsSaveEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSaveEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSaveEnabledProperty =
            DependencyProperty.Register("IsSaveEnabled", typeof(bool), typeof(AddSteaming), new PropertyMetadata(false));

        public class StreamingEventArgs : EventArgs
        {
            public string FilePath { get; set; }
            public string RTSPPath { get; set; }

            public StreamingEventArgs(string filePath, string rTSPPath)
            {
                FilePath = filePath;
                RTSPPath = rTSPPath;
            }
        }

        public event EventHandler<StreamingEventArgs> Save;
        public event EventHandler Cancel;

        public AddSteaming()
        {
            InitializeComponent();
        }

        private void ButtonSelectFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Select Video File";

                bool? dialogResult = dialog.ShowDialog();
                if (dialogResult != true)
                {
                    return;
                }

                string filePath = dialog.FileName;

                this.FilePath = filePath;
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                string message = ex.Message;

                App.PrintService.Log($"{this.GetType().Name}, {message}", Print.EMode.error);

                MessageBox.Show(message, App.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(this.FilePath))
                {
                    return;
                }
                if (string.IsNullOrEmpty(this.RTSPPath))
                {
                    return;
                }

                if (this.Save != null)
                {
                    this.Save(this, new StreamingEventArgs(this.FilePath, this.RTSPPath));

                    this.ButtonCancel_Click(sender, e);
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

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.Cancel != null)
                {
                    this.Cancel(this, EventArgs.Empty);

                    this.FilePath = null;
                    this.RTSPPath = null;
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

        private void TextBoxFilePath_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                TextBox element = sender as TextBox;
                if (element == null)
                {
                    return;
                }

                this.IsSaveEnabled = !string.IsNullOrEmpty(element.Text) && !string.IsNullOrEmpty(this.RTSPPath);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void TextBoxRTSPPath_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                TextBox element = sender as TextBox;
                if (element == null)
                {
                    return;
                }

                if (element.Text.Length == 1)
                {
                    if (Regex.IsMatch(element.Text, "^/"))
                    {
                        element.Text = "";
                    }
                }

                this.IsSaveEnabled = !string.IsNullOrEmpty(element.Text) && !string.IsNullOrEmpty(this.FilePath);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
