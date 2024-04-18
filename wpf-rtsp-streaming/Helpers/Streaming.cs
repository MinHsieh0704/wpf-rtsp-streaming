using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;

namespace wpf_rtsp_streaming.Helpers
{
    public class Streaming : IDisposable
    {
        private Process process { get; set; }

        public int processId { get; private set; }

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
        public void Connect()
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
                this.process.StartInfo.Arguments = $"-re -stream_loop -1 -i \"{this.filePath}\" -c copy -rtsp_transport tcp -f rtsp rtsp://127.0.0.1:8554/{this.rtspPath}";

                this.process.EnableRaisingEvents = true;

                this.process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        this.onMessage.OnNext(e.Data);
                    }
                };
                this.process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        this.onMessage.OnNext(e.Data);
                    }
                };

                this.process.Start();
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

                    if (!this.process.HasExited) this.process.Kill();
                    this.process.Close();
                    this.process.Dispose();

                    this.process = null;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
