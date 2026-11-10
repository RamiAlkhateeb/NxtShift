using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace NxtShift.Web.Services;

/// <summary>
/// Keeps the browser's in-app notification timers in step with the shift. These only fire while the page is alive
/// (open, or recently backgrounded): a static web app has no way to notify once it is closed.
/// </summary>
public sealed class ReminderService(IJSRuntime js, ShiftStore store, ShiftText text, NavigationManager nav) : IAsyncDisposable
{
    private DotNetObjectReference<ReminderService>? _ref;
    // Rescheduled only when the state or language changes, not on clock ticks, so a timer that is about to
    // fire is never cancelled by a tick that already sees its moment as past.
    private (ShiftData? Data, string Language) _scheduledFor;

    public async Task StartAsync()
    {
        if (_ref is not null) return;
        _ref = DotNetObjectReference.Create(this);
        store.Changed += OnStoreChanged;
        await RescheduleAsync();
    }

    private async void OnStoreChanged()
    {
        try { await RescheduleAsync(); }
        catch (JSException) { /* notifications are best effort */ }
    }

    private async Task RescheduleAsync()
    {
        var key = (store.Data, text["app_name"]);
        if (Equals(key, _scheduledFor)) return;
        _scheduledFor = key;
        var upcoming = ShiftMath.UpcomingReminders(store.Data, store.NowMs);

        await js.InvokeVoidAsync("nxt.notify.cancelAll");
        foreach (var r in upcoming) await ScheduleAsync(r);
    }

    private async Task ScheduleAsync(ShiftMath.Reminder r)
    {
        if (store.Data.Current is not { } shift) return;
        var remind = store.Data.Settings.RemindAtEnd;
        // A null title schedules a sound-only timer (no notification).
        var (title, body, path) = r.Kind switch
        {
            ShiftMath.ReminderKind.Hour => ((string?)null, "", "running"),
            ShiftMath.ReminderKind.End when !remind => (null, "", "time-up"),
            ShiftMath.ReminderKind.HeadsUp => (text["notif_headsup_title"], text.T("notif_headsup_body", ShiftMath.Clock(shift.EndMs, store.Zone)), "running"),
            ShiftMath.ReminderKind.End => (text["notif_end_title"], text["notif_end_body"], "time-up"),
            _ => (text["notif_repeat_title"], text["notif_repeat_body"], "time-up"),
        };
        await js.InvokeVoidAsync("nxt.notify.schedule", r.Id, r.AtMs, title, body, "nxtshift", nav.ToAbsoluteUri(path).ToString(), _ref);
    }

    [JSInvokable]
    public async Task OnNotificationDue(string id)
    {
        var s = store.Data.Settings;
        var pattern = id switch
        {
            _ when id.StartsWith("hour-") => s.HourlySound && !IsStaleHourMark(id) ? "tick" : null,
            "end" => s.EndSound ? "done" : null,
            "repeat" or "snooze" => s.EndSound ? "tick" : null,
            _ => null,
        };
        if (pattern is not null)
        {
            try { await js.InvokeVoidAsync("nxt.chime", pattern); } catch (JSException) { }
        }

        // Queue the next repeat only. Rescheduling everything here would cancel timers that came due at the same
        // moment (after the phone wakes, or in a throttled background tab) before they fire, such as the end sound.
        if (id is "end" or "repeat" or "snooze")
        {
            foreach (var r in ShiftMath.UpcomingReminders(store.Data, store.NowMs).Where(r => r.Kind == ShiftMath.ReminderKind.Repeat))
            {
                try { await ScheduleAsync(r); } catch (JSException) { }
            }
        }
        store.NotifyTick();
    }

    /// <summary>An hour tick that fires more than 2 minutes late (the app was asleep) is skipped, so waking up doesn't play a backlog.</summary>
    private bool IsStaleHourMark(string id) =>
        store.Data.Current is { } shift
        && int.TryParse(id.AsSpan("hour-".Length), out var k)
        && store.NowMs - ShiftMath.HourMarkMs(shift, k) > 2 * 60_000L;

    public ValueTask DisposeAsync()
    {
        store.Changed -= OnStoreChanged;
        _ref?.Dispose();
        return ValueTask.CompletedTask;
    }
}
