using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows.Threading;
using MessengerBubbles.Core;

namespace MessengerBubbles;

// Only reads the latest version number; it downloads and installs nothing.
class UpdateChecker
{
    static readonly TimeSpan FirstCheck = TimeSpan.FromMinutes(1), Interval = TimeSpan.FromHours(24);

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    readonly DispatcherTimer _timer = new() { Interval = FirstCheck };
    readonly Settings _settings;
    Version? _announced;

    public event Action<Version>? NewerFound;

    public UpdateChecker(Settings settings)
    {
        _settings = settings;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MessengerBubbles");
        _timer.Tick += async (_, _) =>
        {
            _timer.Interval = Interval;
            if (_settings.CheckUpdates) await CheckAsync();
        };
    }

    public void Start() => _timer.Start();

    async Task CheckAsync()
    {
        try
        {
            var current = Assembly.GetEntryAssembly()!.GetName().Version!;
            var newer = UpdateCheck.NewerVersion(await _http.GetStringAsync(UpdateCheck.ApiUrl), current);
            if (newer == null || newer == _announced) return;
            _announced = newer;
            AppPaths.Log($"update available: {newer}");
            NewerFound?.Invoke(newer);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            AppPaths.Log($"update check failed: {e.GetType().Name}");
        }
    }
}
