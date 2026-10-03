using System.Text;
using TopBar.App.Services;
using TopBar.Core.Services;
using TopBar.Core.UrlCheck;
using TopBar.Platform;

namespace TopBar.App.ViewModels;

/// <summary>State of the URL checker element: counters, colors decide in the control, tooltip text is built here.</summary>
public sealed class UrlCheckerViewModel
{
    private const int MaxTooltipProblems = 8;

    private readonly IUrlCheckService _service;
    private readonly ISettingsService _settings;

    /// <summary>At least one URL is configured.</summary>
    public bool HasUrls { get; private set; }
    public bool IsRunning { get; private set; }
    /// <summary>A round has finished at least once (counters are valid).</summary>
    public bool HasResults { get; private set; }
    public bool HasProblem { get; private set; }
    public int OkCount { get; private set; }
    public int Total { get; private set; }
    public string Tooltip { get; private set; } = "";

    /// <summary>Raised on the UI thread whenever something visible changed.</summary>
    public event Action? Updated;

    internal UrlCheckerViewModel(IUrlCheckService service, ISettingsService settings, IUiThread ui)
    {
        _service = service;
        _settings = settings;
        service.Changed += (_, _) => ui.Post(Update);
        settings.Changed += (_, _) => ui.Post(Update);
        Update();
    }

    public void CheckNow() => _service.CheckNow();

    public void Update()
    {
        var snapshot = _service.Current;
        var urls = _settings.Current.UrlChecker.Urls;

        HasUrls = urls.Count > 0;
        IsRunning = snapshot.IsRunning;
        HasResults = snapshot.LastCompleted is not null && snapshot.Total > 0;
        HasProblem = snapshot.HasProblem;
        OkCount = snapshot.OkCount;
        Total = snapshot.Total;
        Tooltip = BuildTooltip(snapshot);
        Updated?.Invoke();
    }

    private string BuildTooltip(UrlCheckSnapshot snapshot)
    {
        if (!HasUrls) return Loc.Get("UrlChecker_Tip_NoUrls");
        if (!HasResults) return Loc.Get("UrlChecker_Tip_Checking");

        var time = snapshot.LastCompleted!.Value.ToString("HH:mm:ss");
        var sb = new StringBuilder();
        if (!snapshot.HasProblem)
        {
            sb.Append(string.Format(Loc.Get("UrlChecker_Tip_AllOk"), snapshot.Total, time));
        }
        else
        {
            sb.Append(string.Format(Loc.Get("UrlChecker_Tip_Problem"), snapshot.ProblemCount, snapshot.Total, time));
            var problems = snapshot.Results.Where(r => r.IsProblem).ToList();
            foreach (var r in problems.Take(MaxTooltipProblems))
                sb.Append('\n').Append(r.Url).Append(" — ").Append(Describe(r));
            if (problems.Count > MaxTooltipProblems)
                sb.Append('\n').Append(string.Format(Loc.Get("UrlChecker_Tip_More"), problems.Count - MaxTooltipProblems));
        }
        sb.Append('\n').Append(Loc.Get("UrlChecker_Tip_Click"));
        return sb.ToString();
    }

    private static string Describe(UrlCheckResult r) => r.Failure switch
    {
        UrlFailure.Http => string.Format(Loc.Get("UrlChecker_Fail_Http"), r.StatusCode),
        UrlFailure.Timeout => Loc.Get("UrlChecker_Fail_Timeout"),
        UrlFailure.NameResolution => Loc.Get("UrlChecker_Fail_Dns"),
        UrlFailure.Tls => Loc.Get("UrlChecker_Fail_Tls"),
        UrlFailure.Connection => Loc.Get("UrlChecker_Fail_Connection"),
        _ => Loc.Get("UrlChecker_Fail_Other"),
    };
}
