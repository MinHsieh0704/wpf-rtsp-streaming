using System;
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
        private Process process { get; set; }

        public int processId { get; private set; } = -1;

        public Subject<string> onMessage { get; } = new Subject<string>();

        public Subject<Exception> onError { get; } = new Subject<Exception>();

        public string filePath { get; set; }
        public string rtspPath { get; set; }

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
                if (filePath.IndexOf("https://www.youtube.com") > -1)
                {
                    await this.ConnectWithYoutube();
                }
                else
                {
                    this.ConnectWithFile();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Connect With File
        /// </summary>
        private void ConnectWithFile()
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
                this.process.StartInfo.Arguments = $"-re -stream_loop -1 -i \"{this.filePath}\" -c copy -rtsp_transport tcp -f rtsp rtsp://127.0.0.1:{App.RTSPPort}/{this.rtspPath}";

                this.process.EnableRaisingEvents = true;

                this.process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    string message = e.Data;
                    if (!string.IsNullOrEmpty(message))
                    {
                        this.onMessage.OnNext(message);
                    }
                };
                this.process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string message = e.Data;
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
