using System.Text.Json;

namespace MessengerBubbles.Core;

public enum PanelSize { Small, Medium, Large }

public record Settings
{
    public bool PersistentBubble { get; set; }
    public bool Preview { get; set; } = true;
    public bool Autostart { get; set; }
    public bool Sound { get; set; } = true;
    public bool GroupBubbles { get; set; }
    public bool CheckUpdates { get; set; } = true;
    // Read once at launch, so a change applies the next time the app opens.
    public bool DisableGpu { get; set; }
    // Null until chosen, so the first run asks.
    public AppLanguage? Language { get; set; }
    public PanelSize PanelSize { get; set; } = PanelSize.Medium;
    // Local time until which new messages pop no bubble and play no sound.
    public DateTime? SnoozedUntil { get; set; }
    public Dictionary<string, Box> Positions { get; set; } = new();

    public bool IsSnoozed(DateTime now) => SnoozedUntil > now;

    public static Settings Load(string path)
    {
        try
        {
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path));
            if (s?.Positions != null)
            {
                s.Positions = s.Positions.Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value);
                return s;
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
        }
        return new Settings();
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this));
        File.Move(temp, path, true);
    }
}
