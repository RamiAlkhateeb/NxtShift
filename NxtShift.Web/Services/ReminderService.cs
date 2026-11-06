using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace NxtShift.Web.Services;

/// <summary>
/// Keeps the browser's in-app notification timers in step with the shift. These only fire while the page is alive;
/// the calendar reminder (<see cref="CalendarService"/>) covers the closed-app case.
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
        if (store.Data.Current is not { } shift) return;

        foreach (var r in upcoming)
        {
            var (title, body, path) = r.Kind switch
            {
                ShiftMath.ReminderKind.HeadsUp => (text["notif_headsup_title"], text.T("notif_headsup_body", ShiftMath.Clock(shift.EndMs, store.Zone)), "running"),
                ShiftMath.ReminderKind.End => (text["notif_end_title"], text["notif_end_body"], "time-up"),
                _ => (text["notif_repeat_title"], text["notif_repeat_body"], "time-up"),
            };
            await js.InvokeVoidAsync("nxt.notify.schedule", r.Id, r.AtMs, title, body, "nxtshift", nav.ToAbsoluteUri(path).ToString(), _ref);
        }
    }

    [JSInvokable]
    public async Task OnNotificationDue(string id)
    {
        if (id != "headsup" && store.Data.Settings.Sound)
        {
            try { await js.InvokeVoidAsync("nxt.chime"); } catch (JSException) { }
        }
        _scheduledFor = default; // schedule the next repeat
        await RescheduleAsync();
        store.NotifyTick();
    }

    public ValueTask DisposeAsync()
    {
        store.Changed -= OnStoreChanged;
        _ref?.Dispose();
        return ValueTask.CompletedTask;
    }
}
