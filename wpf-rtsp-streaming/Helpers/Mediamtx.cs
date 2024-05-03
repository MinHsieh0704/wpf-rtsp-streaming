using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

namespace wpf_rtsp_streaming.Helpers
{
    public class Mediamtx : IDisposable
    {
        private Process process { get; set; }

        public int processId { get; private set; } = -1;

        public Subject<string> onMessage { get; } = new Subject<string>();

        public Subject<Exception> onError { get; } = new Subject<Exception>();

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

        ~Mediamtx()
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
                string file = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx\\mediamtx.exe";
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
                this.process.StartInfo.WorkingDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}mediamtx";

                this.process.EnableRaisingEvents = true;

                this.process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string message = Regex.Replace(e.Data, "^[0-9]{4}/[0-9]{2}/[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2} ", "");
                        if (Regex.IsMatch(message, "^ERR"))
                        {
                            this.onError.OnNext(new Exception(message));
                            return;
                        }

                        this.onMessage.OnNext(message);
                    }
                };
                this.process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        this.onError.OnNext(new Exception(e.Data));
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
