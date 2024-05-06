using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using wpf_rtsp_streaming.Helpers;

namespace wpf_rtsp_streaming.DataCenter
{
    public partial class StreamingInfo : DependencyObject
    {
        public class IStreamingInfo
        {
            [JsonIgnore]
            public int Index { get; set; }
            public string FilePath { get; set; }
            public string RTSPPath { get; set; }
            [JsonIgnore]
            public string URL { get; set; }
            [JsonIgnore]
            public bool IsStart { get; set; }
        }

        public ObservableCollection<IStreamingInfo> StreamingInfos
        {
            get { return (ObservableCollection<IStreamingInfo>)GetValue(StreamingInfosProperty); }
            set { SetValue(StreamingInfosProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StreamingInfosProperty =
            DependencyProperty.Register("StreamingInfosProperty", typeof(ObservableCollection<IStreamingInfo>), typeof(StreamingInfo), new PropertyMetadata(new ObservableCollection<IStreamingInfo>()));

        public string FilePath { get; set; }

        public IPAddress address { get; set; }

        /// <summary>
        /// Read
        /// </summary>
        public void Read()
        {
            try
            {
                if (!File.Exists(this.FilePath))
                {
                    return;
                }

                foreach (NetworkInterface network in NetworkInterface.GetAllNetworkInterfaces())
                {
                    IPInterfaceProperties interfaceProperties = network.GetIPProperties();
                    if (interfaceProperties.GatewayAddresses.Count == 0)
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation addressInformation in interfaceProperties.UnicastAddresses)
                    {
                        IPAddress _address = addressInformation.Address;
                        if (_address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            continue;
                        }

                        this.address = _address;
                    }
                }

                using (FileStream fileStream = new FileStream(App.StreamingFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        string streamingString = reader.ReadToEnd();
                        if (!string.IsNullOrEmpty(streamingString))
                        {
                            List<IStreamingInfo> streamings = JsonConvert.DeserializeObject<List<IStreamingInfo>>(streamingString);
                            for (int i = 0; i < streamings.Count(); i++)
                            {
                                IStreamingInfo streaming = streamings[i];

                                this.StreamingInfos.Add(new IStreamingInfo()
                                {
                                    Index = i + 1,
                                    FilePath = streaming.FilePath,
                                    RTSPPath = streaming.RTSPPath,
                                    URL = $"rtsp://{(this.address == null ? "127.0.0.1" : this.address.ToString())}:{App.RTSPPort}/{streaming.RTSPPath}",
                                    IsStart = false,
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Write
        /// </summary>
        public void Write()
        {
            try
            {
                using (FileStream fileStream = new FileStream(App.StreamingFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        string streamingString = JsonConvert.SerializeObject(this.StreamingInfos, Formatting.Indented);

                        writer.Write(streamingString);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="info"></param>
        public void Add(IStreamingInfo info)
        {
            try
            {
                IStreamingInfo _info = this.StreamingInfos.Where((n) => n.RTSPPath == info.RTSPPath).FirstOrDefault();
                if (_info != null)
                {
                    throw new Exception($"RTSP Path \"{info.RTSPPath}\" is already used");
                }

                info.Index = this.StreamingInfos.Count() + 1;
                info.URL = $"rtsp://{(this.address == null ? "127.0.0.1" : this.address.ToString())}:{App.RTSPPort}/{info.RTSPPath}";

                this.StreamingInfos.Add(info);

                this.Write();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="info"></param>
        public void Delete(IStreamingInfo info)
        {
            try
            {
                IStreamingInfo _info = this.StreamingInfos.Where((n) => n.RTSPPath == info.RTSPPath).FirstOrDefault();
                this.StreamingInfos.Remove(_info);

                for (int i = 0; i < this.StreamingInfos.Count(); i++)
                {
                    IStreamingInfo streaming = this.StreamingInfos[i];

                    streaming.Index = i + 1;
                }

                this.Write();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
