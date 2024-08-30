using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace wpf_rtsp_streaming.Helpers
{
    public class Streaming : IDisposable
    {
        public class IDevice
        {
            public string Name { get; set; }
            public string AlternativeName { get; set; }
        }

        public class IDeviceInfo
        {
            public int width { get; set; }
            public int height { get; set; }
            public int fps { get; set; }
        }

        private Process process { get; set; }

        public int processId { get; private set; } = -1;

        public Subject<string> onMessage { get; } = new Subject<string>();

        public Subject<Exception> onError { get; } = new Subject<Exception>();

        public Subject<string> onClose { get; } = new Subject<string>();

        public string filePath { get; set; }
        public string rtspPath { get; set; }
        public string deviceAlternativeName { get; set; }

        #region Dispose
        private bool disposed { get; set; } = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Disconnect();
                }

                this.onMessage.OnCompleted();
                this.onError.OnCompleted();
                this.onClose.OnCompleted();

                disposed = true;
            }
        }

        ~Streaming()
        {
            Dispose(false);
        }
        #endregion

        /// <summary>
        /// Connect
        /// </summary>
        public async Task Connect()
        {
            try
            {
                if (this.filePath.IndexOf("https://www.youtube.com") > -1)
                {
                    await this.ConnectWithYoutube();
                }
                else
                {
                    if (string.IsNullOrEmpty(this.deviceAlternativeName))
                    {
                        await this.ConnectWithFile();
                    }
                    else
                    {
                        await this.ConnectWithDevice();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Connect
        /// </summary>
        public async Task Download()
        {
            try
            {
                if (this.filePath.IndexOf("https://www.youtube.com") > -1)
                {
                    await this.DownloadWithYoutube();
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Get Device List
        /// </summary>
        public async Task<List<IDevice>> GetDeviceList()
        {
            try
            {
                string file = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx\\ffmpeg.exe";
                if (!File.Exists(file))
                {
                    throw new Exception($"Can not found {file}");
                }

                this.process = new Process();

                this.process.StartInfo.FileName = file;
                this.process.StartInfo.UseShellExecute = false;
                this.process.StartInfo.RedirectStandardOutput = true;
                this.process.StartInfo.RedirectStandardError = true;
                this.process.StartInfo.CreateNoWindow = true;
                this.process.StartInfo.Arguments = $"-list_devices true -f dshow -i dummy";

                this.process.EnableRaisingEvents = true;

                List<IDevice> devices = new List<IDevice>();

                this.process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    string message = e.Data;
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (Regex.IsMatch(message, "dshow ", RegexOptions.IgnoreCase))
                        {
                            if (Regex.IsMatch(message, "\\(video\\)", RegexOptions.IgnoreCase) && (devices.Count == 0 || !string.IsNullOrEmpty(devices.LastOrDefault().AlternativeName)))
                            {
                                string name = message;
                                name = new Regex("\\(video\\)", RegexOptions.IgnoreCase).Replace(name, "");
                                name = new Regex("^\\[.*\\]", RegexOptions.IgnoreCase).Replace(name, "");
                                name = new Regex("(\"|')", RegexOptions.IgnoreCase).Replace(name, "");
                                name = name.Trim();

                                devices.Add(new IDevice() { Name = name });
                            }
                            else if (string.IsNullOrEmpty(devices.LastOrDefault().AlternativeName))
                            {
                                string alternativeName = message;
                                alternativeName = new Regex("Alternative name", RegexOptions.IgnoreCase).Replace(alternativeName, "");
                                alternativeName = new Regex("^\\[.*\\]", RegexOptions.IgnoreCase).Replace(alternativeName, "");
                                alternativeName = new Regex("(\"|')", RegexOptions.IgnoreCase).Replace(alternativeName, "");
                                alternativeName = alternativeName.Trim();

                                devices.LastOrDefault().AlternativeName = alternativeName;
                            }
                        }
                    };
                };
                this.process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    string message = e.Data;
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (Regex.IsMatch(message, "dshow ", RegexOptions.IgnoreCase))
                        {
                            if (Regex.IsMatch(message, "\\(video\\)", RegexOptions.IgnoreCase) && (devices.Count == 0 || !string.IsNullOrEmpty(devices.LastOrDefault()?.AlternativeName)))
                            {
                                string name = message;
                                name = new Regex("\\(video\\)", RegexOptions.IgnoreCase).Replace(name, "");
                                name = new Regex("^\\[.*\\]", RegexOptions.IgnoreCase).Replace(name, "");
                                name = new Regex("(\"|')", RegexOptions.IgnoreCase).Replace(name, "");
                                name = name.Trim();

                                devices.Add(new IDevice() { Name = name });
                            }
                            else if (devices.Count != 0 && string.IsNullOrEmpty(devices.LastOrDefault()?.AlternativeName))
                            {
                                string alternativeName = message;
                                alternativeName = new Regex("Alternative name", RegexOptions.IgnoreCase).Replace(alternativeName, "");
                                alternativeName = new Regex("^\\[.*\\]", RegexOptions.IgnoreCase).Replace(alternativeName, "");
                                alternativeName = new Regex("(\"|')", RegexOptions.IgnoreCase).Replace(alternativeName, "");
                                alternativeName = alternativeName.Trim();

                                devices.LastOrDefault().AlternativeName = alternativeName;
                            }
                        }
                    };
                };

                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                Task.Run(new Action(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            if (this.process.HasExited)
                            {
                                taskCompletionSource.SetResult(true);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }));

                this.process.Start();
                App.WritePID(this.processId, process.Id);
                this.processId = this.process.Id;

                this.process.BeginOutputReadLine();
                this.process.BeginErrorReadLine();

                await taskCompletionSource.Task;

                return devices;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Connect With File
        /// </summary>
        private async Task ConnectWithFile()
        {
            try
            {
                string file = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx\\ffmpeg.exe";
                if (!File.Exists(file))
                {
                    throw new Exception($"Can not found {file}");
                }

                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                this.process = new Process();

                this.process.StartInfo.FileName = file;
                this.process.StartInfo.UseShellExecute = false;
                this.process.StartInfo.RedirectStandardOutput = true;
                this.process.StartInfo.RedirectStandardError = true;
                this.process.StartInfo.CreateNoWindow = true;
                this.process.StartInfo.Arguments = $"-re -stream_loop -1 -i \"{this.filePath}\" -c copy -rtsp_transport tcp -f rtsp rtsp://127.0.0.1:{App.RTSPPort}/{this.rtspPath}";

                this.process.EnableRaisingEvents = true;

                this.process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    string message = e.Data;
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                        {
                            taskCompletionSource.SetResult(true);
                        }

                        this.onMessage.OnNext(message);
                    }
                };
                this.process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    string message = e.Data;
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                        {
                            taskCompletionSource.SetResult(true);
                        }

                        if (Regex.IsMatch(message.ToLower(), "fail|error", RegexOptions.IgnoreCase))
                        {
                            this.onError.OnNext(new Exception(message));
                            return;
                        }

                        this.onMessage.OnNext(message);
                    }
                };

                this.process.Start();
                App.WritePID(this.processId, process.Id);
                this.processId = this.process.Id;

                this.process.BeginOutputReadLine();
                this.process.BeginErrorReadLine();

                await taskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Connect With Device
        /// </summary>
        private async Task ConnectWithDevice()
        {
            try
            {
                string file = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx\\ffmpeg.exe";
                if (!File.Exists(file))
                {
                    throw new Exception($"Can not found {file}");
                }

                List<IDeviceInfo> deviceInfos = new List<IDeviceInfo>();

                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                for (int i = 0; i < 2; i++)
                {
                    TaskCompletionSource<bool> formatCheckTaskCompletionSource = new TaskCompletionSource<bool>();

                    this.process = new Process();

                    this.process.StartInfo.FileName = "cmd.exe";
                    this.process.StartInfo.UseShellExecute = false;
                    this.process.StartInfo.RedirectStandardOutput = true;
                    this.process.StartInfo.RedirectStandardError = true;
                    this.process.StartInfo.RedirectStandardInput = true;
                    this.process.StartInfo.CreateNoWindow = true;
                    this.process.StartInfo.WorkingDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx";

                    this.process.EnableRaisingEvents = true;

                    this.process.OutputDataReceived += async (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            this.onMessage.OnNext(message);

                            if (i == 0)
                            {
                                if (Regex.IsMatch(message, "^\\[dshow.*\\] +vcodec", RegexOptions.IgnoreCase) && !Regex.IsMatch(message, "\\)$", RegexOptions.IgnoreCase))
                                {
                                    string info = message;
                                    info = info.Substring(info.IndexOf("max"));
                                    info = new Regex("max *", RegexOptions.IgnoreCase).Replace(info, "");

                                    List<string> infos = info.Split(' ').ToList();
                                    string size = new Regex("s=", RegexOptions.IgnoreCase).Replace(infos[0], "").Trim();
                                    int fps = Convert.ToInt32(new Regex("fps=", RegexOptions.IgnoreCase).Replace(infos[1], "").Trim());

                                    List<string> sizes = size.Split('x').ToList();
                                    int width = Convert.ToInt32(sizes[0]);
                                    int height = Convert.ToInt32(sizes[1]);

                                    deviceInfos.Add(new IDeviceInfo()
                                    {
                                        width = width,
                                        height = height,
                                        fps = fps,
                                    });
                                }
                            }
                            else
                            {
                                if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                {
                                    taskCompletionSource.SetResult(true);
                                }
                            }
                        }
                    };
                    this.process.ErrorDataReceived += async (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            if (Regex.IsMatch(message.ToLower(), "fail|error", RegexOptions.IgnoreCase))
                            {
                                if (i == 0)
                                {
                                    if (Regex.IsMatch(message, "Error opening input", RegexOptions.IgnoreCase))
                                    {
                                        if (!formatCheckTaskCompletionSource.Task.IsCompleted && !formatCheckTaskCompletionSource.Task.IsCanceled)
                                        {
                                            formatCheckTaskCompletionSource.SetResult(true);
                                        }

                                        return;
                                    }
                                }

                                this.onError.OnNext(new Exception(message));
                                return;
                            }

                            this.onMessage.OnNext(message);

                            if (i == 0)
                            {
                                if (Regex.IsMatch(message, "^\\[dshow.*\\] +vcodec", RegexOptions.IgnoreCase) && !Regex.IsMatch(message, "\\)$", RegexOptions.IgnoreCase))
                                {
                                    string info = message;
                                    info = info.Substring(info.IndexOf("max"));
                                    info = new Regex("max *", RegexOptions.IgnoreCase).Replace(info, "");

                                    List<string> infos = info.Split(' ').ToList();
                                    string size = new Regex("s=", RegexOptions.IgnoreCase).Replace(infos[0], "").Trim();
                                    int fps = Convert.ToInt32(new Regex("fps=", RegexOptions.IgnoreCase).Replace(infos[1], "").Trim());

                                    List<string> sizes = size.Split('x').ToList();
                                    int width = Convert.ToInt32(sizes[0]);
                                    int height = Convert.ToInt32(sizes[1]);

                                    deviceInfos.Add(new IDeviceInfo()
                                    {
                                        width = width,
                                        height = height,
                                        fps = fps,
                                    });
                                }
                            }
                            else
                            {
                                if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                {
                                    taskCompletionSource.SetResult(true);
                                }
                            }
                        }
                    };

                    this.process.Start();
                    App.WritePID(this.processId, process.Id);
                    this.processId = this.process.Id;

                    this.process.BeginOutputReadLine();
                    this.process.BeginErrorReadLine();

                    if (i == 0)
                    {
                        this.process.StandardInput.WriteLine($"ffmpeg.exe -list_options true -f dshow -i video=\"{this.deviceAlternativeName}\"");
                        App.PrintService.Log($"1, main: {this.process.ProcessName}[{this.process.Id}], child: {string.Join("; ", Community.GetChildProcess(this.processId).Select((n) => $"{n.ProcessName}[{n.Id}]").ToArray())}", Min_Helpers.PrintHelper.Print.EMode.info);

                        await formatCheckTaskCompletionSource.Task;
                    }
                    else
                    {
                        deviceInfos = deviceInfos.GroupBy((n) => n.fps, (n) => n).FirstOrDefault()?.OrderByDescending((n) => n.width * n.height).ToList();

                        IDeviceInfo deviceInfo = deviceInfos?.FirstOrDefault();
                        if (deviceInfo == null)
                        {
                            throw new Exception("Device setting not found.");
                        }

                        this.process.StandardInput.WriteLine($"ffmpeg.exe -f dshow -video_size {deviceInfo.width}x{deviceInfo.height} -framerate {deviceInfo.fps} -i video=\"{this.deviceAlternativeName}\" -c:v libx264 -preset:v ultrafast -tune:v zerolatency -rtsp_transport tcp -pix_fmt yuvj420p -f rtsp rtsp://127.0.0.1:{App.RTSPPort}/{this.rtspPath}");
                        App.PrintService.Log($"2, main: {this.process.ProcessName}[{this.process.Id}], child: {string.Join("; ", Community.GetChildProcess(this.processId).Select((n) => $"{n.ProcessName}[{n.Id}]").ToArray())}", Min_Helpers.PrintHelper.Print.EMode.info);
                    }
                }

                await taskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Connect With Youtube
        /// </summary>
        private async Task ConnectWithYoutube()
        {
            try
            {
                string file = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx\\yt-dlp.exe";
                if (!File.Exists(file))
                {
                    throw new Exception($"Can not found {file}");
                }

                file = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx\\ffmpeg.exe";
                if (!File.Exists(file))
                {
                    throw new Exception($"Can not found {file}");
                }

                Uri uri = new Uri(this.filePath);
                NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);

                string url = null;
                foreach (var key in query)
                {
                    if (key.ToString() != "v")
                    {
                        continue;
                    }

                    url = $"{uri.AbsoluteUri.Replace(uri.Query, "")}?{key}={query[key.ToString()]}";
                    break;
                }
                if (url == null)
                {
                    throw new Exception("Youtube url lose some query parameter");
                }

                bool isDownloadCompleted = false;

                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                for (int i = 0; i < 2; i++)
                {
                    TaskCompletionSource<bool> formatCheckTaskCompletionSource = new TaskCompletionSource<bool>();

                    this.process = new Process();

                    this.process.StartInfo.FileName = "cmd.exe";
                    this.process.StartInfo.UseShellExecute = false;
                    this.process.StartInfo.RedirectStandardOutput = true;
                    this.process.StartInfo.RedirectStandardError = true;
                    this.process.StartInfo.RedirectStandardInput = true;
                    this.process.StartInfo.CreateNoWindow = true;
                    this.process.StartInfo.WorkingDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx";

                    this.process.EnableRaisingEvents = true;

                    this.process.OutputDataReceived += async (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            this.onMessage.OnNext(message);

                            if (i == 0)
                            {
                                if (Regex.IsMatch(message, "Available formats for ", RegexOptions.IgnoreCase))
                                {
                                    await Task.Delay(500);

                                    this.Disconnect();

                                    if (!formatCheckTaskCompletionSource.Task.IsCompleted && !formatCheckTaskCompletionSource.Task.IsCanceled)
                                    {
                                        formatCheckTaskCompletionSource.SetResult(true);
                                    }
                                }
                            }
                            else
                            {
                                if (Regex.IsMatch(message, "Extracting URL: ", RegexOptions.IgnoreCase))
                                {
                                    if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                    {
                                        taskCompletionSource.SetResult(true);
                                    }
                                }
                                else if (Regex.IsMatch(message, "\\[download\\] 100%", RegexOptions.IgnoreCase))
                                {
                                    isDownloadCompleted = true;
                                }
                            }
                        }
                    };
                    this.process.ErrorDataReceived += async (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            if (Regex.IsMatch(message, "fail|error", RegexOptions.IgnoreCase))
                            {
                                if (isDownloadCompleted && Regex.IsMatch(message, "Error during demuxing: Function not implemented", RegexOptions.IgnoreCase))
                                {
                                    this.onMessage.OnNext(message);

                                    this.Disconnect();
                                    await this.Connect();
                                    return;
                                }

                                if (Regex.IsMatch(message, "\\[download\\] Got error: ", RegexOptions.IgnoreCase))
                                {
                                    this.onMessage.OnNext(message);

                                    this.Disconnect();
                                    await this.Connect();
                                    return;
                                }

                                if (Regex.IsMatch(message, "submitting a packet to the muxer: Broken pipe", RegexOptions.IgnoreCase))
                                {
                                    this.onMessage.OnNext(message);

                                    this.Disconnect();
                                    await this.Connect();
                                    return;
                                }

                                if (Regex.IsMatch(message, "Did not get any data blocks", RegexOptions.IgnoreCase))
                                {
                                    this.onMessage.OnNext(message);

                                    this.Disconnect();
                                    await this.Connect();
                                    return;
                                }

                                if (Regex.IsMatch(message, "nsig extraction failed: ", RegexOptions.IgnoreCase))
                                {
                                    this.onMessage.OnNext(message);

                                    return;
                                }

                                if (!formatCheckTaskCompletionSource.Task.IsCompleted && !formatCheckTaskCompletionSource.Task.IsCanceled)
                                {
                                    formatCheckTaskCompletionSource.SetCanceled();
                                }
                                if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                {
                                    taskCompletionSource.SetResult(true);
                                }

                                this.onError.OnNext(new Exception(message));
                                return;
                            }

                            this.onMessage.OnNext(message);

                            if (i == 0)
                            {
                                if (Regex.IsMatch(message, "Available formats for ", RegexOptions.IgnoreCase))
                                {
                                    await Task.Delay(500);

                                    this.Disconnect();

                                    if (!formatCheckTaskCompletionSource.Task.IsCompleted && !formatCheckTaskCompletionSource.Task.IsCanceled)
                                    {
                                        formatCheckTaskCompletionSource.SetResult(true);
                                    }
                                }
                            }
                            else
                            {
                                if (Regex.IsMatch(message, "Extracting URL: ", RegexOptions.IgnoreCase))
                                {
                                    if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                    {
                                        taskCompletionSource.SetResult(true);
                                    }
                                }
                                else if (Regex.IsMatch(message, "\\[download\\] 100%", RegexOptions.IgnoreCase))
                                {
                                    isDownloadCompleted = true;
                                }
                            }
                        }
                    };

                    this.process.Start();
                    App.WritePID(this.processId, process.Id);
                    this.processId = this.process.Id;

                    this.process.BeginOutputReadLine();
                    this.process.BeginErrorReadLine();

                    if (i == 0)
                    {
                        this.process.StandardInput.WriteLine($"yt-dlp.exe --no-playlist -F \"{url}\"");
                        App.PrintService.Log($"1, main: {this.process.ProcessName}[{this.process.Id}], child: {string.Join("; ", Community.GetChildProcess(this.processId).Select((n) => $"{n.ProcessName}[{n.Id}]").ToArray())}", Min_Helpers.PrintHelper.Print.EMode.info);

                        await formatCheckTaskCompletionSource.Task;
                    }
                    else
                    {
                        this.process.StandardInput.WriteLine($"yt-dlp.exe -f \"(bv*[vcodec~='^((he|a)vc|h26[45])'])\" --live-from-start --no-playlist -o - \"{url}\" | ffmpeg.exe -re -stream_loop -1 -i pipe: -c copy -rtsp_transport tcp -f rtsp rtsp://127.0.0.1:{App.RTSPPort}/{this.rtspPath}");
                        App.PrintService.Log($"2, main: {this.process.ProcessName}[{this.process.Id}], child: {string.Join("; ", Community.GetChildProcess(this.processId).Select((n) => $"{n.ProcessName}[{n.Id}]").ToArray())}", Min_Helpers.PrintHelper.Print.EMode.info);
                    }
                }

                await taskCompletionSource.Task;
            }
            catch (OperationCanceledException e)
            {
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Download With Youtube
        /// </summary>
        private async Task DownloadWithYoutube()
        {
            try
            {
                string file = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx\\yt-dlp.exe";
                if (!File.Exists(file))
                {
                    throw new Exception($"Can not found {file}");
                }

                Uri uri = new Uri(this.filePath);
                NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);

                string url = null;
                foreach (var key in query)
                {
                    if (key.ToString() != "v")
                    {
                        continue;
                    }

                    url = $"{uri.AbsoluteUri.Replace(uri.Query, "")}?{key}={query[key.ToString()]}";
                    break;
                }
                if (url == null)
                {
                    throw new Exception("Youtube url lose some query parameter");
                }

                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                for (int i = 0; i < 2; i++)
                {
                    TaskCompletionSource<bool> formatCheckTaskCompletionSource = new TaskCompletionSource<bool>();

                    this.process = new Process();

                    this.process.StartInfo.FileName = "cmd.exe";
                    this.process.StartInfo.UseShellExecute = false;
                    this.process.StartInfo.RedirectStandardOutput = true;
                    this.process.StartInfo.RedirectStandardError = true;
                    this.process.StartInfo.RedirectStandardInput = true;
                    this.process.StartInfo.CreateNoWindow = true;
                    this.process.StartInfo.WorkingDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx";

                    this.process.EnableRaisingEvents = true;

                    this.process.OutputDataReceived += async (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            this.onMessage.OnNext(message);

                            if (i == 0)
                            {
                                if (Regex.IsMatch(message, "Available formats for ", RegexOptions.IgnoreCase))
                                {
                                    await Task.Delay(500);

                                    this.Disconnect();

                                    if (!formatCheckTaskCompletionSource.Task.IsCompleted && !formatCheckTaskCompletionSource.Task.IsCanceled)
                                    {
                                        formatCheckTaskCompletionSource.SetResult(true);
                                    }
                                }
                            }
                            else
                            {
                                if (Regex.IsMatch(message, "Extracting URL: ", RegexOptions.IgnoreCase))
                                {
                                    if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                    {
                                        taskCompletionSource.SetResult(true);
                                    }
                                }
                                else if (Regex.IsMatch(message, "\\[download\\] 100%", RegexOptions.IgnoreCase))
                                {
                                    this.onClose.OnNext($"Download Youtube {url} Done");
                                }
                            }
                        }
                    };
                    this.process.ErrorDataReceived += async (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            if (Regex.IsMatch(message, "fail|error", RegexOptions.IgnoreCase))
                            {
                                if (Regex.IsMatch(message, "\\[download\\] Got error: ", RegexOptions.IgnoreCase))
                                {
                                    this.onMessage.OnNext(message);

                                    this.Disconnect();
                                    return;
                                }

                                if (Regex.IsMatch(message, "nsig extraction failed: ", RegexOptions.IgnoreCase))
                                {
                                    this.onMessage.OnNext(message);

                                    return;
                                }

                                if (!formatCheckTaskCompletionSource.Task.IsCompleted && !formatCheckTaskCompletionSource.Task.IsCanceled)
                                {
                                    formatCheckTaskCompletionSource.SetCanceled();
                                }
                                if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                {
                                    taskCompletionSource.SetResult(true);
                                }

                                this.onError.OnNext(new Exception(message));
                                return;
                            }

                            this.onMessage.OnNext(message);

                            if (i == 0)
                            {
                                if (Regex.IsMatch(message, "Available formats for ", RegexOptions.IgnoreCase))
                                {
                                    await Task.Delay(500);

                                    this.Disconnect();

                                    if (!formatCheckTaskCompletionSource.Task.IsCompleted && !formatCheckTaskCompletionSource.Task.IsCanceled)
                                    {
                                        formatCheckTaskCompletionSource.SetResult(true);
                                    }
                                }
                            }
                            else
                            {
                                if (Regex.IsMatch(message, "Extracting URL: ", RegexOptions.IgnoreCase))
                                {
                                    if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                                    {
                                        taskCompletionSource.SetResult(true);
                                    }
                                }
                                else if (Regex.IsMatch(message, "\\[download\\] 100%", RegexOptions.IgnoreCase))
                                {
                                    this.onClose.OnNext($"Download Youtube {url} Done");
                                }
                            }
                        }
                    };

                    this.process.Start();
                    App.WritePID(this.processId, process.Id);
                    this.processId = this.process.Id;

                    this.process.BeginOutputReadLine();
                    this.process.BeginErrorReadLine();

                    if (i == 0)
                    {
                        this.process.StandardInput.WriteLine($"yt-dlp.exe --no-playlist -F \"{url}\"");
                        App.PrintService.Log($"1, main: {this.process.ProcessName}[{this.process.Id}], child: {string.Join("; ", Community.GetChildProcess(this.processId).Select((n) => $"{n.ProcessName}[{n.Id}]").ToArray())}", Min_Helpers.PrintHelper.Print.EMode.info);

                        await formatCheckTaskCompletionSource.Task;
                    }
                    else
                    {
                        this.process.StandardInput.WriteLine($"yt-dlp.exe -f \"(bv*[vcodec~='^((he|a)vc|h26[45])'])\" --live-from-start --no-playlist -o \"{AppDomain.CurrentDomain.BaseDirectory}Downloads\\%(title)s.%(ext)s\" \"{url}\"");
                        App.PrintService.Log($"2, main: {this.process.ProcessName}[{this.process.Id}], child: {string.Join("; ", Community.GetChildProcess(this.processId).Select((n) => $"{n.ProcessName}[{n.Id}]").ToArray())}", Min_Helpers.PrintHelper.Print.EMode.info);
                    }
                }

                await taskCompletionSource.Task;
            }
            catch (OperationCanceledException e)
            {
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (this.process != null)
                {
                    this.process.CancelOutputRead();
                    this.process.CancelErrorRead();

                    Community.KillChildProcess(this.process.Id);

                    if (!this.process.HasExited)
                    {
                        process.Kill();
                        App.WritePID(this.processId, -1);
                    }
                    this.process.Close();
                    this.process.Dispose();

                    this.process = null;
                    this.processId = -1;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
