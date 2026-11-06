using Microsoft.JSInterop;
using Nxt.UI;

namespace NxtShift.Web.Services;

/// <summary>Hands the punch-out reminder to the phone's calendar as an .ics file.</summary>
public sealed class CalendarService(IJSRuntime js, ShiftStore store, ShiftText text, ToastService toast)
{
    /// <summary>Set after punching in when the user wants to be offered the calendar reminder; the Running screen shows the sheet once.</summary>
    public bool OfferPending { get; set; }

    public async Task AddAsync()
    {
        if (store.Data.Current is not { } shift) return;
        var texts = new IcsTexts(
            text["ics_summary"],
            text.T("ics_description", ShiftMath.Clock(shift.StartMs, store.Zone), text.Duration(shift.LengthMinutes)),
            text["ics_alarm_headsup"],
            text["ics_alarm_end"],
            text["ics_alarm_repeat"]);
        var ics = IcsBuilder.Build(shift, store.Data.Settings, texts, store.NowMs);

        string result;
        try { result = await js.InvokeAsync<string>("nxt.download", IcsBuilder.FileName, IcsBuilder.MimeType, ics); }
        catch (JSException) { result = "failed"; }

        switch (result)
        {
            case "cancelled":
                return;
            case "failed":
                toast.Show(text["calendar_failed"], isError: true);
                return;
            case "downloaded":
                toast.Show(text["calendar_downloaded"]);
                break;
        }
        await store.MarkCalendarAddedAsync();
    }
}
