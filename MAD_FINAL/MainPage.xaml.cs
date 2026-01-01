using MAD_FINAL.ViewModels;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;

namespace MAD_FINAL
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;
        private bool _isMapReady;
        private bool _isBottomSheetReady;
        private double _bottomSheetMaxTranslate;
        private double _bottomSheetPanStart;
        private bool _bottomSheetExpanded;

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            _viewModel.CenterMapRequested += OnCenterMapRequested;
            _viewModel.CurrentPinRequested += OnCurrentPinRequested;
            _viewModel.DestinationPinRequested += OnDestinationPinRequested;

            SizeChanged += OnPageSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }

        private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
        {
            _isMapReady = true;

            if (_viewModel.CurrentLocation is not null)
            {
                await SetCurrentAsync(_viewModel.CurrentLocation);
            }

            if (_viewModel.DestinationLocation is not null)
            {
                await SetDestinationAsync(_viewModel.DestinationLocation);
            }
        }

        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri))
            {
                return;
            }

            if (!string.Equals(uri.Scheme, "app", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, "mapclick", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            e.Cancel = true;

            var query = ParseQuery(uri.Query);
            if (!query.TryGetValue("lat", out var latText) || !query.TryGetValue("lng", out var lngText))
            {
                return;
            }

            if (!double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(lngText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                return;
            }

            _viewModel.SetDestination(new Location(lat, lng));
        }

        private async void OnCenterMapRequested(Location location)
        {
            if (!_isMapReady)
            {
                return;
            }

            await CenterMapAsync(location, 15);
        }

        private async void OnCurrentPinRequested(Location location)
        {
            if (!_isMapReady)
            {
                return;
            }

            await SetCurrentAsync(location);
        }

        private async void OnDestinationPinRequested(Location location)
        {
            if (!_isMapReady)
            {
                return;
            }

            await SetDestinationAsync(location);
        }

        private Task CenterMapAsync(Location location, int zoom)
        {
            var lat = location.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
            var lng = location.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
            return MapWebView.EvaluateJavaScriptAsync($"centerMap({lat}, {lng}, {zoom});");
        }

        private Task SetCurrentAsync(Location location)
        {
            var lat = location.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
            var lng = location.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
            return MapWebView.EvaluateJavaScriptAsync($"setCurrent({lat}, {lng});");
        }

        private Task SetDestinationAsync(Location location)
        {
            var lat = location.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
            var lng = location.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
            return MapWebView.EvaluateJavaScriptAsync($"setDestination({lat}, {lng});");
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            var trimmed = query.StartsWith("?") ? query[1..] : query;
            var pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var idx = pair.IndexOf('=');
                if (idx <= 0 || idx == pair.Length - 1)
                {
                    continue;
                }

                var key = Uri.UnescapeDataString(pair[..idx]);
                var value = Uri.UnescapeDataString(pair[(idx + 1)..]);
                result[key] = value;
            }

            return result;
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            var expandedHeight = Math.Min(560, Height * 0.72);
            var collapsedVisible = 150.0;

            expandedHeight = Math.Max(expandedHeight, collapsedVisible + 120);
            expandedHeight = Math.Min(expandedHeight, Height - 120);

            BottomSheet.HeightRequest = expandedHeight;
            _bottomSheetMaxTranslate = Math.Max(0, expandedHeight - collapsedVisible);

            if (!_isBottomSheetReady)
            {
                BottomSheet.TranslationY = _bottomSheetMaxTranslate;
                _bottomSheetExpanded = false;
                _isBottomSheetReady = true;
                return;
            }

            BottomSheet.TranslationY = Math.Clamp(BottomSheet.TranslationY, 0, _bottomSheetMaxTranslate);
        }

        private void OnBottomSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (!_isBottomSheetReady)
            {
                return;
            }

            if (e.StatusType == GestureStatus.Started)
            {
                _bottomSheetPanStart = BottomSheet.TranslationY;
                return;
            }

            if (e.StatusType == GestureStatus.Running)
            {
                var next = _bottomSheetPanStart + e.TotalY;
                BottomSheet.TranslationY = Math.Clamp(next, 0, _bottomSheetMaxTranslate);
                return;
            }

            if (e.StatusType is GestureStatus.Canceled or GestureStatus.Completed)
            {
                var shouldCollapse = BottomSheet.TranslationY > _bottomSheetMaxTranslate * 0.5;
                SnapBottomSheet(!shouldCollapse);
            }
        }

        private void OnBottomSheetHandleTapped(object? sender, EventArgs e)
        {
            if (!_isBottomSheetReady)
            {
                return;
            }

            SnapBottomSheet(!_bottomSheetExpanded);
        }

        private void SnapBottomSheet(bool expand)
        {
            _bottomSheetExpanded = expand;
            var target = expand ? 0 : _bottomSheetMaxTranslate;

            BottomSheet.AbortAnimation("BottomSheetSnap");
            BottomSheet.Animate(
                "BottomSheetSnap",
                callback: v => BottomSheet.TranslationY = v,
                start: BottomSheet.TranslationY,
                end: target,
                rate: 16,
                length: 220,
                easing: Easing.CubicOut);
        }
    }
}
