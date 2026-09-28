using System.ComponentModel;
using System.Runtime.CompilerServices;
using CodexPulse.Models;

namespace CodexPulse.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private string _connectionText = "Connecting to Codex…";
    private string _planText = "Plan unavailable";
    private string _lastUpdatedText = "Not refreshed yet";
    private string _primaryUsageText = "—";
    private string _primaryRemainingText = "Waiting for limits";
    private string _primaryResetText = "—";
    private string _secondaryText = "No secondary window reported";
    private string _errorText = string.Empty;
    private string _fiveHourRemainingText = "—% remaining";
    private string _weekRemainingText = "—% remaining";
    private double _primaryProgress;
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ConnectionText { get => _connectionText; private set => Set(ref _connectionText, value); }
    public string PlanText { get => _planText; private set => Set(ref _planText, value); }
    public string LastUpdatedText { get => _lastUpdatedText; private set => Set(ref _lastUpdatedText, value); }
    public string PrimaryUsageText { get => _primaryUsageText; private set => Set(ref _primaryUsageText, value); }
    public string PrimaryRemainingText { get => _primaryRemainingText; private set => Set(ref _primaryRemainingText, value); }
    public string PrimaryResetText { get => _primaryResetText; private set => Set(ref _primaryResetText, value); }
    public string SecondaryText { get => _secondaryText; private set => Set(ref _secondaryText, value); }
    public string ErrorText { get => _errorText; private set => Set(ref _errorText, value); }
    public string FiveHourRemainingText { get => _fiveHourRemainingText; private set => Set(ref _fiveHourRemainingText, value); }
    public string WeekRemainingText { get => _weekRemainingText; private set => Set(ref _weekRemainingText, value); }
    public double PrimaryProgress { get => _primaryProgress; private set => Set(ref _primaryProgress, value); }
    public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }

    public void SetLoading(bool isLoading) => IsLoading = isLoading;

    public void ShowError(Exception exception)
    {
        ConnectionText = "Codex unavailable";
        ErrorText = exception is System.ComponentModel.Win32Exception
            ? "Install Codex CLI and make sure `codex` is available on PATH."
            : exception.Message;
    }

    public void Apply(RateLimitsResponse response)
    {
        var buckets = response.RateLimitsByLimitId;
        var primaryBucket = buckets?.GetValueOrDefault("codex") ?? response.RateLimits;
        var window = primaryBucket?.Primary;
        var weekWindow = primaryBucket?.Secondary ?? FindLongestWindow(buckets, window);
        var used = Math.Clamp(window?.UsedPercent ?? 0, 0, 100);

        ConnectionText = "Codex connected";
        PlanText = string.IsNullOrWhiteSpace(primaryBucket?.PlanType) ? "ChatGPT plan" : primaryBucket.PlanType!;
        PrimaryUsageText = window?.UsedPercent is null ? "—" : $"{window.UsedPercent:0}% used";
        PrimaryRemainingText = window?.UsedPercent is null ? "Usage unavailable" : $"{100 - used:0}% remaining";
        PrimaryProgress = used / 100d;
        PrimaryResetText = FormatReset(window?.ResetsAt);
        SecondaryText = FormatSecondary(buckets, response.RateLimits?.Secondary);
        FiveHourRemainingText = FormatRemaining(window);
        WeekRemainingText = FormatRemaining(weekWindow);
        LastUpdatedText = $"Updated {DateTime.Now:h:mm:ss tt}";
        ErrorText = string.Empty;
    }

    private static RateLimitWindow? FindLongestWindow(Dictionary<string, RateLimitBucket>? buckets, RateLimitWindow? primary)
    {
        return buckets?.Values
            .SelectMany(bucket => new[] { bucket.Primary, bucket.Secondary })
            .Where(window => window?.WindowDurationMins is not null && window.WindowDurationMins > (primary?.WindowDurationMins ?? 0))
            .OrderByDescending(window => window!.WindowDurationMins)
            .FirstOrDefault();
    }

    private static string FormatRemaining(RateLimitWindow? window)
    {
        if (window?.UsedPercent is null)
        {
            return "—% remaining";
        }

        var remaining = 100 - Math.Clamp(window.UsedPercent.Value, 0, 100);
        return $"{remaining:0}% remaining";
    }

    private static string FormatSecondary(Dictionary<string, RateLimitBucket>? buckets, RateLimitWindow? fallback)
    {
        var other = buckets?.Where(pair => pair.Key != "codex").Select(pair => pair.Value).FirstOrDefault();
        var window = other?.Primary ?? fallback;
        if (window?.UsedPercent is null)
        {
            return "No secondary window reported";
        }

        return $"Secondary window: {window.UsedPercent:0}% used · resets {FormatReset(window.ResetsAt)}";
    }

    private static string FormatReset(long? unixSeconds)
    {
        if (unixSeconds is null)
        {
            return "Reset time unavailable";
        }

        var reset = DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).ToLocalTime();
        var remaining = reset - DateTimeOffset.Now;
        var countdown = remaining.TotalSeconds <= 0 ? "soon" : remaining.TotalHours >= 1 ? $"in {(int)remaining.TotalHours}h {remaining.Minutes}m" : $"in {Math.Max(1, remaining.Minutes)}m";
        return $"Resets {reset:h:mm tt} · {countdown}";
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
