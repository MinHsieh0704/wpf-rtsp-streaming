﻿using System;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace wpf_rtsp_streaming.Helpers
{
    public class Community
    {
        /// <summary>
        /// Kill Child Process
        /// </summary>
        /// <param name="processId"></param>
        public static void KillChildProcess(int processId)
        {
            try
            {
                ManagementObjectSearcher managementSearcher = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", processId));
                foreach (ManagementObject managementObject in managementSearcher.Get())
                {
                    try
                    {
                        Process process = Process.GetProcessById(Convert.ToInt32(managementObject["ProcessID"]));
                        if (process == null)
                        {
                            continue;
                        }

                        Community.KillChildProcess(process.Id);

                        if (!process.HasExited) process.Kill();
                        process.Close();
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (!Regex.IsMatch(ex.Message, "Process with an Id of [0-9]+ is not running", RegexOptions.IgnoreCase))
                        {
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}