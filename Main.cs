using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using MelonLoader;
using UnityEngine;

namespace AvatarIdDumper
{
    public class Main : MelonMod
    {
        int session = 0;
        public string session_start;
        public string last_instance;
        public static string no_instance;
        static float last_routine;
        public static Boolean debug = false;

        public void Log()
        {
            List<string> idList = Utils.LogAvatars();

            if (idList.Count == 0) return;
            MelonModLogger.Log("Logging " + idList.Count.ToString() + " avatar ids...");

            string path = "ALog-" + session.ToString() + "-" + session_start + "-n.txt";
            using (StreamWriter sw = File.AppendText(path))
            {
                foreach (string id in idList)
                {
                    MelonModLogger.Log(id);
                    sw.WriteLine(id);
                }
            }
        }

        public void OnNewInstance()
        {
            MelonModLogger.Log("New instance, trying to upload avatar ids...");
            session++;
            session_start = DateTime.Now.ToString("yyyy-dd-M");

            try
            {
                foreach (string f in Directory.GetFiles("."))
                {
                    if (f.StartsWith(".\\ALog-") && f.EndsWith("-n.txt"))
                    {
                        byte[] data = File.ReadAllBytes(f);

                        WebRequest request = WebRequest.Create("http://vrcavatars.tk");
                        request.ContentLength = data.Length;
                        request.Method = "PUT";
                        request.Timeout = 100;

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
            }
            catch (Exception e)
            {
                MelonModLogger.LogError("Failed to do avatar send routine. " + e.Message + " in " + e.Source + " Stack: " + e.StackTrace);
            }

            last_routine = Time.time + 5;
        }

        public override void OnApplicationStart()
        {
            MelonModLogger.Log("Avatar Id Dumper has started.");
            no_instance = Utils.GetInstance();
            //base.OnApplicationStart();
        }

        public override void OnUpdate()
        {
            string instance = Utils.GetInstance();
            if (last_instance != instance && instance != no_instance)
            {
                last_instance = instance;
                if (debug) MelonModLogger.Log("New instance: " + last_instance);
                Utils.NewInstance();
                OnNewInstance();
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
