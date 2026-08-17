namespace BlueHeighliner.NugetFetcher.Views;

/// <summary>
/// The application's main window.
/// </summary>
internal sealed partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.ProblemReported += OnProblemReported;
    }

    private async void OnProblemReported(object? sender, string message)
    {
        ProblemDialog dialog = new("Problem", message);
        await dialog.ShowDialog(this);
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Directory.Packages.props file(s)",
            AllowMultiple = true,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Directory.Packages.props") { Patterns = new[] { "Directory.Packages.props", "*.props" } },
            },
        });

        foreach (IStorageFile file in files)
            ViewModel.AddScannedPackages(file.Path.LocalPath);
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ClearPackages();
    }

    private async void OnDownloadClick(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select output zip file",
            SuggestedFileName = "packages.zip",
            DefaultExtension = "zip",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("Zip archive") { Patterns = new[] { "*.zip" } },
            },
        });

        if (file is null)
            return;

        await ViewModel.DownloadAsync(file.Path.LocalPath, CancellationToken.None);
    }
}
