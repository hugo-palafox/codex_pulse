using System.Text.Json.Serialization;

namespace CodexPulse.Models;

public sealed class RateLimitsResponse
{
    [JsonPropertyName("rateLimits")]
    public RateLimitBucket? RateLimits { get; set; }

    [JsonPropertyName("rateLimitsByLimitId")]
    public Dictionary<string, RateLimitBucket>? RateLimitsByLimitId { get; set; }
}

public sealed class RateLimitBucket
{
    [JsonPropertyName("limitId")]
    public string? LimitId { get; set; }

    [JsonPropertyName("limitName")]
    public string? LimitName { get; set; }

    [JsonPropertyName("primary")]
    public RateLimitWindow? Primary { get; set; }

    [JsonPropertyName("secondary")]
    public RateLimitWindow? Secondary { get; set; }

    [JsonPropertyName("rateLimitReachedType")]
    public string? RateLimitReachedType { get; set; }

    [JsonPropertyName("planType")]
    public string? PlanType { get; set; }
}

public sealed class RateLimitWindow
{
    [JsonPropertyName("usedPercent")]
    public double? UsedPercent { get; set; }

    [JsonPropertyName("windowDurationMins")]
    public int? WindowDurationMins { get; set; }

    [JsonPropertyName("resetsAt")]
    public long? ResetsAt { get; set; }
}
