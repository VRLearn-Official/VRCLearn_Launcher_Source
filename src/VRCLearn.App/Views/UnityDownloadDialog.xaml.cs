using System.Windows;
using VRCLearn.Infrastructure.Windows;

namespace VRCLearn.App.Views;

public partial class UnityDownloadDialog : DialogWindowBase
{
    private static readonly TimeSpan MinimumChecking = TimeSpan.FromMilliseconds(300);

    private readonly Func<CancellationToken, Task<UnityEdition>>? _detect;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly string _chinaText;
    private readonly string _internationalText;
    private readonly string _unknownText;

    public UnityDownloadDialog(
        string title,
        string checkingText,
        string chinaText,
        string internationalText,
        string unknownText,
        string continueText,
        string proxyText,
        Func<CancellationToken, Task<UnityEdition>> detect)
    {
        InitializeComponent();

        Title = TitleText.Text = title;
        MessageText.Text = checkingText;
        ContinueButton.Content = continueText;
        ProxyButton.Content = proxyText;
        _chinaText = chinaText;
        _internationalText = internationalText;
        _unknownText = unknownText;
        _detect = detect;

        Loaded += OnLoaded;
        Closed += (_, _) => _cancellation.Cancel();
    }

    public UnityDownloadDialog(
        string title,
        string message,
        string officialText,
        string continueText,
        string proxyText)
    {
        InitializeComponent();

        Title = TitleText.Text = title;
        MessageText.Text = message;
        OfficialButton.Content = officialText;
        ContinueButton.Content = continueText;
        ProxyButton.Content = proxyText;
        _detect = null;
        _chinaText = "";
        _internationalText = "";
        _unknownText = "";

        CheckingBar.Visibility = Visibility.Collapsed;
        OfficialButton.Visibility = Visibility.Visible;
        ProxyButton.Visibility = Visibility.Visible;
        ContinueButton.Visibility = Visibility.Visible;

        Closed += (_, _) => _cancellation.Cancel();
    }

    public bool OfficialClicked { get; private set; }

    public bool ProxyClicked { get; private set; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_detect is null)
        {
            return;
        }

        UnityEdition edition;

        try
        {
            var elapsed = Task.Delay(MinimumChecking, _cancellation.Token);

            edition = await _detect(_cancellation.Token);
            await elapsed;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (_cancellation.IsCancellationRequested)
        {
            return;
        }

        CheckingBar.Visibility = Visibility.Collapsed;
        MessageText.Text = edition switch
        {
            UnityEdition.China => _chinaText,
            UnityEdition.International => _internationalText,
            _ => _unknownText,
        };
        ProxyButton.Visibility = edition == UnityEdition.International ? Visibility.Collapsed : Visibility.Visible;
        ContinueButton.Visibility = Visibility.Visible;
    }

    private void OnOfficialClick(object sender, RoutedEventArgs e)
    {
        OfficialClicked = true;
        DialogResult = false;
    }

    private void OnProxyClick(object sender, RoutedEventArgs e)
    {
        ProxyClicked = true;
        DialogResult = false;
    }

    private void OnContinueClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
