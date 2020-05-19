using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VRC;
using VRC.Core;

using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace AvatarIdDumper
{
    class Utils
    {
        public static List<string> loggedList = new List<string>();

        public static List<string> LogAvatars()
        {
            var users = GetAllPlayers();
            List<string> idList = new List<string>();

            if (users == null) return null;
            for (var c = 0; c < users.Count; c++)
            {
                var user = users[c];
                if (Main.public_only && user.prop_VRCAvatarManager_0.field_Private_ApiAvatar_0.releaseStatus != "public") continue;
                if (user == null || user.prop_VRCAvatarManager_0 == null || user.field_Private_APIUser_0 == null) continue;
                if (user.field_Private_VRCAvatarManager_0 == null || user.field_Private_VRCAvatarManager_0.field_Private_ApiAvatar_0 == null) continue;
                if (VRCPlayer.field_Internal_Static_VRCPlayer_0 == null) continue;
                if(user.field_Private_APIUser_0.id == VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Private_Player_0.field_Private_APIUser_0.id) continue;
                if (user.prop_VRCAvatarManager_0.enabled == false) continue;
                var contains = loggedList.Contains(user.field_Private_VRCAvatarManager_0.field_Private_ApiAvatar_0.id);
                if (!contains)
                {
                    idList.Add(user.field_Private_VRCAvatarManager_0.field_Private_ApiAvatar_0.id);
                    loggedList.Add(user.field_Private_VRCAvatarManager_0.field_Private_ApiAvatar_0.id);
                }
            }
            return idList;
        }

        public static Il2CppSystem.Collections.Generic.List<Player> GetAllPlayers()
        {
            if (PlayerManager.field_Private_Static_PlayerManager_0 == null) return null;
            return PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;
        }

        public static int GetFileSize()
        {
            string file = string.Join("\n", loggedList);
            byte[] fileBytes = Encoding.ASCII.GetBytes(file);
            return fileBytes.Length;
        }

        public static PlayerManager GetPlayerManager()
        {
            return PlayerManager.prop_PlayerManager_0;
        }

        public static string GetChecksum(Stream s)
        {
            using (SHA256 hasher = SHA256Managed.Create()) return Convert.ToBase64String(hasher.ComputeHash(s));
        }

        // THANK YOU KICHIRO1337/HASH
        public static int GetVersion(System.Boolean be)
        {
            int version;

            if (!be)
            {
                WebRequest request = WebRequest.Create("https://raw.githubusercontent.com/Katistic/AvatarIdDumper/master/version.txt");
                ServicePointManager.ServerCertificateValidationCallback = (System.Object s, X509Certificate c, X509Chain cc, SslPolicyErrors ssl) => true;

                WebResponse response = request.GetResponse();
                using (StreamReader rs = new StreamReader(response.GetResponseStream())) version = int.Parse(rs.ReadToEnd());

                return version;
            }
            else
            {
                WebRequest request = WebRequest.Create("https://raw.githubusercontent.com/Katistic/AvatarIdDumper/master/latest.txt");
                ServicePointManager.ServerCertificateValidationCallback = (System.Object s, X509Certificate c, X509Chain cc, SslPolicyErrors ssl) => true;

                string latest;
                string current;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (MemoryStream s = new MemoryStream(Convert.FromBase64String(new StreamReader(response.GetResponseStream()).ReadToEnd()))) latest = GetChecksum(s);
                using (Stream s = File.Open("Mods/AvatarIdDump.dll", FileMode.Open)) current = GetChecksum(s);

                return (current == latest) ? 0 : 999;
            }
        }

        public static string GetInstance()
        {
            ApiWorld currentRoom = RoomManagerBase.field_Internal_Static_ApiWorld_0;
            if (currentRoom != null)
            {
                return currentRoom.id + ":" + currentRoom.currentInstanceIdWithTags;
            }
            return null;
        }

        public static void NewInstance()
        {
            loggedList.Clear();
        }

        public static QuickMenu GetQuickMenu()
        {
            return QuickMenu.prop_QuickMenu_0;
        }

        public static GameObject MakeButton(String name, float x, float y, Action listener)
        {
            QuickMenu quickMenu = GetQuickMenu();
            Transform btn_t = UnityEngine.Object.Instantiate<GameObject>(quickMenu.transform.Find("CameraMenu/BackButton").gameObject).transform;

            float x_pos = quickMenu.transform.Find("UserInteractMenu/ForceLogoutButton").localPosition.x - quickMenu.transform.Find("UserInteractMenu/BanButton").localPosition.x;
            float y_pos = quickMenu.transform.Find("UserInteractMenu/ForceLogoutButton").localPosition.x - quickMenu.transform.Find("UserInteractMenu/BanButton").localPosition.x;
            btn_t.localPosition = new Vector3(btn_t.localPosition.x + x_pos * x, btn_t.localPosition.y + y_pos * y, btn_t.localPosition.z);

            btn_t.name = name;
            btn_t.SetParent(quickMenu.transform.Find("ShortcutMenu"), false);
            btn_t.GetComponentInChildren<Text>().text = name;
            btn_t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            btn_t.GetComponent<Button>().onClick.AddListener(listener);
            btn_t.GetComponentInChildren<UiTooltip>().text = name;

            return btn_t.gameObject;
        }

        public static void SwitchRandomAvatar()
        {
            string id;

            WebRequest request = WebRequest.Create("http://vrcavatars.tk/public/random");
            WebResponse response = request.GetResponse();
            using (StreamReader rs = new StreamReader(response.GetResponseStream())) id = Regex.Replace(rs.ReadToEnd(), @"\n", "");

            var menu = GameObject.Find("Screens").transform.Find("Avatar").GetComponent<VRC.UI.PageAvatar>();
            ApiAvatar avatar = new ApiAvatar();
            avatar.id = id;

            avatar.Get(new Action<ApiContainer>(delegate (ApiContainer container)
            {
                menu.avatar.field_Internal_ApiAvatar_0 = avatar;
                menu.ChangeToSelectedAvatar();
            }));
        }

        /* Threading breaks the .Get
        public static void _SwitchRandomAvatar()
        {
            Thread thread = new Thread(new ThreadStart(_SwitchRandomAvatar));
            thread.Start();
        }
        */
    }
}
