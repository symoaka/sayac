using System;

namespace SayacApp.Services;

/// <summary>
/// Time formatting. Targets are stored in UTC and displayed in the machine's local
/// timezone with correct DST (replacing the old hardcoded +3h Istanbul offset).
/// </summary>
public static class TimeService
{
    private static LocalizationService Loc => LocalizationService.Instance;

    /// <summary>"2 gün 03:11:09", "03:11:09", or "Tamamlandı" when the target has passed.</summary>
    public static string FormatRemaining(DateTimeOffset targetUtc, DateTimeOffset nowUtc)
    {
        var (big, clock) = FormatRemainingParts(targetUtc, nowUtc);
        if (clock.Length == 0) return big;                 // completed
        return big.Length > 0 ? $"{big} {clock}" : clock;  // "2 gün 03:11:09" or "03:11:09"
    }

    /// <summary>
    /// Split countdown for the tile UI: <c>big</c> is the days portion ("364 gün" / "11 d",
    /// empty under a day) and <c>clock</c> is "HH:mm:ss". When completed, <c>big</c> is the
    /// "Completed" label and <c>clock</c> is empty.
    /// </summary>
    public static (string big, string clock) FormatRemainingParts(DateTimeOffset targetUtc, DateTimeOffset nowUtc)
    {
        var diff = targetUtc - nowUtc;
        if (diff.TotalSeconds <= 0)
            return (Loc["Completed"], "");

        var total = (long)Math.Floor(diff.TotalSeconds);
        var days = total / 86400;
        var rest = total % 86400;
        var hours = rest / 3600;
        rest %= 3600;
        var mins = rest / 60;
        var secs = rest % 60;

        var clock = $"{hours:00}:{mins:00}:{secs:00}";
        var big = days > 0 ? $"{days} {Loc["DayUnit"]}" : "";
        return (big, clock);
    }

    public static bool IsCompleted(DateTimeOffset targetUtc, DateTimeOffset nowUtc) =>
        (targetUtc - nowUtc).TotalSeconds <= 0;

    /// <summary>Target shown in local time, "yyyy-MM-dd HH:mm".</summary>
    public static string FormatTarget(DateTimeOffset targetUtc) =>
        targetUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    /// <summary>Combine a local date + local time-of-day into a UTC instant.</summary>
    public static DateTimeOffset LocalToUtc(DateTime localDate, TimeSpan localTime)
    {
        var localDt = localDate.Date + localTime;
        // Treat as unspecified local wall-clock time, then convert.
        return new DateTimeOffset(DateTime.SpecifyKind(localDt, DateTimeKind.Local)).ToUniversalTime();
    }
}
