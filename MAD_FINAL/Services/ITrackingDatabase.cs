using MAD_FINAL.Models;

namespace MAD_FINAL.Services;

public interface ITrackingDatabase
{
    Task SaveTripSnapshotAsync(string tripId, string locationData);
    Task<IReadOnlyList<TripSnapshot>> GetTripSnapshotsAsync();
    Task DeleteTripSnapshotAsync(int id);
}
