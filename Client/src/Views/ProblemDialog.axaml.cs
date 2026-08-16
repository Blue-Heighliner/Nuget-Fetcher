namespace NugetFetcher.Views;

/// <summary>
/// A modal popup used to report a problem to the user.
/// </summary>
internal sealed partial class ProblemDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDialog"/> class.
    /// </summary>
    public ProblemDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDialog"/> class with the given title and message.
    /// </summary>
    /// <param name="title">The dialog's title.</param>
    /// <param name="message">The problem message to display.</param>
    public ProblemDialog(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
