using Microsoft.AspNetCore.Mvc;
using UserActivityLogger.Extensions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using UserActivityLogger.Services;
using UserActivityLogger.Filters;
using UserActivityLogger.Options;

namespace UserActivityLogger.UnitTests;

public class RegistrationTests
{
    [Fact]
    public void AddUserActivityLogging_WhenCalledTwice_AddsFilterOnlyOnce()
    {
        var options = new MvcOptions();

        options.AddUserActivityLogging();
        options.AddUserActivityLogging();

        Assert.Single(options.Filters);
    }
    [Fact]
    public void AddUserActivityLogger_WhenCalledTwice_RegistersServiceOnce()
    {
        var services = new ServiceCollection();

        services.AddUserActivityLogger<TestDbContext>();
        services.AddUserActivityLogger<TestDbContext>();

        var kayitlar = services.Where(
            service => service.ServiceType == typeof(IUserActivityLogger)
        );

        Assert.Single(kayitlar);

        var filterRegistrations = services.Where(
        service => service.ServiceType == typeof(UserActivityLoggingFilter)
        );

        Assert.Single(filterRegistrations);

        var optionsRegistrations = services.Where(
        service => service.ServiceType == typeof(UserActivityLoggerOptions)
        );

        Assert.Single(optionsRegistrations);
    }
    [Fact]
    public void AddUserActivityLogger_WhenConfiguredTwice_Throws()
    {
        var services = new ServiceCollection();

        services.AddUserActivityLogger<TestDbContext>(
            options => options.FailureBehavior = AuditFailureBehavior.Throw
        );

        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddUserActivityLogger<TestDbContext>(
                options => options.FailureBehavior = AuditFailureBehavior.LogAndContinue
            );
        });

        var registrations = services.Where(
            service => service.ServiceType == typeof(UserActivityLoggerOptions)
        );

        Assert.Single(registrations);
    }

    [Fact]
    public void AddUserActivityLogger_WhenCalledAgainWithoutConfiguration_KeepsOriginalOptions()
    {
        var services = new ServiceCollection();

        services.AddUserActivityLogger<TestDbContext>(
            options => options.FailureBehavior = AuditFailureBehavior.Throw
        );

        var originalOptions = services
            .Single(service => service.ServiceType == typeof(UserActivityLoggerOptions))
            .ImplementationInstance;

        services.AddUserActivityLogger<TestDbContext>();

        var currentOptions = services
            .Single(service => service.ServiceType == typeof(UserActivityLoggerOptions))
            .ImplementationInstance;

        Assert.Same(originalOptions, currentOptions);
    }

}