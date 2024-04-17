using System.Windows;

namespace wpf_rtsp_streaming.DataCenter
{
    public partial class DataCenter : DependencyObject
    {
        public static StreamingInfo StreamingInfo { get; set; } = new StreamingInfo();
    }
}
