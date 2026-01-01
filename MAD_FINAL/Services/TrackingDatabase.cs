using MAD_FINAL.Models;
using SQLite;

namespace MAD_FINAL.Services;

public sealed class TrackingDatabase : ITrackingDatabase
{
    private SQLiteAsyncConnection? _connection;
    private bool _isInitialized;

    public async Task SaveTripSnapshotAsync(string tripId, string locationData)
    {
        await EnsureInitializedAsync();

        var snapshot = new TripSnapshot
        {
            TripID = tripId,
            LocationData = locationData,
            SavedAtUtc = DateTime.UtcNow,
        };

        // SQLite Step 4: Insert Operation
        await _connection!.InsertAsync(snapshot);
    }

    public async Task<IReadOnlyList<TripSnapshot>> GetTripSnapshotsAsync()
    {
        await EnsureInitializedAsync();
        var list = await _connection!.Table<TripSnapshot>().OrderByDescending(x => x.Id).ToListAsync();
        return list;
    }

    public async Task DeleteTripSnapshotAsync(int id)
    {
        await EnsureInitializedAsync();
        await _connection!.DeleteAsync<TripSnapshot>(id);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        // SQLite Step 1: Database Path (local file location)
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tracking_management.db3");

        // SQLite Step 2: Connection (create SQLite connection)
        _connection = new SQLiteAsyncConnection(dbPath);

        // SQLite Step 3: Table Creation (create the table if it does not exist)
        await _connection.CreateTableAsync<TripSnapshot>();

        _isInitialized = true;
    }
}
