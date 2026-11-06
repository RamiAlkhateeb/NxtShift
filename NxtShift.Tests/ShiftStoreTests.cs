namespace NxtShift.Tests;

public class ShiftStoreTests
{
    private readonly FakeTime _time = new();
    private readonly MemoryStorage _storage = new();

    private async Task<ShiftStore> NewStoreAsync()
    {
        var store = new ShiftStore(_storage, _time);
        await store.LoadAsync();
        return store;
    }

    [Fact]
    public async Task Full_day_walks_through_every_screen()
    {
        var store = await NewStoreAsync();
        Assert.Equal(ShiftScreen.Welcome, store.Screen);

        await store.CompleteOnboardingAsync();
        Assert.Equal(ShiftScreen.Home, store.Screen);

        await store.PunchInAsync();
        Assert.Equal(ShiftScreen.Running, store.Screen);
        Assert.Equal(Ms.At(17, 8), store.Data.Current!.EndMs);

        _time.Advance(TimeSpan.FromHours(8.5));
        Assert.Equal(ShiftScreen.TimeUp, store.Screen);

        await store.SnoozeAsync();
        Assert.Equal(ShiftScreen.Running, store.Screen);
        _time.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(ShiftScreen.TimeUp, store.Screen);

        await store.PunchOutAsync();
        Assert.Equal(ShiftScreen.Done, store.Screen);
        Assert.Equal(Ms.At(8, 38), store.Data.Last!.StartMs);
        Assert.Equal(Ms.At(17, 13), store.Data.Last.PunchedOutMs);

        await store.BackToHomeAsync();
        Assert.Equal(ShiftScreen.Home, store.Screen);
    }

    [Fact]
    public async Task State_survives_a_reload()
    {
        var store = await NewStoreAsync();
        await store.PunchInAsync(Ms.At(8, 0));

        var reloaded = await NewStoreAsync();
        Assert.Equal(Ms.At(8, 0), reloaded.Data.Current!.StartMs);
        Assert.True(reloaded.Data.Onboarded);
    }

    [Fact]
    public async Task Corrupt_storage_starts_fresh()
    {
        _storage.Json = "{not json";
        var store = await NewStoreAsync();
        Assert.Equal(ShiftScreen.Welcome, store.Screen);
    }

    [Fact]
    public async Task Start_can_never_be_in_the_future()
    {
        var store = await NewStoreAsync();
        await store.PunchInAsync(Ms.At(10, 0));
        Assert.Equal(Ms.At(8, 38), store.Data.Current!.StartMs);
    }

    [Fact]
    public async Task Fixing_the_start_moves_the_shift_and_clears_a_snooze()
    {
        var store = await NewStoreAsync();
        await store.PunchInAsync();
        _time.Advance(TimeSpan.FromHours(8.5));
        await store.SnoozeAsync();
        Assert.NotNull(store.Data.Current!.SnoozedUntilMs);

        await store.FixStartAsync(Ms.At(8, 0));
        Assert.Equal(Ms.At(8, 0), store.Data.Current!.StartMs);
        Assert.Null(store.Data.Current.SnoozedUntilMs);
        Assert.Equal(Ms.At(8, 38), store.Data.Current.Id);
        Assert.Equal(ShiftScreen.TimeUp, store.Screen);
    }

    [Fact]
    public async Task End_now_goes_to_time_up_and_freezes_worked_time()
    {
        var store = await NewStoreAsync();
        await store.PunchInAsync();
        _time.Advance(TimeSpan.FromHours(2));
        await store.EndNowAsync();
        Assert.Equal(ShiftScreen.TimeUp, store.Screen);

        _time.Advance(TimeSpan.FromHours(1));
        Assert.Equal(2 * 60 * Ms.Minute, ShiftMath.WorkedMs(store.Data.Current!, store.NowMs));
    }

    [Fact]
    public async Task Shift_length_setting_applies_to_the_running_shift_and_is_clamped()
    {
        var store = await NewStoreAsync();
        await store.PunchInAsync();
        await store.UpdateSettingsAsync(s => s with { LengthMinutes = 480 });
        Assert.Equal(480, store.Data.Current!.LengthMinutes);

        await store.UpdateSettingsAsync(s => s with { LengthMinutes = 5 });
        Assert.Equal(ShiftSettings.MinLengthMinutes, store.Data.Settings.LengthMinutes);
    }
}
