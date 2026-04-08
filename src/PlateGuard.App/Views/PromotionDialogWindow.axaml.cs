using System;
using Avalonia.Controls;
using PlateGuard.App.ViewModels;

namespace PlateGuard.App.Views;

public partial class PromotionDialogWindow : Window
{
    private PromotionDialogViewModel? _viewModel;

    public PromotionDialogWindow()
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

        _viewModel = DataContext as PromotionDialogViewModel;

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

    private void OnCloseRequested(bool dialogResult)
    {
        Close(dialogResult);
    }
}
