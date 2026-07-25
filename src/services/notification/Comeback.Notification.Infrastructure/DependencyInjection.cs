namespace Comeback.Notification.Infrastructure;

using System.Net;
using System.Net.Mail;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Infrastructure.Email;
using Comeback.Notification.Infrastructure.Messaging;
using Comeback.Notification.Infrastructure.Persistence;
using Comeback.Notification.Infrastructure.Persistence.Repositories;
using Comeback.Notification.Infrastructure.Realtime;
using Comeback.Notification.Infrastructure.Settings;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<INotificationUnitOfWork>(sp => sp.GetRequiredService<NotificationDbContext>());
        services.AddScoped<IInAppNotificationRepository, InAppNotificationRepository>();

        services.AddSignalR();
        services.AddScoped<INotificationPusher, SignalRNotificationPusher>();

        var smtp = configuration.GetSection(SmtpSettings.SectionName).Get<SmtpSettings>()
                   ?? new SmtpSettings();

        // A new SmtpClient per send, deliberately. A single shared instance keeps its connection
        // open between sends; once the server drops that idle connection, the next send fails with
        // "Service not available ... Timeout - closing connection" and the message is lost. That is
        // exactly what happened to the first verification email after any quiet period.
        // SmtpClient is also not safe for concurrent sends, which a shared instance invites.
        services
            .AddFluentEmail(smtp.FromEmail, smtp.FromName)
            .AddSmtpSender(() => new SmtpClient(smtp.Host, smtp.Port)
            {
                Credentials = smtp.Username is not null
                    ? new NetworkCredential(smtp.Username, smtp.Password)
                    : null,
                EnableSsl = false,
            });

        services.AddScoped<IEmailSender, FluentEmailSender>();

        var profileApi = configuration["Services:ProfileApi"] ?? "http://profile-api:8080";
        services.AddHttpClient<Application.Common.Interfaces.IAllUsersClient, Http.HttpAllUsersClient>(
            c => c.BaseAddress = new Uri(profileApi));

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notification", false));

            x.AddConsumer<EmailVerificationRequestedConsumer>();
            x.AddConsumer<MatchInvitationSentConsumer>();
            x.AddConsumer<MatchInvitationRespondedConsumer>();
            x.AddConsumer<MatchParticipantWithdrawnConsumer>();
            x.AddConsumer<MatchCancelledConsumer>();
            x.AddConsumer<MatchResultSubmittedConsumer>();
            x.AddConsumer<MatchDetailsUpdatedConsumer>();
            x.AddConsumer<MatchResultReminderConsumer>();
            x.AddConsumer<MatchResultOverdueConsumer>();
            x.AddConsumer<MatchMissedConsumer>();
            x.AddConsumer<PlayerWantedConsumer>();
            x.AddConsumer<PlayerJoinedViaPublicCallConsumer>();
            x.AddConsumer<GroupMatchInviteConsumer>();
            x.AddConsumer<GroupMatchInviteRespondedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "comeback");
                    h.Password(configuration["RabbitMq:Password"] ?? "comeback_dev");
                });

                // Sending mail and pushing notifications can fail transiently (SMTP hiccup,
                // a dropped connection). Without a retry the very first failure moves the
                // message to the _error queue and the user never receives it.
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
