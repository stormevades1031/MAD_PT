using SQLite;

namespace MAD_FINAL.Models;

public sealed class TripSnapshot
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string TripID { get; set; } = string.Empty;

    public string LocationData { get; set; } = string.Empty;

    public DateTime SavedAtUtc { get; set; }
}

