namespace NxtShift.Tests;

/// <summary>A clock the test moves by hand, in a fixed zone (UTC+3, no DST).</summary>
public sealed class FakeTime : TimeProvider
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone("Test+3", TimeSpan.FromHours(3), "Test+3", "Test+3");

    public DateTimeOffset Now { get; set; } = new(2026, 11, 6, 8, 38, 0, TimeSpan.FromHours(3));

    public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone => Zone;

    public void Advance(TimeSpan by) => Now = Now.Add(by);
}

public sealed class MemoryStorage : IShiftStorage
{
    public string? Json { get; set; }
    public Task<string?> LoadAsync() => Task.FromResult(Json);
    public Task SaveAsync(string json) { Json = json; return Task.CompletedTask; }
}

public static class Ms
{
    public static long At(int hour, int minute, int day = 6) =>
        new DateTimeOffset(2026, 11, day, hour, minute, 0, TimeSpan.FromHours(3)).ToUnixTimeMilliseconds();

    public const long Minute = 60_000;
}
