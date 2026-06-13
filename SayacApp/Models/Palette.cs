namespace SayacApp.Models;

/// <summary>
/// Fixed swatch palette carried over 1:1 from the original AHK app.
/// Stored as 6-digit hex strings (no leading '#').
/// </summary>
public static class Palette
{
    public static readonly string[] Colors =
    {
        "0f0f0f", "111111", "1A1A1A", "2a2a2a", "3A3A3A", "FFFFFF",
        "E8E8E8", "AAAAAA", "F87171", "FB923C", "FACC15", "A3E635",
        "4ADE80", "2DD4BF", "38BDF8", "60A5FA", "818CF8", "A78BFA",
        "C084FC", "F472B6", "F43F5E", "14B8A6", "22C55E", "64748B"
    };
}
