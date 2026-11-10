using System.Globalization;

namespace NxtShift;

/// <summary>Pure time arithmetic and formatting. Everything is derived from timestamps, never from counted ticks.</summary>
public static class ShiftMath
{
    public static long RemainingMs(ActiveShift shift, long nowMs) => Math.Max(0, shift.EndMs - nowMs);

    public static long WorkedMs(ActiveShift shift, long nowMs) =>
        Math.Clamp(nowMs - shift.StartMs, 0, Math.Max(0, (shift.EndedEarlyAtMs ?? long.MaxValue) - shift.StartMs));

    /// <summary>0 at punch-in, 1 when the shift is up.</summary>
    public static double Progress(ActiveShift shift, long nowMs)
    {
        var total = shift.EndMs - shift.StartMs;
        return total <= 0 ? 1 : Math.Clamp((double)(nowMs - shift.StartMs) / total, 0, 1);
    }

    public static long OvertimeMs(ActiveShift shift, long nowMs) => Math.Max(0, nowMs - shift.EndMs);

    public static long HeadsUpMs(ActiveShift shift) => shift.EndMs - ShiftSettings.HeadsUpMinutes * 60_000L;

    /// <summary>The shift is over (time up or ended early) and the user hasn't confirmed punching out.</summary>
    public static bool IsDue(ActiveShift shift, long nowMs) => shift.EndedEarly || nowMs >= shift.EndMs;

    /// <summary>The screen the app should show for this state.</summary>
    public static ShiftScreen ScreenFor(ShiftData data, long nowMs)
    {
        if (data.Current is { } shift)
        {
            var snoozed = shift.SnoozedUntilMs is { } until && nowMs < until;
            return IsDue(shift, nowMs) && !snoozed ? ShiftScreen.TimeUp : ShiftScreen.Running;
        }
        if (data.Last is not null) return ShiftScreen.Done;
        return data.Onboarded ? ShiftScreen.Home : ShiftScreen.Welcome;
    }

    /// <summary>
    /// The start instant for a wall-clock time the user picked: today if that is not in the future, otherwise yesterday
    /// (a night shift that began before midnight).
    /// </summary>
    public static long ResolveStart(TimeOnly time, DateTimeOffset nowLocal)
    {
        var today = new DateTimeOffset(DateOnly.FromDateTime(nowLocal.DateTime).ToDateTime(time), nowLocal.Offset);
        var start = today > nowLocal ? today.AddDays(-1) : today;
        return start.ToUnixTimeMilliseconds();
    }

    public enum ReminderKind { HeadsUp, End, Repeat, Snooze, Hour }

    public sealed record Reminder(string Id, ReminderKind Kind, long AtMs);

    /// <summary>Upcoming in-app reminders for the current state, soonest first.</summary>
    public static IReadOnlyList<Reminder> UpcomingReminders(ShiftData data, long nowMs)
    {
        var list = new List<Reminder>();
        if (data.Current is not { } shift) return list;
        var s = data.Settings;

        if (shift.SnoozedUntilMs is { } until && until > nowMs)
        {
            list.Add(new("snooze", ReminderKind.Snooze, until));
            return list;
        }
        if (shift.EndedEarly) return list;

        if (s.HourlySound)
        {
            for (var k = 1; ; k++)
            {
                var at = HourMarkMs(shift, k);
                if (at >= shift.EndMs) break;
                if (at > nowMs) list.Add(new($"hour-{k}", ReminderKind.Hour, at));
            }
        }
        if (s.HeadsUp && s.RemindAtEnd && HeadsUpMs(shift) > nowMs && HeadsUpMs(shift) > shift.StartMs)
            list.Add(new("headsup", ReminderKind.HeadsUp, HeadsUpMs(shift)));
        if ((s.RemindAtEnd || s.EndSound) && shift.EndMs > nowMs)
            list.Add(new("end", ReminderKind.End, shift.EndMs));
        if (s.RemindAtEnd && s.Repeat)
        {
            var step = ShiftSettings.RepeatMinutes * 60_000L;
            for (var i = 1; i <= ShiftSettings.MaxRepeats; i++)
            {
                var at = shift.EndMs + i * step;
                if (at > nowMs) { list.Add(new("repeat", ReminderKind.Repeat, at)); break; }
            }
        }
        list.Sort((a, b) => a.AtMs.CompareTo(b.AtMs));
        return list;
    }

    /// <summary>The instant the k-th full hour of the shift is done.</summary>
    public static long HourMarkMs(ActiveShift shift, int k) => shift.StartMs + k * 3_600_000L;

    /// <summary>The next hour mark still ahead and before the end, or null.</summary>
    public static long? NextHourMarkMs(ActiveShift shift, long nowMs)
    {
        if (shift.EndedEarly || nowMs >= shift.EndMs) return null;
        var k = (int)(Math.Max(0, nowMs - shift.StartMs) / 3_600_000L) + 1;
        var at = HourMarkMs(shift, k);
        return at < shift.EndMs ? at : null;
    }

    // ---------- Formatting (Latin digits in every language; times read left-to-right) ----------

    /// <summary>17:08</summary>
    public static string Clock(long ms, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(ms), zone).ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>5:34:12 (hours not padded).</summary>
    public static string Countdown(long ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
    }

    /// <summary>2:56 (h:mm).</summary>
    public static string HoursMinutes(long ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return $"{(int)t.TotalHours}:{t.Minutes:00}";
    }

    /// <summary>8:30 from minutes.</summary>
    public static string HoursMinutes(int minutes) => $"{minutes / 60}:{minutes % 60:00}";
}
