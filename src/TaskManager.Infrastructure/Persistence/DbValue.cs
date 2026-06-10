using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>Helpers for converting between CLR values and their SQLite TEXT representation.</summary>
internal static class DbValue
{
    private const string DateFormat = "O"; // ISO 8601 round-trip, preserves UTC kind.

    public static string FromGuid(Guid value) => value.ToString("D");

    public static string FromDate(DateTime value)
        => value.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture);

    public static object FromNullableDate(DateTime? value)
        => value.HasValue ? FromDate(value.Value) : DBNull.Value;

    public static object OrDbNull(string? value)
        => (object?)value ?? DBNull.Value;

    public static Guid ReadGuid(SqliteDataReader reader, int ordinal)
        => Guid.Parse(reader.GetString(ordinal));

    public static string? ReadNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    public static DateTime ReadDate(SqliteDataReader reader, int ordinal)
        => DateTime.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static DateTime? ReadNullableDate(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : ReadDate(reader, ordinal);
}
