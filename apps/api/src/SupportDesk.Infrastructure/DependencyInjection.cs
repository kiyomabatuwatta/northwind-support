using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Sla;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.Infrastructure.Repositories;
using SupportDesk.Infrastructure.Services;

namespace SupportDesk.Infrastructure;

/// <summary>
/// Wires the domain's repositories and the application's abstractions to their SQL Server /
/// system implementations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is missing. See README.md for local setup.");

        services.AddDbContext<SupportDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SupportDbContext>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddSingleton(ReadSlaOptions(configuration));

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        services.AddScoped<ITicketQueries, TicketQueries>();
        services.AddScoped<ICustomerQueries, CustomerQueries>();
        services.AddScoped<IAgentQueries, AgentQueries>();
        services.AddScoped<ICategoryQueries, CategoryQueries>();
        services.AddScoped<ITriageData, TriageData>();
        services.AddScoped<IEscalationQueries, EscalationQueries>();

        services.AddScoped<SupportDbSeeder>();

        return services;
    }

    /// <summary>
    /// Reads the "Sla" section (appsettings or Sla__* environment variables). Anything missing
    /// keeps the default from <see cref="SlaOptions"/>.
    /// </summary>
    private static SlaOptions ReadSlaOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(SlaOptions.SectionName);
        var options = new SlaOptions();

        foreach (var priority in Enum.GetValues<TicketPriority>())
        {
            if (TryRead(section[$"BaseWindowHours:{priority}"], out var hours))
            {
                options.BaseWindowHours[priority] = hours;
            }
        }

        if (TryRead(section[nameof(SlaOptions.PremiumCustomerMultiplier)], out var multiplier))
        {
            options.PremiumCustomerMultiplier = multiplier;
        }

        if (TryRead(section[nameof(SlaOptions.MinimumWindowHours)], out var minimum))
        {
            options.MinimumWindowHours = minimum;
        }

        if (TryRead(section[nameof(SlaOptions.AtRiskThresholdPercent)], out var threshold))
        {
            options.AtRiskThresholdPercent = threshold;
        }

        return options;
    }

    private static bool TryRead(string? raw, out double value) =>
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
