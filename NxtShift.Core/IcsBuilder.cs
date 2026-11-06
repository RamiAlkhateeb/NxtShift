using System.Globalization;
using System.Text;

namespace NxtShift;

/// <summary>Texts that go into the calendar event, in the user's language.</summary>
public sealed record IcsTexts(string Summary, string Description, string HeadsUpAlarm, string EndAlarm, string RepeatAlarm);

/// <summary>
/// Builds an RFC 5545 calendar file for the punch-out reminder. The phone's calendar fires its alarms even when the
/// web app is closed, which a static PWA cannot do on its own. The event sits at the shift's end; the UID stays the same
/// for the whole shift and SEQUENCE grows with every change, so importing again updates the event in most calendars.
/// </summary>
public static class IcsBuilder
{
    public const string FileName = "punch-out-reminder.ics";
    public const string MimeType = "text/calendar";

    public static string Build(ActiveShift shift, ShiftSettings settings, IcsTexts texts, long nowMs)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//NxtShift//Shift Timer//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:shift-{shift.Id}@nxtshift",
            $"SEQUENCE:{shift.CalendarSequence}",
            $"DTSTAMP:{Utc(nowMs)}",
            $"DTSTART:{Utc(shift.EndMs)}",
            $"DTEND:{Utc(shift.EndMs + 5 * 60_000L)}",
            $"SUMMARY:{Escape(texts.Summary)}",
            $"DESCRIPTION:{Escape(texts.Description)}",
            "TRANSP:TRANSPARENT",
        };

        // Order matters: iOS Calendar keeps only the first two alarms.
        Alarm(lines, "PT0S", texts.EndAlarm);
        if (settings.HeadsUp) Alarm(lines, $"-PT{ShiftSettings.HeadsUpMinutes}M", texts.HeadsUpAlarm);
        if (settings.Repeat)
        {
            Alarm(lines, $"PT{ShiftSettings.RepeatMinutes}M", texts.RepeatAlarm);
            Alarm(lines, $"PT{ShiftSettings.RepeatMinutes * 2}M", texts.RepeatAlarm);
        }

        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");

        var sb = new StringBuilder();
        foreach (var line in lines) sb.Append(Fold(line)).Append("\r\n");
        return sb.ToString();
    }

    private static void Alarm(List<string> lines, string trigger, string text)
    {
        lines.Add("BEGIN:VALARM");
        lines.Add("ACTION:DISPLAY");
        lines.Add($"DESCRIPTION:{Escape(text)}");
        lines.Add($"TRIGGER:{trigger}");
        lines.Add("END:VALARM");
    }

    private static string Utc(long ms) =>
        DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    public static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    /// <summary>Folds a content line at 75 octets (UTF-8), never inside a multi-byte character.</summary>
    public static string Fold(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= 75) return line;
        var sb = new StringBuilder();
        var count = 0;
        var limit = 75;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (count + size > limit)
            {
                sb.Append("\r\n ");
                count = 0;
                limit = 74; // the leading space counts
            }
            sb.Append(rune.ToString());
            count += size;
        }
        return sb.ToString();
    }
}
