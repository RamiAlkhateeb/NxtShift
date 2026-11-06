using System.Text.Json;

namespace NxtShift;

/// <summary>Where the store keeps its JSON (localStorage in the app, memory in tests).</summary>
public interface IShiftStorage
{
    Task<string?> LoadAsync();
    Task SaveAsync(string json);
}

/// <summary>
/// The app's state and every transition between screens. Each change is saved at once, so closing the app
/// or the phone killing it never loses a running shift.
/// </summary>
public sealed class ShiftStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IShiftStorage _storage;
    private readonly TimeProvider _time;

    public ShiftStore(IShiftStorage storage, TimeProvider time)
    {
        _storage = storage;
        _time = time;
    }

    public ShiftData Data { get; private set; } = new();
    public bool Loaded { get; private set; }
    public event Action? Changed;

    public long NowMs => _time.GetUtcNow().ToUnixTimeMilliseconds();
    public DateTimeOffset NowLocal => TimeZoneInfo.ConvertTime(_time.GetUtcNow(), _time.LocalTimeZone);
    public TimeZoneInfo Zone => _time.LocalTimeZone;
    public ShiftScreen Screen => ShiftMath.ScreenFor(Data, NowMs);

    public async Task LoadAsync()
    {
        if (Loaded) return;
        try
        {
            var json = await _storage.LoadAsync();
            if (!string.IsNullOrWhiteSpace(json)) Data = JsonSerializer.Deserialize<ShiftData>(json, Json) ?? new();
        }
        catch (JsonException) { Data = new(); }
        Loaded = true;
        Changed?.Invoke();
    }

    public Task CompleteOnboardingAsync() => SaveAsync(Data with { Onboarded = true });

    /// <summary>Starts a shift now, or at <paramref name="startMs"/> when the user started earlier.</summary>
    public Task PunchInAsync(long? startMs = null)
    {
        var now = NowMs;
        var start = Math.Min(startMs ?? now, now);
        return SaveAsync(Data with
        {
            Onboarded = true,
            Last = null,
            Current = new ActiveShift { Id = now, StartMs = start, LengthMinutes = Data.Settings.LengthMinutes }
        });
    }

    public Task FixStartAsync(long startMs) => Data.Current is { } shift
        ? SaveAsync(Data with
        {
            Current = shift with
            {
                StartMs = Math.Min(startMs, NowMs),
                SnoozedUntilMs = null
            }
        })
        : Task.CompletedTask;

    public Task EndNowAsync() => Data.Current is { } shift
        ? SaveAsync(Data with { Current = shift with { EndedEarly = true, EndedEarlyAtMs = NowMs, SnoozedUntilMs = null } })
        : Task.CompletedTask;

    public Task SnoozeAsync() => Data.Current is { } shift
        ? SaveAsync(Data with { Current = shift with { SnoozedUntilMs = NowMs + ShiftSettings.SnoozeMinutes * 60_000L } })
        : Task.CompletedTask;

    public Task PunchOutAsync()
    {
        if (Data.Current is not { } shift) return Task.CompletedTask;
        var now = NowMs;
        return SaveAsync(Data with { Current = null, Last = new CompletedShift(shift.StartMs, shift.EndMs, now) });
    }

    public Task BackToHomeAsync() => SaveAsync(Data with { Last = null });

    /// <summary>Changes settings. A new shift length also applies to the running shift.</summary>
    public Task UpdateSettingsAsync(Func<ShiftSettings, ShiftSettings> change)
    {
        var settings = change(Data.Settings);
        settings = settings with
        {
            LengthMinutes = Math.Clamp(settings.LengthMinutes, ShiftSettings.MinLengthMinutes, ShiftSettings.MaxLengthMinutes)
        };
        var current = Data.Current;
        if (current is not null && current.LengthMinutes != settings.LengthMinutes && !current.EndedEarly)
            current = current with { LengthMinutes = settings.LengthMinutes };
        return SaveAsync(Data with { Settings = settings, Current = current });
    }

    /// <summary>Tells listeners the clock moved (no state change, but the screen may need to switch).</summary>
    public void NotifyTick() => Changed?.Invoke();

    private async Task SaveAsync(ShiftData data)
    {
        Data = data;
        await _storage.SaveAsync(JsonSerializer.Serialize(data, Json));
        Changed?.Invoke();
    }
}
