using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using MAD_FINAL.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;

namespace MAD_FINAL.ViewModels;

public sealed class MainPageViewModel : INotifyPropertyChanged
{
    private readonly ITrackingDatabase _database;
    private bool _isInitialized;
    private bool _isBusy;
    private Location? _currentLocation;
    private Location? _destinationLocation;
    private string _latitude = "—";
    private string _longitude = "—";
    private string _connectivityStatus = "Unknown";
    private string _tripId = string.Empty;
    private string _tripIdError = string.Empty;
    private bool _hasTripIdError;
    private string _sensorError = string.Empty;
    private bool _hasSensorError;
    private string _saveStatus = string.Empty;
    private bool _hasSaveStatus;
    private string _selectedLatitude = "—";
    private string _selectedLongitude = "—";
    private string _destinationStatus = "Tap the map to choose a destination";

    public MainPageViewModel(ITrackingDatabase database)
    {
        _database = database;
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
        StartNavigationCommand = new Command(async () => await StartNavigationAsync(), () => !IsBusy);
        CenterOnCurrentCommand = new Command(async () => await CenterOnCurrentAsync(), () => !IsBusy);
        GoToHistoryCommand = new Command(async () => await GoToHistoryAsync(), () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SaveCommand { get; }
    public ICommand StartNavigationCommand { get; }
    public ICommand CenterOnCurrentCommand { get; }
    public ICommand GoToHistoryCommand { get; }

    public event Action<Location>? CenterMapRequested;
    public event Action<Location>? CurrentPinRequested;
    public event Action<Location>? DestinationPinRequested;

    public string MapHtml => GoogleMapsHtml.Build(GoogleMapsConfig.ApiKey);

    public string MapHint => "Tap anywhere on the map to set a destination pin.";

    public string Latitude
    {
        get => _latitude;
        private set
        {
            if (!SetProperty(ref _latitude, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentLatLong));
        }
    }

    public string Longitude
    {
        get => _longitude;
        private set
        {
            if (!SetProperty(ref _longitude, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentLatLong));
        }
    }

    public string ConnectivityStatus
    {
        get => _connectivityStatus;
        private set => SetProperty(ref _connectivityStatus, value);
    }

    public string TripId
    {
        get => _tripId;
        set
        {
            if (!SetProperty(ref _tripId, value))
            {
                return;
            }

            if (HasTripIdError)
            {
                ValidateTripId();
            }
        }
    }

    public string TripIdError
    {
        get => _tripIdError;
        private set => SetProperty(ref _tripIdError, value);
    }

    public bool HasTripIdError
    {
        get => _hasTripIdError;
        private set => SetProperty(ref _hasTripIdError, value);
    }

    public string SensorError
    {
        get => _sensorError;
        private set => SetProperty(ref _sensorError, value);
    }

    public bool HasSensorError
    {
        get => _hasSensorError;
        private set => SetProperty(ref _hasSensorError, value);
    }

    public string SaveStatus
    {
        get => _saveStatus;
        private set => SetProperty(ref _saveStatus, value);
    }

    public bool HasSaveStatus
    {
        get => _hasSaveStatus;
        private set => SetProperty(ref _hasSaveStatus, value);
    }

    public string CurrentLatLong => $"{Latitude}, {Longitude}";

    public string SelectedLatLong => $"{_selectedLatitude}, {_selectedLongitude}";

    public string DestinationStatus
    {
        get => _destinationStatus;
        private set => SetProperty(ref _destinationStatus, value);
    }

    public Location? CurrentLocation => _currentLocation;
    public Location? DestinationLocation => _destinationLocation;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            (SaveCommand as Command)?.ChangeCanExecute();
            (StartNavigationCommand as Command)?.ChangeCanExecute();
            (CenterOnCurrentCommand as Command)?.ChangeCanExecute();
            (GoToHistoryCommand as Command)?.ChangeCanExecute();
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        UpdateConnectivityStatus();
        Connectivity.ConnectivityChanged += OnConnectivityChanged;

        await RefreshLocationAsync();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        UpdateConnectivityStatus();
    }

    private void UpdateConnectivityStatus()
    {
        var access = Connectivity.Current.NetworkAccess;
        var profiles = Connectivity.Current.ConnectionProfiles;

        var profileList = profiles?.ToList() ?? new List<ConnectionProfile>();
        var profileText = profileList.Count > 0 ? string.Join(", ", profileList) : "None";
        ConnectivityStatus = $"{access} ({profileText})";
    }

    private async Task RefreshLocationAsync()
    {
        try
        {
            SensorError = string.Empty;
            HasSensorError = false;
            HasSaveStatus = false;

            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (permission != PermissionStatus.Granted)
            {
                SensorError = "Location permission denied. Enable it in Settings to capture GPS coordinates.";
                HasSensorError = true;
                return;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            _currentLocation = await Geolocation.GetLocationAsync(request);

            _currentLocation ??= await Geolocation.GetLastKnownLocationAsync();

            if (_currentLocation is null)
            {
                SensorError = "Unable to retrieve location. Make sure GPS is enabled.";
                HasSensorError = true;
                return;
            }

            Latitude = _currentLocation.Latitude.ToString("0.000000");
            Longitude = _currentLocation.Longitude.ToString("0.000000");

            CurrentPinRequested?.Invoke(_currentLocation);
            CenterMapRequested?.Invoke(_currentLocation);
        }
        catch (FeatureNotEnabledException)
        {
            SensorError = "Location services are disabled. Please turn on GPS.";
            HasSensorError = true;
        }
        catch (PermissionException)
        {
            SensorError = "Location permission is not granted.";
            HasSensorError = true;
        }
        catch (Exception ex)
        {
            SensorError = $"Location error: {ex.Message}";
            HasSensorError = true;
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        SaveStatus = string.Empty;
        HasSaveStatus = false;

        if (!ValidateTripId())
        {
            return;
        }

        var currentData = _currentLocation is null
            ? string.Empty
            : $"{_currentLocation.Latitude:0.000000},{_currentLocation.Longitude:0.000000}";

        var destinationData = _destinationLocation is null
            ? string.Empty
            : $"{_destinationLocation.Latitude:0.000000},{_destinationLocation.Longitude:0.000000}";

        var locationData = $"Current:{currentData}; Destination:{destinationData}";

        if (string.IsNullOrWhiteSpace(currentData))
        {
            SensorError = "No location available to save yet. Please allow GPS and try again.";
            HasSensorError = true;
            return;
        }

        try
        {
            IsBusy = true;
            await _database.SaveTripSnapshotAsync(TripId.Trim(), locationData);
            SaveStatus = $"Saved at {DateTime.Now:HH:mm:ss}";
            HasSaveStatus = true;
        }
        catch (Exception ex)
        {
            SaveStatus = $"Save failed: {ex.Message}";
            HasSaveStatus = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SetDestination(Location location)
    {
        _destinationLocation = location;
        _selectedLatitude = location.Latitude.ToString("0.000000");
        _selectedLongitude = location.Longitude.ToString("0.000000");
        DestinationStatus = "Destination selected";
        OnPropertyChanged(nameof(SelectedLatLong));
        DestinationPinRequested?.Invoke(location);
        CenterMapRequested?.Invoke(location);
    }

    private async Task CenterOnCurrentAsync()
    {
        if (_currentLocation is null)
        {
            await RefreshLocationAsync();
        }

        if (_currentLocation is not null)
        {
            CenterMapRequested?.Invoke(_currentLocation);
        }
    }

    private async Task StartNavigationAsync()
    {
        HasSaveStatus = false;

        if (_destinationLocation is null)
        {
            SaveStatus = "Select a destination on the map first.";
            HasSaveStatus = true;
            return;
        }

        var lat = _destinationLocation.Latitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
        var lng = _destinationLocation.Longitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await Launcher.Default.OpenAsync($"google.navigation:q={lat},{lng}&mode=d");
                return;
            }

            await Launcher.Default.OpenAsync($"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}&travelmode=driving");
        }
        catch (Exception ex)
        {
            SaveStatus = $"Navigation failed: {ex.Message}";
            HasSaveStatus = true;
        }
    }

    private static Task GoToHistoryAsync()
    {
        return Shell.Current.GoToAsync("//history");
    }

    private bool ValidateTripId()
    {
        var value = TripId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            TripIdError = "Trip ID is required.";
            HasTripIdError = true;
            return false;
        }

        if (value.Length < 4)
        {
            TripIdError = "Trip ID must be at least 4 characters.";
            HasTripIdError = true;
            return false;
        }

        if (!Regex.IsMatch(value, "^[a-zA-Z0-9]+$"))
        {
            TripIdError = "Trip ID must be alphanumeric only.";
            HasTripIdError = true;
            return false;
        }

        TripIdError = string.Empty;
        HasTripIdError = false;
        return true;
    }

    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
