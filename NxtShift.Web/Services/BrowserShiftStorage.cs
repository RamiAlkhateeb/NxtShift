using Nxt.UI;

namespace NxtShift.Web.Services;

/// <summary>Keeps the whole app state as one JSON value in localStorage.</summary>
public sealed class BrowserShiftStorage(LocalStore store) : IShiftStorage
{
    public const string Key = "nxtshift.state";

    public Task<string?> LoadAsync() => store.GetAsync(Key);
    public Task SaveAsync(string json) => store.SetAsync(Key, json);
}
