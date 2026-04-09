using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PlateGuard.App.ViewModels;

namespace PlateGuard.App.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider? _serviceProvider;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider) : this()
    {
        _serviceProvider = serviceProvider;
        DataContext = viewModel;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SearchInputTextBox?.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.AddUsageRequested -= OnAddUsageRequested;
            _viewModel.PromotionDialogRequested -= OnPromotionDialogRequested;
            _viewModel.EditUsageRequested -= OnEditUsageRequested;
            _viewModel.DeleteUsageRequested -= OnDeleteUsageRequested;
        }

        base.OnClosed(e);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.AddUsageRequested -= OnAddUsageRequested;
            _viewModel.PromotionDialogRequested -= OnPromotionDialogRequested;
            _viewModel.EditUsageRequested -= OnEditUsageRequested;
            _viewModel.DeleteUsageRequested -= OnDeleteUsageRequested;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
        {
            _viewModel.AddUsageRequested += OnAddUsageRequested;
            _viewModel.PromotionDialogRequested += OnPromotionDialogRequested;
            _viewModel.EditUsageRequested += OnEditUsageRequested;
            _viewModel.DeleteUsageRequested += OnDeleteUsageRequested;
        }
    }

    private async void OnAddUsageRequested(AddUsageDialogRequest request)
    {
        if (_serviceProvider is null || _viewModel is null)
        {
            return;
        }

        var dialogViewModel = ActivatorUtilities.CreateInstance<AddUsageDialogViewModel>(_serviceProvider, request);
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

    private async void OnPromotionDialogRequested(PromotionDialogRequest request)
    {
        if (_serviceProvider is null || _viewModel is null)
        {
            return;
        }

        var dialogViewModel = ActivatorUtilities.CreateInstance<PromotionDialogViewModel>(_serviceProvider, request);
        var dialogWindow = new PromotionDialogWindow
        {
            DataContext = dialogViewModel
        };

        var wasSaved = await dialogWindow.ShowDialog<bool?>(this);
        if (wasSaved == true && dialogViewModel.LastSavedPromotion is not null)
        {
            await _viewModel.RefreshAfterPromotionSavedAsync(dialogViewModel.LastSavedPromotion);
        }
    }

    private async void OnEditUsageRequested(EditUsageDialogRequest request)
    {
        if (_serviceProvider is null || _viewModel is null)
        {
            return;
        }

        var dialogViewModel = ActivatorUtilities.CreateInstance<EditUsageDialogViewModel>(_serviceProvider, request);
        var dialogWindow = new EditUsageWindow
        {
            DataContext = dialogViewModel
        };

        var wasSaved = await dialogWindow.ShowDialog<bool?>(this);
        if (wasSaved == true)
        {
            await _viewModel.RefreshAfterUsageRecordUpdatedAsync();
        }
    }

    private async void OnDeleteUsageRequested(DeleteUsageDialogRequest request)
    {
        if (_serviceProvider is null || _viewModel is null)
        {
            return;
        }

        var dialogViewModel = ActivatorUtilities.CreateInstance<DeleteUsageDialogViewModel>(_serviceProvider, request);
        var dialogWindow = new DeleteUsageWindow
        {
            DataContext = dialogViewModel
        };

        var wasDeleted = await dialogWindow.ShowDialog<bool?>(this);
        if (wasDeleted == true)
        {
            await _viewModel.RefreshAfterUsageDeletedAsync();
        }
    }
}
