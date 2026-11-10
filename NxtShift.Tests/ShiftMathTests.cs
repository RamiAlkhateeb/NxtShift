namespace NxtShift.Tests;

public class ShiftMathTests
{
    private static ActiveShift Shift(long start, int minutes = 510) => new() { Id = start, StartMs = start, LengthMinutes = minutes };

    [Fact]
    public void End_is_start_plus_length()
    {
        var shift = Shift(Ms.At(8, 38));
        Assert.Equal(Ms.At(17, 8), shift.EndMs);
        Assert.Equal("17:08", ShiftMath.Clock(shift.EndMs, FakeTime.Zone));
        Assert.Equal("16:58", ShiftMath.Clock(ShiftMath.HeadsUpMs(shift), FakeTime.Zone));
    }

    [Fact]
    public void Countdown_worked_and_progress_come_from_timestamps()
    {
        var shift = Shift(Ms.At(8, 38));
        var now = Ms.At(11, 34) + 12_000 - 24 * 1000; // 11:33:48 → 5:34:12 left
        Assert.Equal("5:34:12", ShiftMath.Countdown(ShiftMath.RemainingMs(shift, now)));
        Assert.Equal("2:55", ShiftMath.HoursMinutes(ShiftMath.WorkedMs(shift, now)));
        Assert.InRange(ShiftMath.Progress(shift, now), 0.34, 0.35);
    }

    [Fact]
    public void Remaining_and_progress_clamp_after_the_end()
    {
        var shift = Shift(Ms.At(8, 0), 60);
        var later = Ms.At(10, 0);
        Assert.Equal(0, ShiftMath.RemainingMs(shift, later));
        Assert.Equal(1, ShiftMath.Progress(shift, later));
        Assert.Equal(60 * Ms.Minute, ShiftMath.OvertimeMs(shift, later));
    }

    [Fact]
    public void Shift_can_cross_midnight()
    {
        var shift = Shift(Ms.At(22, 0, day: 6));
        Assert.Equal(Ms.At(6, 30, day: 7), shift.EndMs);
        Assert.Equal("06:30", ShiftMath.Clock(shift.EndMs, FakeTime.Zone));
    }

    [Fact]
    public void Picked_time_in_the_future_means_yesterday()
    {
        var now = new DateTimeOffset(2026, 11, 6, 1, 0, 0, TimeSpan.FromHours(3));
        Assert.Equal(Ms.At(22, 0, day: 5), ShiftMath.ResolveStart(new TimeOnly(22, 0), now));
        Assert.Equal(Ms.At(0, 30, day: 6), ShiftMath.ResolveStart(new TimeOnly(0, 30), now));
    }

    [Theory]
    [InlineData(510, "8:30")]
    [InlineData(60, "1:00")]
    [InlineData(845, "14:05")]
    public void Formats_hours_and_minutes(int minutes, string expected) => Assert.Equal(expected, ShiftMath.HoursMinutes(minutes));

    [Fact]
    public void Screen_follows_the_state()
    {
        var now = Ms.At(12, 0);
        Assert.Equal(ShiftScreen.Welcome, ShiftMath.ScreenFor(new ShiftData(), now));
        Assert.Equal(ShiftScreen.Home, ShiftMath.ScreenFor(new ShiftData { Onboarded = true }, now));

        var running = new ShiftData { Onboarded = true, Current = Shift(Ms.At(8, 38)) };
        Assert.Equal(ShiftScreen.Running, ShiftMath.ScreenFor(running, now));
        Assert.Equal(ShiftScreen.TimeUp, ShiftMath.ScreenFor(running, Ms.At(17, 8)));

        var snoozed = running with { Current = running.Current! with { SnoozedUntilMs = Ms.At(17, 13) } };
        Assert.Equal(ShiftScreen.Running, ShiftMath.ScreenFor(snoozed, Ms.At(17, 10)));
        Assert.Equal(ShiftScreen.TimeUp, ShiftMath.ScreenFor(snoozed, Ms.At(17, 13)));

        var early = running with { Current = running.Current! with { EndedEarly = true } };
        Assert.Equal(ShiftScreen.TimeUp, ShiftMath.ScreenFor(early, now));

        var done = new ShiftData { Onboarded = true, Last = new CompletedShift(1, 2, 3) };
        Assert.Equal(ShiftScreen.Done, ShiftMath.ScreenFor(done, now));
    }

    [Fact]
    public void Reminders_are_heads_up_end_and_first_repeat()
    {
        var data = new ShiftData { Current = Shift(Ms.At(8, 38)), Settings = new() { HourlySound = false } };
        var list = ShiftMath.UpcomingReminders(data, Ms.At(9, 0));
        Assert.Equal(
            [("headsup", Ms.At(16, 58)), ("end", Ms.At(17, 8)), ("repeat", Ms.At(17, 13))],
            list.Select(r => (r.Id, r.AtMs)).ToArray());
    }

    [Fact]
    public void After_the_end_only_the_next_repeat_is_left_and_repeats_stop_at_the_cap()
    {
        var data = new ShiftData { Current = Shift(Ms.At(8, 38)), Settings = new() { HourlySound = false } };
        var next = Assert.Single(ShiftMath.UpcomingReminders(data, Ms.At(17, 14)));
        Assert.Equal(Ms.At(17, 18), next.AtMs);

        var muchLater = Ms.At(17, 8) + (ShiftSettings.MaxRepeats * ShiftSettings.RepeatMinutes + 1) * Ms.Minute;
        Assert.Empty(ShiftMath.UpcomingReminders(data, muchLater));
    }

    [Fact]
    public void Settings_switch_reminders_off()
    {
        var shift = Shift(Ms.At(8, 38));
        var off = new ShiftData { Current = shift, Settings = new() { RemindAtEnd = false, EndSound = false, HourlySound = false } };
        Assert.Empty(ShiftMath.UpcomingReminders(off, Ms.At(9, 0)));

        var noExtras = new ShiftData { Current = shift, Settings = new() { HeadsUp = false, Repeat = false, HourlySound = false } };
        Assert.Equal(["end"], ShiftMath.UpcomingReminders(noExtras, Ms.At(9, 0)).Select(r => r.Id));
    }

    [Fact]
    public void Snooze_replaces_other_reminders()
    {
        var shift = Shift(Ms.At(8, 38)) with { SnoozedUntilMs = Ms.At(17, 15) };
        var only = Assert.Single(ShiftMath.UpcomingReminders(new ShiftData { Current = shift }, Ms.At(17, 10)));
        Assert.Equal(ShiftMath.ReminderKind.Snooze, only.Kind);
    }

    [Fact]
    public void Hour_marks_are_each_full_hour_before_the_end()
    {
        var data = new ShiftData { Current = Shift(Ms.At(8, 38)) };
        var hours = ShiftMath.UpcomingReminders(data, Ms.At(8, 38)).Where(r => r.Kind == ShiftMath.ReminderKind.Hour).ToList();
        Assert.Equal(8, hours.Count);
        Assert.Equal(Ms.At(9, 38), hours[0].AtMs);
        Assert.Equal(Ms.At(16, 38), hours[^1].AtMs);
        Assert.Equal("hour-1", hours[0].Id);
    }

    [Fact]
    public void An_exact_shift_has_no_hour_mark_on_the_end()
    {
        var data = new ShiftData { Current = Shift(Ms.At(9, 0), 480) };
        var list = ShiftMath.UpcomingReminders(data, Ms.At(9, 0));
        Assert.Equal(7, list.Count(r => r.Kind == ShiftMath.ReminderKind.Hour));
        Assert.Single(list, r => r.Kind == ShiftMath.ReminderKind.End && r.AtMs == Ms.At(17, 0));
    }

    [Fact]
    public void Past_hour_marks_are_skipped_and_the_list_is_in_time_order()
    {
        var data = new ShiftData { Current = Shift(Ms.At(8, 38)) };
        var list = ShiftMath.UpcomingReminders(data, Ms.At(15, 0));
        Assert.Equal(["hour-7", "hour-8", "headsup", "end", "repeat"], list.Select(r => r.Id));
    }

    [Fact]
    public void No_hour_marks_when_off_ended_early_or_snoozed()
    {
        var shift = Shift(Ms.At(8, 38));
        Assert.DoesNotContain(ShiftMath.UpcomingReminders(new ShiftData { Current = shift, Settings = new() { HourlySound = false } }, Ms.At(9, 0)),
            r => r.Kind == ShiftMath.ReminderKind.Hour);
        Assert.Empty(ShiftMath.UpcomingReminders(new ShiftData { Current = shift with { EndedEarly = true } }, Ms.At(9, 0)));
        Assert.Equal(["snooze"], ShiftMath.UpcomingReminders(new ShiftData { Current = shift with { SnoozedUntilMs = Ms.At(17, 13) } }, Ms.At(17, 9)).Select(r => r.Id));
    }

    [Fact]
    public void End_sound_keeps_the_end_entry_with_reminders_off()
    {
        var data = new ShiftData { Current = Shift(Ms.At(8, 38)), Settings = new() { RemindAtEnd = false, HourlySound = false } };
        Assert.Equal(["end"], ShiftMath.UpcomingReminders(data, Ms.At(9, 0)).Select(r => r.Id));
    }

    [Fact]
    public void Next_hour_mark_is_shown_until_the_last_one()
    {
        var shift = Shift(Ms.At(8, 38));
        Assert.Equal(Ms.At(9, 38), ShiftMath.NextHourMarkMs(shift, Ms.At(8, 38)));
        Assert.Equal(Ms.At(11, 38), ShiftMath.NextHourMarkMs(shift, Ms.At(10, 40)));
        Assert.Null(ShiftMath.NextHourMarkMs(shift, Ms.At(16, 40)));
        Assert.Null(ShiftMath.NextHourMarkMs(shift, Ms.At(18, 0)));
    }
}
