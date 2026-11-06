namespace NxtShift;

/// <summary>What the user chose in Settings. Times are minutes.</summary>
public sealed record ShiftSettings
{
    public const int DefaultLengthMinutes = 8 * 60 + 30;
    public const int MinLengthMinutes = 60;
    public const int MaxLengthMinutes = 14 * 60;
    public const int LengthStepMinutes = 15;
    public const int HeadsUpMinutes = 10;
    public const int RepeatMinutes = 5;
    public const int SnoozeMinutes = 5;
    /// <summary>Repeats stop after this many, so a forgotten phone doesn't buzz all night.</summary>
    public const int MaxRepeats = 12;

    public int LengthMinutes { get; init; } = DefaultLengthMinutes;
    public bool RemindAtEnd { get; init; } = true;
    public bool HeadsUp { get; init; } = true;
    public bool Repeat { get; init; } = true;
    public bool Sound { get; init; }
}

/// <summary>The shift in progress. All instants are Unix epoch milliseconds (UTC), so DST and midnight are non-issues.</summary>
public sealed record ActiveShift
{
    /// <summary>Stable id: the instant of the punch-in tap.</summary>
    public long Id { get; init; }
    public long StartMs { get; init; }
    public int LengthMinutes { get; init; }
    /// <summary>"End shift now" was pressed: the shift is over even though its time isn't up.</summary>
    public bool EndedEarly { get; init; }
    public long? EndedEarlyAtMs { get; init; }
    /// <summary>"Remind me again in 5 min": the time-up screen waits until this instant.</summary>
    public long? SnoozedUntilMs { get; init; }

    public long EndMs => StartMs + LengthMinutes * 60_000L;
}

/// <summary>A finished shift, shown on the Done screen.</summary>
public sealed record CompletedShift(long StartMs, long PlannedEndMs, long PunchedOutMs)
{
    public long WorkedMs => Math.Max(0, PunchedOutMs - StartMs);
}

/// <summary>Everything the app persists, as one JSON document.</summary>
public sealed record ShiftData
{
    public bool Onboarded { get; init; }
    public ShiftSettings Settings { get; init; } = new();
    public ActiveShift? Current { get; init; }
    /// <summary>Set by "I've punched out" and cleared by "Back to home".</summary>
    public CompletedShift? Last { get; init; }
}

public enum ShiftScreen { Welcome, Home, Running, TimeUp, Done }
