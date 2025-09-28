using System;

namespace RemakeSoF.BG.Sofmodels
{
    [Serializable]
    // gitem_t
    // Represents a pickup/item definition from the original header
    public class GItem
    {
        // char *classname; // spawning name
        public string classname;

        // char *pickup_sound;
        public string pickup_sound;

        // char *world_model[MAX_ITEM_MODELS];
        public string[] world_model = new string[4];

        // char *icon;
        public string icon;

        // char *render;
        public string render;

        // char *pickup_prefix; // an, some, a, the, etc..
        public string pickup_prefix;

        // char *pickup_name; // for printing on pickup
        public string pickup_name;

        // int quantity; // for ammo how much
        public int quantity;

        // itemType_t giType; // IT_* flags
        public int giType;

        // int giTag;
        public int giTag;

        // char *precaches; // string of all models and images this item will use
        public string precaches;

        // char *sounds; // string of all sounds this item will use
        public string sounds;

        // int outfittingGroup;
        public int outfittingGroup;
    }
}