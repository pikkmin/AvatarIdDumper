﻿using System;
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
        int session = 0;
        int version = 2;
        public string session_start;
        public string last_instance;
        public static string no_instance;
        static float last_routine;
        public static Boolean debug = false;

        public static int usersInSession = 1;

        public void Log()
        {
            List<string> idList = Utils.LogAvatars();

            if (idList == null || idList.Count == 0) return;
            if (debug) MelonModLogger.Log("Logging " + idList.Count.ToString() + " avatar ids...");

            string path = "ALog-" + session.ToString() + "-" + session_start + "-n.txt";
            using (StreamWriter sw = File.AppendText(path))
            {
                foreach (string id in idList)
                {
                    MelonModLogger.Log("Logged id " + id);
                    sw.WriteLine(id);
                }
            }

            if (Utils.GetFileSize() > 49152)
            {
                if (debug) MelonModLogger.Log("File nearly full, simulating new instance.");
                Utils.NewInstance();
                OnNewInstance();
            }
        }

        public void OnNewInstance()
        {
            Utils.NewInstance();

            MelonModLogger.Log("New instance, trying to upload avatar ids...");
            session++;
            session_start = DateTime.Now.ToString("yyyy-dd-MM-HH-mm");


            foreach (string f in Directory.GetFiles("."))
            {
                try
                {
                    if (f.StartsWith(".\\ALog-") && f.EndsWith("-n.txt"))
                    {
                        if (debug) MelonModLogger.Log("Uploading " + f);
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
                            MelonModLogger.LogError("Failed to send avatars from " + f + " to http://vrcavatars.tk");
                            MelonModLogger.LogError(response.StatusCode.ToString() + ", " + response.StatusDescription);
                        }
                        else
                        {
                            MelonModLogger.Log("Sent avatars from " + f + " to http://vrcavatars.tk");
                            File.Move(f, f.Substring(0, f.Length - 6) + ".txt");
                        }
                    }
                }
                catch (Exception e)
                {
                    MelonModLogger.LogError("Failed to send avatars from " + f + " to http://vrcavatars.tk. " + e.Message + " in " + e.Source + " Stack: " + e.StackTrace);
                }
            }

            last_routine = Time.time + 5;
        }

        public override void OnApplicationStart()
        {
            MelonModLogger.Log("Avatar Id Dumper has started.");
            no_instance = Utils.GetInstance();
            
            int latest_version = Utils.GetVersion();
            if (latest_version != version) // When there be an update
            {
                MelonModLogger.Log("New version available! Updating to new version...");

                WebRequest request = WebRequest.Create("https://raw.githubusercontent.com/Katistic/AvatarIdDumper/master/build/AvatarIdDump.dll");
                ServicePointManager.ServerCertificateValidationCallback = (System.Object s, X509Certificate c, X509Chain cc, SslPolicyErrors ssl) => true;

                ServicePointManager.ServerCertificateValidationCallback = (System.Object s, X509Certificate c, X509Chain cc, SslPolicyErrors ssl) => true;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Download & write new mod file
                byte[] buffer = new byte[1024];
                using (FileStream fs = File.OpenWrite("Mods/Avatar" + latest_version.ToString() + "IdDump.dll"))
                {
                    using (Stream rs = response.GetResponseStream())
                    {
                        int bytesRead = rs.Read(buffer, 0, buffer.Length);
                        while (bytesRead > 0)
                        {
                            fs.Write(buffer, 0, buffer.Length);
                            bytesRead = rs.Read(buffer, 0, buffer.Length);
                        }
                    }
                }

                // Delete old mod file(s) last so failed update doesn't break anything :<)
                foreach (string file in Directory.GetFiles("Mods"))
                {
                    if (!file.Contains("AvatarIdDump")) continue;
                    File.Delete(file);
                }
                File.Move("Mods/Avatar" + latest_version.ToString() + "IdDump.dll", "Mods/AvatarIdDump.dll");

                MelonModLogger.Log("Update complete! Make sure to restart VRChat to apply changes.");
            }
            //base.OnApplicationStart();
        }

        public override void OnUpdate()
        {
            string instance = Utils.GetInstance();
            if (last_instance != instance && instance != no_instance)
            {
                last_instance = instance;
                if (debug) MelonModLogger.Log("New instance: " + last_instance);
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
                    if (debug) MelonModLogger.Log("Player joined, logging avatar");
                    last_routine = Time.time + 30;
                    Log();
                }

                usersInSession = userCount;
            }

            try
            {
                if (Time.time > last_routine && Utils.GetPlayerManager() != null)
                {
                    last_routine = Time.time + 30;
                    Log();
                }
            }
            catch (Exception e)
            {
                MelonModLogger.Log("Error in main loop " + e.Message + " in " + e.Source + " Stack: " + e.StackTrace);
            }
            // base.OnUpdate();
        }
    }
}
