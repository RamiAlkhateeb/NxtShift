using Microsoft.AspNetCore.Components;
using Nxt.UI;
using NxtShift.Web.Services;

namespace NxtShift.Web.Components;

/// <summary>Base for the screens: re-renders on every state change and clock tick.</summary>
public abstract class ScreenBase : ComponentBase, IDisposable
{
    [Inject] protected ShiftStore Store { get; set; } = default!;
    [Inject] protected ShiftText T { get; set; } = default!;
    [Inject] protected NxtText L { get; set; } = default!;
    [Inject] protected INxtLocale Locale { get; set; } = default!;

    protected long Now => Store.NowMs;
    protected string Clock(long ms) => ShiftMath.Clock(ms, Store.Zone);

    protected override void OnInitialized() => Store.Changed += Refresh;

    private void Refresh() => InvokeAsync(StateHasChanged);

    public virtual void Dispose() => Store.Changed -= Refresh;
}
