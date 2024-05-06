using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace wpf_rtsp_streaming.Helpers
{
    public class FirewallRule
    {
        /// <summary>
        /// Search
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> Search(string name)
        {
            try
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.EnableRaisingEvents = true;

                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            if (Regex.IsMatch(message, "No rules match the specified criteria", RegexOptions.IgnoreCase))
                            {
                                taskCompletionSource.SetResult(true);
                            }
                            else if (Regex.IsMatch(message, "Rule Name: ", RegexOptions.IgnoreCase))
                            {
                                taskCompletionSource.SetResult(false);
                            }
                        }
                    };
                    process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            taskCompletionSource.SetException(new Exception(message));
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.StandardInput.WriteLine($"chcp 437");
                    process.StandardInput.WriteLine($"netsh.exe advfirewall firewall show rule name=\"{name}\"");

                    bool result = await taskCompletionSource.Task;

                    process.CancelOutputRead();
                    process.CancelErrorRead();

                    if (!process.HasExited) process.Kill();

                    return result;
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
        /// <param name="name"></param>
        /// <param name="port"></param>
        public async Task Add(string name, int port)
        {
            try
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.EnableRaisingEvents = true;

                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            if (Regex.IsMatch(message, "Ok", RegexOptions.IgnoreCase))
                            {
                                taskCompletionSource.SetResult(true);
                            }
                        }
                    };
                    process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        string message = e.Data;
                        if (!string.IsNullOrEmpty(message))
                        {
                            taskCompletionSource.SetException(new Exception(message));
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.StandardInput.WriteLine($"chcp 437");
                    process.StandardInput.WriteLine($"netsh advfirewall firewall add rule name=\"{name}\" dir=\"in\" action=\"allow\" protocol=\"TCP\" localport=\"{port}\"");

                    await taskCompletionSource.Task;

                    process.CancelOutputRead();
                    process.CancelErrorRead();

                    if (!process.HasExited) process.Kill();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
