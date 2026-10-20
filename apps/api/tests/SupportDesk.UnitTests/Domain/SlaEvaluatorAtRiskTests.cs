using SupportDesk.Domain.Aggregates.Tickets;
using Xunit;

namespace SupportDesk.UnitTests.Domain;

public class SlaEvaluatorAtRiskTests
{
    private const double Threshold = 25;

    private static readonly DateTime Start = new(2026, 9, 27, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Due = Start.AddHours(40);

    private static SlaStatus Evaluate(DateTime now, DateTime? resolved = null) =>
        SlaEvaluator.Evaluate(Due, resolved, now, Start, Threshold);

    [Fact]
    public void Exactly_on_the_due_date_is_at_risk_not_breached() =>
        Assert.Equal(SlaStatus.AtRisk, Evaluate(Due));

    [Fact]
    public void One_tick_after_the_due_date_is_breached() =>
        Assert.Equal(SlaStatus.Breached, Evaluate(Due.AddTicks(1)));

    [Fact]
    public void Exactly_at_the_threshold_is_at_risk() =>
        Assert.Equal(SlaStatus.AtRisk, Evaluate(Due.AddHours(-10)));

    [Fact]
    public void One_tick_before_the_threshold_is_within_sla() =>
        Assert.Equal(SlaStatus.WithinSla, Evaluate(Due.AddHours(-10).AddTicks(-1)));

    [Fact]
    public void Resolved_on_the_due_date_is_met() =>
        Assert.Equal(SlaStatus.Met, Evaluate(Due.AddDays(3), resolved: Due));

    [Fact]
    public void Resolved_one_tick_late_is_breached() =>
        Assert.Equal(SlaStatus.Breached, Evaluate(Due.AddDays(3), resolved: Due.AddTicks(1)));

    [Fact]
    public void No_due_date_is_not_applicable() =>
        Assert.Equal(SlaStatus.NotApplicable, SlaEvaluator.Evaluate(null, null, Due, Start, Threshold));

    [Fact]
    public void Without_a_window_start_nothing_is_at_risk() =>
        Assert.Equal(SlaStatus.WithinSla, SlaEvaluator.Evaluate(Due, null, Due.AddHours(-1)));
}
