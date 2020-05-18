using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MelonLoader;
using UnityEngine;

namespace AvatarIdDumper
{
    public class Main : MelonMod
    {
        public static int session = 0;
        public static int version = 3;
        public static int usersInSession = 1;
        public static string session_start;
        public string last_instance;
        public static string no_instance;
        static float last_routine;

        private delegate void AvatarInstantiateDelegate(IntPtr @this, IntPtr a_ptr, IntPtr a_desc_ptr, bool loaded);
        private static AvatarInstantiateDelegate on_avatar_instantiate_delegate;

        // Settings
        public static Boolean mute = false;
        public static Boolean mute_errors = false;
        public static Boolean debug = false;
        public static Boolean keep_logs = true;
        public static Boolean upload_logs = true;
        public static Boolean bleeding_edge = false;

        public static void Log(string s)
        {
            if (mute && !debug) return;
            MelonModLogger.Log(s);
        }

        public static void LogError(string s)
        {
            if (mute_errors && !debug) return;
            MelonModLogger.LogError(s);
        }

        public static void WriteLogs(List<string> idList)
        {
            if (idList == null || idList.Count == 0) return;
            if (debug) Log("Logging " + idList.Count.ToString() + " avatar ids...");
            if (!Directory.Exists("ALogs")) Directory.CreateDirectory("ALogs");

            string path = "ALogs/ALog-" + session.ToString() + "-" + session_start + "-n.txt";
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
            if (!upload_logs || !Directory.Exists("ALogs")) return;
            if (debug) Log("Trying to upload avatar ids...");
            foreach (string f in Directory.GetFiles("ALogs"))
            {
                try
                {
                    if (f.Contains("ALog-") && f.EndsWith("-n.txt"))
                    {
                        if (debug) Log("Uploading " + f);
                        byte[] data = File.ReadAllBytes(f);

                        WebRequest request = WebRequest.Create("http://vrcavatars.tk");
                        request.ContentLength = data.Length;
                        request.Method = "PUT";
                        request.Timeout = 10000;

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

        public void UploadLogsThreaded()
        {
            if (!upload_logs) return;

            Thread thread = new Thread(new ThreadStart(UploadLogs));
            thread.Start();
        }

        // Thank you charlesdeepk
        // https://github.com/charlesdeepk/MultiplayerDynamicBonesMod/blob/bd992ad538f7e5fdd3939ae8d18bf19a82fba0a0/Main.cs#L84
        public static void Hook(IntPtr target, IntPtr detour)
        {
            typeof(Imports).GetMethod("Hook", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { target, detour });
        }

        public static void OnNewInstance()
        {
            Utils.NewInstance();
            
            session++;
            session_start = DateTime.Now.ToString("yyyy-dd-MM-HH-mm");
            last_routine = Time.time + 5;
        }

        public unsafe override void OnApplicationStart()
        {
            Log("Avatar Id Dumper has started.");
            no_instance = Utils.GetInstance();

            // Thank you charlesdeepk, his work is pretty great
            // https://github.com/charlesdeepk/MultiplayerDynamicBonesMod/blob/bd992ad538f7e5fdd3939ae8d18bf19a82fba0a0/Main.cs#L214

            IntPtr hook_func = (IntPtr)typeof(VRCAvatarManager.MulticastDelegateNPublicSealedVoGaVRBoObVoInBeInGaUnique).GetField("NativeMethodInfoPtr_Invoke_Public_Virtual_New_Void_GameObject_VRC_AvatarDescriptor_Boolean_0", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
            Hook(hook_func, new System.Action<IntPtr, IntPtr, IntPtr, bool>(OnAvatarInstantiate).Method.MethodHandle.GetFunctionPointer());
            on_avatar_instantiate_delegate = Marshal.GetDelegateForFunctionPointer<AvatarInstantiateDelegate>(*(IntPtr*)hook_func);
            if (debug) Log("Hook OnAvatarInstantiate " + ((on_avatar_instantiate_delegate != null) ? "succeeded!" : "failed!"));

            int latest_version = Utils.GetVersion(bleeding_edge);
            if (latest_version > version) // When there be an update
            {
                if (debug && latest_version == 999) Log("Found bleeding-edge update.");
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

            if (!keep_logs && Directory.Exists("ALogs"))
            {
                if (debug) Log("Removing logs...");
                foreach (string file in Directory.GetFiles("ALogs"))
                {
                    if (!file.Contains("ALog-")) continue;
                    File.Delete(file);
                    Log("Deleted file " + file);
                }
            }
            //base.OnApplicationQuit();
        }

        private static void OnAvatarInstantiate(IntPtr @this, IntPtr a_ptr, IntPtr a_desc_ptr, bool loaded)
        {
            on_avatar_instantiate_delegate(@this, a_ptr, a_desc_ptr, loaded);

            if (loaded)
            {
                GameObject avatar = new GameObject(a_ptr);
                VRCPlayer user = avatar.transform.root.GetComponentInChildren<VRCPlayer>();
                if (user.prop_Player_0.field_Private_APIUser_0.id != VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_Player_0.field_Private_APIUser_0.id && !Utils.loggedList.Contains(user.prop_Player_0.field_Private_APIUser_0.id))
                {
                    if (debug) Log("New avatar loaded (OnAvatarInstantiate)!");
                    Utils.loggedList.Add(user.prop_ApiAvatar_0.id);
                    WriteLogs(new List<string> { user.prop_ApiAvatar_0.id });
                }
            }
        }

        public override void OnUpdate()
        {
            string instance = Utils.GetInstance();
            if (last_instance != instance)
            {
                if (instance == no_instance)
                {
                    if (debug) Log("Left instance.");
                    UploadLogsThreaded();
                    last_instance = instance;
                }
                else
                {
                    last_instance = instance;
                    if (debug) Log("New instance: " + last_instance);
                    OnNewInstance();
                }
            }

            try
            {
                if (last_routine != 0 && Time.time > last_routine && Utils.GetPlayerManager() != null)
                {
                    last_routine = (on_avatar_instantiate_delegate != null) ? Time.time + 30: 0;
                    WriteLogs(Utils.LogAvatars());
                }
            }
            catch (Exception e)
            {
                Log("Error in main loop " + e.Message + " in " + e.Source + " Stack: " + e.StackTrace);
            }

            // Only do intensive OnUpdate if OnAvatarInstantiate hook fails - to shorten frames
            if (on_avatar_instantiate_delegate == null)
            {
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
                        WriteLogs(Utils.LogAvatars());
                    }

                    usersInSession = userCount;
                }
            }
            // base.OnUpdate();
        }
    }
}
