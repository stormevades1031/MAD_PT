using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MAD_FINAL.Models;
using MAD_FINAL.Services;

namespace MAD_FINAL.ViewModels;

public sealed class HistoryPageViewModel : INotifyPropertyChanged
{
    private readonly ITrackingDatabase _database;
    private bool _isBusy;
    private string _status = string.Empty;

    public HistoryPageViewModel(ITrackingDatabase database)
    {
        _database = database;
        RefreshCommand = new Command(async () => await RefreshAsync(), () => !IsBusy);
        DeleteCommand = new Command<TripSnapshot>(async item => await DeleteAsync(item), item => item is not null && !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TripSnapshot> Items { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            (RefreshCommand as Command)?.ChangeCanExecute();
            (DeleteCommand as Command)?.ChangeCanExecute();
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            Status = string.Empty;

            var list = await _database.GetTripSnapshotsAsync();

            Items.Clear();
            foreach (var item in list)
            {
                Items.Add(item);
            }

            Status = Items.Count == 0 ? "No saved trips yet." : $"Loaded {Items.Count} record(s).";
        }
        catch (Exception ex)
        {
            Status = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync(TripSnapshot? item)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _database.DeleteTripSnapshotAsync(item.Id);
            Items.Remove(item);
            Status = $"Deleted Trip ID: {item.TripID}";
        }
        catch (Exception ex)
        {
            Status = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

