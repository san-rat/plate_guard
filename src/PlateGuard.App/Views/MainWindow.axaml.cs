using System;
using Avalonia.Controls;
using PlateGuard.App.ViewModels;
using PlateGuard.Core.Interfaces;

namespace PlateGuard.App.Views;

public partial class MainWindow : Window
{
    private readonly IPromotionUsageService? _promotionUsageService;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public MainWindow(IPromotionUsageService promotionUsageService) : this()
    {
        _promotionUsageService = promotionUsageService;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.AddUsageRequested -= OnAddUsageRequested;
        }

        base.OnClosed(e);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.AddUsageRequested -= OnAddUsageRequested;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
        {
            _viewModel.AddUsageRequested += OnAddUsageRequested;
        }
    }

    private async void OnAddUsageRequested(AddUsageDialogRequest request)
    {
        if (_promotionUsageService is null || _viewModel is null)
        {
            return;
        }

        var dialogViewModel = new AddUsageDialogViewModel(_promotionUsageService, request);
        var dialogWindow = new AddUsageWindow
        {
            DataContext = dialogViewModel
        };

        var wasSaved = await dialogWindow.ShowDialog<bool?>(this);
        if (wasSaved == true && dialogViewModel.LastSaveResult is not null)
        {
            await _viewModel.RefreshAfterUsageSavedAsync(dialogViewModel.LastSaveResult);
        }
    }
}
