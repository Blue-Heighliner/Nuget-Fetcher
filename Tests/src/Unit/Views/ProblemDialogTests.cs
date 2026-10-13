namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class ProblemDialogTests
{
    [AvaloniaFact]
    public void Constructor_SetsTitleAndMessage()
    {
        ProblemDialog dialog = new("Problem", "Something broke");

        Assert.Equal("Problem", dialog.Title);
        Assert.Equal("Something broke", dialog.FindControl<TextBlock>("MessageText")!.Text);
    }

    [AvaloniaFact]
    public void OkButton_ClosesDialog()
    {
        ProblemDialog dialog = new();
        dialog.Show();
        bool closed = false;
        dialog.Closed += (_, _) => closed = true;

        dialog.GetVisualDescendants().OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.True(closed);
    }
}
