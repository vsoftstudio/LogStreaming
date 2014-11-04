using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.IO;
using Microsoft.Framework.ConfigurationModel;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;

namespace LogStreaming.Server.vNext.Hubs
{
    [HubName("logs")]
    public class LogHub : Hub
    {
        private FileSystemWatcher watcher = null;
        private string ClientId = null;
        private static Dictionary<string, bool> stopRequests = new Dictionary<string, bool>();

        public void Echo(string message)
        {
            Clients.Caller.alert(message);
        }

        public void FetchFileList()
        {
            try
            {
                var configuration = new Configuration();
                configuration.AddJsonFile("Config.json");
                var fileList = configuration.Get("Data:LogFileList");
                var list = File.ReadAllLines(fileList);

                Clients.Caller.displayFileList(list);
            }
            catch (Exception ex)
            {
                // TODO: replace this by a more secured logging
                Clients.Caller.alert(ex.ToString());
            }
        }

        public void Subscribe(string clientId, string logPath)
        {
            try
            {
                this.ClientId = clientId;

                if (File.Exists(logPath))
                {
                    KeepReading(logPath);
                }
                else
                {
                    watcher = new FileSystemWatcher();
                    watcher.Path = Path.GetDirectoryName(logPath);
                    watcher.Filter = Path.GetFileName(logPath);
                    watcher.Created += Watcher_Created;
                    watcher.Deleted += Watcher_Deleted;
                    watcher.Renamed += Watcher_Renamed;
                    watcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                // TODO: replace this by a more secured logging
                Clients.Caller.alert(ex.ToString());
            }
        }

        public void Unsubscribe(string clientId)
        {
            try
            {
                if (stopRequests.ContainsKey(clientId))
                {
                    stopRequests[clientId] = true;
                }
                else
                {
                    DeinitializeWatcher();
                    stopRequests.Remove(clientId);
                    FetchFileList();
                }
            }
            catch (Exception ex)
            {
                // TODO: replace this by a more secured logging
                Clients.Caller.alert(ex.ToString());
            }
        }

        private void KeepReading(string logPath)
        {
            stopRequests[ClientId] = false;

            var worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerAsync(logPath);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var logPath = (string)e.Argument;

            // Adopt from http://www.codeproject.com/Articles/7568/Tail-NET
            using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                //start at the end of the file
                long lastMaxOffset = reader.BaseStream.Length;

                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                    if (stopRequests[ClientId])
                    {
                        DeinitializeWatcher();
                        stopRequests.Remove(ClientId);
                        FetchFileList();
                        break;
                    }

                    //if the file size has not changed, idle
                    if (reader.BaseStream.Length == lastMaxOffset)
                        continue;

                    //seek to the last max offset
                    reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                    //read out of the file until the EOF
                    string line = "";
                    while (!string.IsNullOrEmpty(line = reader.ReadToEnd()))
                    {
                        Clients.Caller.append(line);
                    }

                    //update the last max offset
                    lastMaxOffset = reader.BaseStream.Position;
                }
            }
        }

        private void DeinitializeWatcher()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= Watcher_Created;
                watcher.Deleted -= Watcher_Deleted;
                watcher.Renamed -= Watcher_Renamed;
                watcher = null;
            }
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Clients.Caller.handleFileRenamed();
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Clients.Caller.handleFileCreated();
            KeepReading(e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Clients.Caller.handleFileDeleted();
        }
    }
}