using System.Collections.Generic;

namespace QuasimorphHelloWorld
{
    public class ModConfig
    {
        public class ItemEntry
        {
            public string ItemId { get; set; } = string.Empty;
            public int Count { get; set; } = 1;
        }

        public class SavedEquipment
        {
            public Dictionary<string, string> Equipment { get; set; } =
                new Dictionary<string, string>();
            public Dictionary<string, string> Limbs { get; set; } =
                new Dictionary<string, string>();
            public Dictionary<string, List<string>> Implants { get; set; } =
                new Dictionary<string, List<string>>();
        }

        public List<ItemEntry> Items { get; set; } = new List<ItemEntry>();
        public Dictionary<string, SavedEquipment> SavedEquipmentHistory { get; set; } =
            new Dictionary<string, SavedEquipment>();
        public string HotkeyCode { get; set; } = "G";
    }
}
