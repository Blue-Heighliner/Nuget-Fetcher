namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class MainWindowTests
{
    private readonly Mock<IPackagePropsScanner> scanner = new();
    private readonly Mock<INugetPackageDownloader> downloader = new();

    [AvaloniaFact]
    public void ClearButton_ClearsPackages()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);
        viewModel.Packages.Add(new PackageRequest { Id = "Pkg.A", Version = "1.0.0", SourceFile = "f.props", IsImplicit = false });
        MainWindow window = new() { DataContext = viewModel };
        window.Show();

        Button clear = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Clear"));
        clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Empty(viewModel.Packages);
    }

    [AvaloniaFact]
    public void ReportedProblem_OpensProblemDialog()
    {
        scanner.Setup(s => s.Scan(It.IsAny<string>())).Throws(new InvalidOperationException("bad file"));
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);
        MainWindow window = new() { DataContext = viewModel };
        window.Show();

        viewModel.AddScannedPackages("x.props");

        Assert.Contains(window.OwnedWindows, w => w is ProblemDialog);
    }

    [AvaloniaFact]
    public void DownloadButton_IsEnabledOnlyWhenDownloadIsPossible()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object) { DotnetRuntimeVersion = "10.0.5" };
        MainWindow window = new() { DataContext = viewModel };
        window.Show();
        Button download = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content is string text && text.StartsWith("Download"));
        Assert.False(download.IsEnabled);

        viewModel.Packages.Add(new PackageRequest { Id = "Pkg.A", Version = "1.0.0", SourceFile = "f.props", IsImplicit = false });

        Assert.True(download.IsEnabled);
    }

    [AvaloniaFact]
    public void AddButton_AddsEnteredPackage()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);
        MainWindow window = new() { DataContext = viewModel };
        window.Show();
        window.FindControl<TextBox>("NewPackageIdBox")!.Text = "Pkg.A";
        window.FindControl<TextBox>("NewPackageVersionBox")!.Text = "1.0.0";

        window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Add")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal("Pkg.A", Assert.Single(viewModel.Packages).Id);
    }
}
