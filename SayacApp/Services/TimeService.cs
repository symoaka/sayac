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
        var diff = targetUtc - nowUtc;
        if (diff.TotalSeconds <= 0)
            return Loc["Completed"];

        var total = (long)Math.Floor(diff.TotalSeconds);
        var days = total / 86400;
        var rest = total % 86400;
        var hours = rest / 3600;
        rest %= 3600;
        var mins = rest / 60;
        var secs = rest % 60;

        var clock = $"{hours:00}:{mins:00}:{secs:00}";
        return days > 0 ? $"{days} {Loc["DayUnit"]} {clock}" : clock;
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
