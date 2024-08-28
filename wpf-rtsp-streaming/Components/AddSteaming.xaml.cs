using Microsoft.Win32;
using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using wpf_rtsp_streaming.Helpers;
using static wpf_rtsp_streaming.Components.AddSteaming;

namespace wpf_rtsp_streaming.Components
{
    /// <summary>
    /// AddSteaming.xaml 的互動邏輯
    /// </summary>
    public partial class AddSteaming : UserControl
    {
        public class IDevice
        {
            public string key { get; set; }
            public string value { get; set; }
        }

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

        public string DeviceAlternativeName
        {
            get { return (string)GetValue(DeviceAlternativeNameProperty); }
            set { SetValue(DeviceAlternativeNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DeviceAlternativeName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DeviceAlternativeNameProperty =
            DependencyProperty.Register("DeviceAlternativeName", typeof(string), typeof(AddSteaming), new PropertyMetadata(null));

        public ObservableCollection<IDevice> Devices
        {
            get { return (ObservableCollection<IDevice>)GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Devices.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DevicesProperty =
            DependencyProperty.Register("Devices", typeof(ObservableCollection<IDevice>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<IDevice>()));

        public IDevice CurrDevice
        {
            get { return (IDevice)GetValue(CurrDeviceProperty); }
            set { SetValue(CurrDeviceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrLanguage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrDeviceProperty =
            DependencyProperty.Register("CurrDevice", typeof(IDevice), typeof(MainWindow), new PropertyMetadata(null));

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
            public string DeviceAlternativeName { get; set; }

            public StreamingEventArgs(string filePath, string rTSPPath, string deviceAlternativeName)
            {
                FilePath = filePath;
                RTSPPath = rTSPPath;
                DeviceAlternativeName = deviceAlternativeName;
            }
        }

        public event EventHandler<StreamingEventArgs> Save;
        public event EventHandler Cancel;

        public AddSteaming()
        {
            InitializeComponent();
        }

        private async void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (!Convert.ToBoolean(e.NewValue))
                {
                    return;
                }

                this.Devices.Clear();

                Streaming streaming = new Streaming();

                var devices = await streaming.GetDeviceList();
                foreach (var device in devices)
                {
                    this.Devices.Add(new IDevice() { key = device.AlternativeName, value = device.Name });
                }
            }
            catch (Exception ex)
            {
                throw;
            }
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
                this.DeviceAlternativeName = null;
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
                    this.Save(this, new StreamingEventArgs(this.FilePath, this.RTSPPath, this.DeviceAlternativeName));

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
                    this.DeviceAlternativeName = null;
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

                this.DeviceAlternativeName = null;

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

                if (Regex.IsMatch(element.Text, "^/"))
                {
                    element.Text = Regex.Replace(element.Text, "^/+", "");
                }

                this.IsSaveEnabled = !string.IsNullOrEmpty(element.Text) && !string.IsNullOrEmpty(this.FilePath);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ComboBoxDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ComboBox element = sender as ComboBox;
                if (element == null)
                {
                    return;
                }

                IDevice selectedItem = element.SelectedItem as IDevice;
                if (selectedItem == null)
                {
                    return;
                }

                this.FilePath = selectedItem.value;
                this.DeviceAlternativeName = selectedItem.key;

                element.SelectedItem = null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
