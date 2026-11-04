namespace Thinktecture.TestDatabaseContext;

/// <summary>
/// Value object wrapping a <c>long</c>; mapped to <c>bigint</c> through a value converter.
/// </summary>
public readonly record struct TimestampValue(long Value);
