using Discord;
using Discord.Interactions;
using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wolverine;

namespace Hermod.Bot.Modules;

public class EnrollModule(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("enroll", "Join this server's play-sharing group")]
    public async Task EnrollAsync()
    {
        await DeferAsync(ephemeral: true);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var guildId = Context.Guild?.Id;
        if (guildId is null)
        {
            await FollowupAsync("This command can only be used in a server.", ephemeral: true);
            return;
        }

        var mapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == guildId.Value && m.IsActive);
        if (mapping is null)
        {
            await FollowupAsync("This server hasn't been set up yet. Please wait a moment and try again.", ephemeral: true);
            return;
        }

        var discordId = Context.User.Id.ToString();

        var result = await bus.InvokeAsync<EnrollmentResult>(
            new EnrollInGroup(discordId, mapping.GroupId),
            timeout: TimeSpan.FromSeconds(10));

        var message = result.Status switch
        {
            EnrollmentStatus.Enrolled => $"You've been enrolled in **{Context.Guild!.Name}**!",
            EnrollmentStatus.AlreadyMember => "You're already a member of this group!",
            EnrollmentStatus.NotRegistered => BuildRegistrationMessage(mapping.GroupId),
            EnrollmentStatus.GroupNotFound => "This server's group could not be found. Please contact an admin.",
            _ => "Something went wrong. Please try again later.",
        };

        await FollowupAsync(message, ephemeral: true);
    }

    private string BuildRegistrationMessage(Guid groupId)
    {
        var baseUrl = configuration["WebApp:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8080";
        var returnUrl = Uri.EscapeDataString($"/enroll?groupId={groupId}");
        var enrollUrl = $"{baseUrl}/auth/login?returnUrl={returnUrl}";
        return $"You need to register first! Click the link below, sign in with Discord, and you'll be automatically enrolled.\n\n{enrollUrl}";
    }
}
