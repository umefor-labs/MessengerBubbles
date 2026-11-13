using System.Net.Http;
using System.Reflection;
using System.Windows.Threading;
using MessengerBubbles.Core;

namespace MessengerBubbles;

public enum CheckResult { Newer, UpToDate, Failed }

// Only reads the latest version number; it downloads and installs nothing.
class UpdateChecker
{
    static readonly TimeSpan FirstCheck = TimeSpan.FromMinutes(1), Interval = TimeSpan.FromHours(24);

    public static Version Current { get; } = Assembly.GetEntryAssembly()!.GetName().Version!;

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
            if (!_settings.CheckUpdates) return;
            var (result, newer) = await FetchAsync();
            // The timed check stays quiet unless there is something new it has not shown yet.
            if (result == CheckResult.Newer && newer != _announced)
            {
                _announced = newer;
                NewerFound?.Invoke(newer!);
            }
        };
    }

    public void Start() => _timer.Start();

    // Asked from the menu, so it always answers, even when nothing is newer or the network is down.
    public async Task<(CheckResult Result, Version? Newer)> CheckNowAsync()
    {
        var answer = await FetchAsync();
        if (answer.Result == CheckResult.Newer) _announced = answer.Newer;
        return answer;
    }

    async Task<(CheckResult Result, Version? Newer)> FetchAsync()
    {
        try
        {
            var newer = UpdateCheck.NewerVersion(await _http.GetStringAsync(UpdateCheck.ApiUrl), Current);
            if (newer == null) return (CheckResult.UpToDate, null);
            AppPaths.Log($"update available: {newer}");
            return (CheckResult.Newer, newer);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            AppPaths.Log($"update check failed: {e.GetType().Name}");
            return (CheckResult.Failed, null);
        }
    }
}
