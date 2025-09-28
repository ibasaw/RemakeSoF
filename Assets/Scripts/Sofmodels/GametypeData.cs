using System;
using RemakeSoF.BG.Constants;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // gametypeData_t
    // Dynamic gametype data container. Mirrors gametypeData_s from bg_public.h
    public class GametypeData
    {
        // const char* name;
        public string name;

        // const char* displayName;
        public string displayName;

        // const char* script;
        public string script;

        // const char* description;
        public string description;

        // const char* basegametype;
        public string basegametype;

        // respawnType_t respawnType;
        public RespawnType respawnType;

        // qboolean pickupsDisabled;
        public bool pickupsDisabled;

        // qboolean teams;
        public bool teams;

        // qboolean showKills;
        public bool showKills;

        // int backpack;
        public int backpack;

        // gametypePhoto_t photos[4];
        public GametypePhoto[] photos = new GametypePhoto[4];
    }
}