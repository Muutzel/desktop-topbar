namespace TopBar.Core.UrlCheck;

/// <summary>Why a check failed (the UI turns this into a German text).</summary>
public enum UrlFailure
{
    None,
    /// <summary>The server answered with a status code outside 2xx.</summary>
    Http,
    Timeout,
    NameResolution,
    Tls,
    Connection,
    Other,
}

public static class UrlCheckRules
{
    /// <summary>A URL turns red only after this many failed checks in a row (one network hiccup must not light up the bar).</summary>
    public const int FailuresUntilProblem = 2;

    /// <summary>"Reachable" means a 2xx answer after following redirects.</summary>
    public static bool IsOk(int statusCode) => statusCode is >= 200 and <= 299;
}

/// <param name="ConsecutiveFailures">Failed checks in a row including this one; 0 if this check succeeded.</param>
public sealed record UrlCheckResult(
    string Url, bool Ok, int? StatusCode, UrlFailure Failure, int ConsecutiveFailures, DateTimeOffset CheckedAt)
{
    /// <summary>True once the URL failed <see cref="UrlCheckRules.FailuresUntilProblem"/> times in a row.</summary>
    public bool IsProblem => !Ok && ConsecutiveFailures >= UrlCheckRules.FailuresUntilProblem;
}

public sealed record UrlCheckSnapshot(bool IsRunning, IReadOnlyList<UrlCheckResult> Results, DateTimeOffset? LastCompleted)
{
    public static UrlCheckSnapshot Empty { get; } = new(false, [], null);

    public int Total => Results.Count;
    public int ProblemCount => Results.Count(r => r.IsProblem);
    /// <summary>URLs that are not (yet) a problem. A first failure still counts as OK until it is confirmed by the re-check.</summary>
    public int OkCount => Total - ProblemCount;
    public bool HasProblem => ProblemCount > 0;
    /// <summary>At least one URL failed once and awaits confirmation.</summary>
    public bool HasPending => Results.Any(r => !r.Ok && !r.IsProblem);
}
