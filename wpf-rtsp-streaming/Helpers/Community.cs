using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace wpf_rtsp_streaming.Helpers
{
    public class Community
    {
        public class IChildProcess
        {
            public int Id { get; set; }
            public string ProcessName { get; set; }
        }

        /// <summary>
        /// Get Child Process
        /// </summary>
        /// <param name="processId"></param>
        /// <returns>List<IChildProcess></returns>
        public static List<IChildProcess> GetChildProcess(int processId)
        {
            try
            {
                List<IChildProcess> childs = new List<IChildProcess>();
                List<string> allowProcessName = new List<string>() { "mediamtx", "cmd", "conhost", "yt-dlp", "ffmpeg" };

                ManagementObjectSearcher managementSearcher = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", processId));
                if (managementSearcher.Get().Count == 0)
                {
                    return new List<IChildProcess>();
                }
                foreach (ManagementObject managementObject in managementSearcher.Get())
                {
                    Process process = null;
                    try
                    {
                        process = Process.GetProcessById(Convert.ToInt32(managementObject["ProcessID"]));
                        if (process == null)
                        {
                            continue;
                        }
                        if (!allowProcessName.Exists((n) => n == process.ProcessName))
                        {
                            continue;
                        }

                        childs.Add(new IChildProcess() { Id = process.Id, ProcessName = process.ProcessName });

                        childs.AddRange(Community.GetChildProcess(process.Id));

                        process.Close();
                        process.Dispose();
                    }
                    catch (Win32Exception ex)
                    {
                        App.PrintService.Log($"Win32Exception --> {processId} {ex.Message}, {ex.StackTrace}", Min_Helpers.PrintHelper.Print.EMode.error);
                    }
                    catch (Exception ex)
                    {
                        if (!Regex.IsMatch(ex.Message, "Process with an Id of [0-9]+ is not running", RegexOptions.IgnoreCase) && !Regex.IsMatch(ex.Message, "Process has exited, so the requested information is not available", RegexOptions.IgnoreCase))
                        {
                            throw;
                        }
                    }
                }

                return childs;
            }
            catch (ManagementException ex)
            {
                App.PrintService.Log($"ManagementException --> {processId} {ex.Message}, {ex.StackTrace}", Min_Helpers.PrintHelper.Print.EMode.error);

                return new List<IChildProcess>();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Kill Child Process
        /// </summary>
        /// <param name="processId"></param>
        public static void KillChildProcess(int processId)
        {
            try
            {
                List<string> allowProcessName = new List<string>() { "mediamtx", "cmd", "conhost", "yt-dlp", "ffmpeg" };

                ManagementObjectSearcher managementSearcher = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", processId));
                if (managementSearcher.Get().Count == 0)
                {
                    return;
                }
                foreach (ManagementObject managementObject in managementSearcher.Get())
                {
                    Process process = null;
                    try
                    {
                        process = Process.GetProcessById(Convert.ToInt32(managementObject["ProcessID"]));
                        if (process == null)
                        {
                            continue;
                        }
                        if (!allowProcessName.Exists((n) => n == process.ProcessName))
                        {
                            continue;
                        }

                        Community.KillChildProcess(process.Id);

                        if (!process.HasExited)
                        {
                            process.Kill();
                            App.PrintService.Log($"kill process --> {process.ProcessName}[{process.Id}]", Min_Helpers.PrintHelper.Print.EMode.success);
                        }
                        process.Close();
                        process.Dispose();
                    }
                    catch (Win32Exception ex)
                    {
                        App.PrintService.Log($"Win32Exception --> {processId} {ex.Message}, {ex.StackTrace}", Min_Helpers.PrintHelper.Print.EMode.error);
                    }
                    catch (Exception ex)
                    {
                        if (!Regex.IsMatch(ex.Message, "Process with an Id of [0-9]+ is not running", RegexOptions.IgnoreCase) && !Regex.IsMatch(ex.Message, "Process has exited, so the requested information is not available", RegexOptions.IgnoreCase))
                        {
                            throw;
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                App.PrintService.Log($"ManagementException --> {processId} {ex.Message}, {ex.StackTrace}", Min_Helpers.PrintHelper.Print.EMode.error);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
