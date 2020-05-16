using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MelonLoader;
using UnityEngine;

namespace AvatarIdDumper
{
    public class Main : MelonMod
    {
        public static int session = 0;
        public static int version = 3;
        public static int usersInSession = 1;
        public string session_start;
        public string last_instance;
        public static string no_instance;
        static float last_routine;

        // Settings
        public static Boolean mute = false;
        public static Boolean mute_errors = false;
        public static Boolean debug = false;
        public static Boolean keep_logs = true;
        public static Boolean upload_logs = true;

        public void Log(string s)
        {
            if (mute && !debug) return;
            MelonModLogger.Log(s);
        }

        public void LogError(string s)
        {
            if (mute_errors && !debug) return;
            MelonModLogger.LogError(s);
        }

        public void WriteLogs()
        {
            List<string> idList = Utils.LogAvatars();

            if (idList == null || idList.Count == 0) return;
            if (debug) Log("Logging " + idList.Count.ToString() + " avatar ids...");

            string path = "ALog-" + session.ToString() + "-" + session_start + "-n.txt";
            using (StreamWriter sw = File.AppendText(path))
            {
                foreach (string id in idList)
                {
                    Log("Logged id " + id);
                    sw.WriteLine(id);
                }
            }

            if (Utils.GetFileSize() > 49152)
            {
                if (debug) Log("File nearly full, simulating new instance.");
                Utils.NewInstance();
                OnNewInstance();
            }
        }

        public void UploadLogs()
        {
            if (!upload_logs) return;
            if (debug) Log("Trying to upload avatar ids...");
            foreach (string f in Directory.GetFiles("."))
            {
                try
                {
                    if (f.StartsWith(".\\ALog-") && f.EndsWith("-n.txt"))
                    {
                        if (debug) Log("Uploading " + f);
                        byte[] data = File.ReadAllBytes(f);

                        WebRequest request = WebRequest.Create("http://vrcavatars.tk");
                        request.ContentLength = data.Length;
                        request.Method = "PUT";
                        request.Timeout = 2000;

                        using (Stream ds = request.GetRequestStream())
                        {
                            ds.Write(data, 0, data.Length);
                        }

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        if (response.StatusCode.ToString() != "OK")
                        {
                            LogError("Failed to send avatars from " + f + " to http://vrcavatars.tk");
                            LogError(response.StatusCode.ToString() + ", " + response.StatusDescription);
                        }
                        else
                        {
                            Log("Sent avatars from " + f + " to http://vrcavatars.tk");
                            File.Move(f, f.Substring(0, f.Length - 6) + ".txt");
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError("Failed to send avatars from " + f + " to http://vrcavatars.tk. " + e.Message + " in " + e.Source + " Stack: " + e.StackTrace);
                }
            }
        }

        public void OnNewInstance()
        {
            Utils.NewInstance();
            UploadLogs();

            session++;
            session_start = DateTime.Now.ToString("yyyy-dd-MM-HH-mm");
            last_routine = Time.time + 5;
        }

        public override void OnApplicationStart()
        {
            Log("Avatar Id Dumper has started.");
            no_instance = Utils.GetInstance();
            
            int latest_version = Utils.GetVersion();
            if (latest_version > version) // When there be an update
            {
                Log("New version available! Updating to new version...");

                WebRequest request = WebRequest.Create("https://raw.githubusercontent.com/Katistic/AvatarIdDumper/master/latest.txt");
                ServicePointManager.ServerCertificateValidationCallback = (System.Object s, X509Certificate c, X509Chain cc, SslPolicyErrors ssl) => true;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Download & write new mod file
                File.WriteAllBytes("Mods/Avatar" + latest_version.ToString() + "IdDump.dll", Convert.FromBase64String(new StreamReader(response.GetResponseStream()).ReadToEnd()));

                // Delete old mod file(s) last so failed update doesn't break anything :<)
                foreach (string file in Directory.GetFiles("Mods"))
                {
                    if (!file.Contains("AvatarIdDump")) continue;
                    File.Delete(file);
                }
                File.Move("Mods/Avatar" + latest_version.ToString() + "IdDump.dll", "Mods/AvatarIdDump.dll");

                Log("Update complete! Make sure to restart VRChat to apply changes.");
            }
            //base.OnApplicationStart();
        }

        public override void OnApplicationQuit()
        {
            UploadLogs();

            if (!keep_logs)
            {
                foreach (string file in Directory.GetFiles("Mods"))
                {
                    if (!file.StartsWith("ALog-")) continue;
                    File.Delete(file);
                }
            }
            //base.OnApplicationQuit();
        }

        public override void OnUpdate()
        {
            string instance = Utils.GetInstance();
            if (last_instance != instance && instance != no_instance)
            {
                last_instance = instance;
                if (debug) Log("New instance: " + last_instance);
                OnNewInstance();
            }

            var users = Utils.GetAllPlayers();
            int userCount;
            if (users == null) userCount = 1;
            else userCount = users.Count;

            if (userCount != usersInSession)
            {
                if (usersInSession < userCount)
                {
                    if (debug) Log("Player joined, logging avatar");
                    last_routine = Time.time + 30;
                    WriteLogs();
                }

                usersInSession = userCount;
            }

            try
            {
                if (Time.time > last_routine && Utils.GetPlayerManager() != null)
                {
                    last_routine = Time.time + 30;
                    WriteLogs();
                }
            }
            catch (Exception e)
            {
                Log("Error in main loop " + e.Message + " in " + e.Source + " Stack: " + e.StackTrace);
            }
            // base.OnUpdate();
        }
    }
}
