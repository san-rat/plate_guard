using System;
using Avalonia.Controls;
using PlateGuard.App.ViewModels;

namespace PlateGuard.App.Views;

public partial class DeleteUsageWindow : Window
{
    private DeleteUsageDialogViewModel? _viewModel;

    public DeleteUsageWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as DeleteUsageDialogViewModel;
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        base.OnClosed(e);
    }

    private void OnCloseRequested(bool result)
    {
        Close(result);
    }
}
