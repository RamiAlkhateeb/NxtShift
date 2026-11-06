using Microsoft.JSInterop;

namespace NxtShift.Web.Services;

/// <summary>What this browser can do: notifications, installed to the Home Screen, iOS.</summary>
public sealed class DeviceService(IJSRuntime js)
{
    /// <summary>"granted" | "denied" | "default" | "unsupported"</summary>
    public async Task<string> NotificationPermissionAsync()
    {
        try { return await js.InvokeAsync<string>("nxt.notify.permission"); }
        catch (JSException) { return "unsupported"; }
    }

    public async Task<string> RequestNotificationsAsync()
    {
        try { return await js.InvokeAsync<string>("nxt.notify.request"); }
        catch (JSException) { return "unsupported"; }
    }

    public async Task<bool> IsIosAsync()
    {
        try { return await js.InvokeAsync<bool>("nxt.isIos"); } catch (JSException) { return false; }
    }

    public async Task<bool> IsStandaloneAsync()
    {
        try { return await js.InvokeAsync<bool>("nxt.isStandalone"); } catch (JSException) { return false; }
    }
}
