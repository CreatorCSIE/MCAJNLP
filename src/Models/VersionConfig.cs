using System.Text.Json.Serialization;

namespace MCAJNLP.Models
{
    public class VersionConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "classic";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("main_class")]
        public string MainClass { get; set; } = "com.mojang.minecraft.MinecraftApplet";

        [JsonPropertyName("jar")]
        public string Jar { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; } = 854;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 480;

        [JsonPropertyName("has_15a_patch")]
        public bool Has15aPatch { get; set; }

        [JsonPropertyName("dpi_fix")]
        public bool DpiFix { get; set; }

        [JsonPropertyName("haspaid")]
        public bool HasPaid { get; set; }

        [JsonPropertyName("has_human")]
        public bool HasHuman { get; set; }

        [JsonPropertyName("has_block_menu")]
        public bool HasBlockMenu { get; set; }

        [JsonPropertyName("has_set_spawn")]
        public bool HasSetSpawn { get; set; }

        [JsonPropertyName("inventory_key")]
        public string InventoryKey { get; set; }

        [JsonPropertyName("has_drop")]
        public bool HasDrop { get; set; }

        [JsonPropertyName("has_sneak")]
        public bool HasSneak { get; set; }
    }
}