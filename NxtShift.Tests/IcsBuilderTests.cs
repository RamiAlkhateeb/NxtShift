using System.Text;

namespace NxtShift.Tests;

public class IcsBuilderTests
{
    private static readonly IcsTexts Texts = new("Punch out", "Started 08:38, 8h 30m", "10 minutes left", "Time to punch out", "Still on the clock");
    private static readonly ActiveShift Shift = new() { Id = 42, StartMs = Ms.At(8, 38), LengthMinutes = 510, CalendarSequence = 2 };

    [Fact]
    public void Event_sits_at_the_shift_end_with_a_stable_uid()
    {
        var ics = IcsBuilder.Build(Shift, new ShiftSettings(), Texts, Ms.At(8, 40));
        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.Contains("UID:shift-42@nxtshift\r\n", ics);
        Assert.Contains("SEQUENCE:2\r\n", ics);
        Assert.Contains("DTSTART:20261106T140800Z\r\n", ics); // 17:08 at UTC+3
        Assert.Contains("DTEND:20261106T141300Z\r\n", ics);
        Assert.DoesNotContain("\n", ics.Replace("\r\n", ""));
    }

    [Fact]
    public void Alarms_follow_settings_with_the_end_alarm_first()
    {
        var all = IcsBuilder.Build(Shift, new ShiftSettings(), Texts, 0);
        var triggers = all.Split("\r\n").Where(l => l.StartsWith("TRIGGER:")).ToArray();
        Assert.Equal(["TRIGGER:PT0S", "TRIGGER:-PT10M", "TRIGGER:PT5M", "TRIGGER:PT10M"], triggers);

        var minimal = IcsBuilder.Build(Shift, new ShiftSettings { HeadsUp = false, Repeat = false }, Texts, 0);
        Assert.Equal(["TRIGGER:PT0S"], minimal.Split("\r\n").Where(l => l.StartsWith("TRIGGER:")).ToArray());
    }

    [Fact]
    public void Text_is_escaped()
    {
        Assert.Equal(@"a\, b\; c\\ d\ne", IcsBuilder.Escape("a, b; c\\ d\ne"));
    }

    [Fact]
    public void Long_lines_fold_at_75_octets_without_splitting_characters()
    {
        var line = "SUMMARY:" + string.Concat(Enumerable.Repeat("اختم خروجك الآن ", 8));
        var folded = IcsBuilder.Fold(line);
        var parts = folded.Split("\r\n");
        Assert.True(parts.Length > 1);
        Assert.All(parts, p => Assert.True(Encoding.UTF8.GetByteCount(p) <= 75));
        Assert.All(parts.Skip(1), p => Assert.StartsWith(" ", p));
        Assert.Equal(line, string.Concat(parts.Select((p, i) => i == 0 ? p : p[1..])));
    }
}
