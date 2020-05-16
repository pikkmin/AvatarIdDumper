using System.Collections.Generic;
using MelonLoader;
using VRC;
using VRC.Core;

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

        public static PlayerManager GetPlayerManager()
        {
            return PlayerManager.prop_PlayerManager_0;
        }

        public static string GetInstance()
        {
            ApiWorld currentRoom = RoomManagerBase.field_Internal_Static_ApiWorld_0;
            if (((currentRoom != null) ? currentRoom.currentInstanceIdOnly : null) != null)
            {
                return currentRoom.id + ":" + currentRoom.currentInstanceIdWithTags;
            }
            return null;
        }

        public static void NewInstance()
        {
            loggedList.Clear();
        }
    }
}
