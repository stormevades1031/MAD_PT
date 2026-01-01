using MAD_FINAL.ViewModels;

namespace MAD_FINAL;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryPageViewModel _viewModel;

    public HistoryPage(HistoryPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}

