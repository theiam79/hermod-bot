using Hermod.Bot.Data;
using Hermod.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wolverine;

namespace Hermod.Bot.Modules;

public class EnrollModule(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : ApplicationCommandModule<SlashCommandContext>
{
    [SlashCommand("enroll", "Join this server's play-sharing group")]
    public async Task EnrollAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var guildId = Context.Interaction.GuildId;
        if (guildId is null)
        {
            await FollowupAsync(new() { Content = "This command can only be used in a server.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var mapping = await db.GuildMappings
            .FirstOrDefaultAsync(m => m.DiscordGuildId == guildId.Value && m.IsActive);
        if (mapping is null)
        {
            await FollowupAsync(new() { Content = "This server hasn't been set up yet. Please wait a moment and try again.", Flags = MessageFlags.Ephemeral });
            return;
        }

        var discordId = Context.Interaction.User.Id.ToString();

        var result = await bus.InvokeAsync<EnrollmentResult>(
            new EnrollInGroup("Discord", discordId, mapping.GroupId),
            timeout: TimeSpan.FromSeconds(10));

        if (result.UserId.HasValue)
            await DiscordUserMappingHelper.UpsertAsync(db, Context.Interaction.User.Id, result.UserId.Value);

        var guildName = Context.Interaction.GuildId.HasValue
            ? Context.Client.Cache.Guilds.GetValueOrDefault(Context.Interaction.GuildId.Value)?.Name ?? "this server"
            : "this server";

        var message = result.Status switch
        {
            EnrollmentStatus.Enrolled => $"You've been enrolled in **{guildName}**!",
            EnrollmentStatus.AlreadyMember => "You're already a member of this group!",
            EnrollmentStatus.NotRegistered => BuildRegistrationMessage(mapping.GroupId),
            EnrollmentStatus.GroupNotFound => "This server's group could not be found. Please contact an admin.",
            _ => "Something went wrong. Please try again later.",
        };

        await FollowupAsync(new() { Content = message, Flags = MessageFlags.Ephemeral });
    }

    private string BuildRegistrationMessage(Guid groupId)
    {
        var baseUrl = configuration["WebApp:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:8080";
        var returnUrl = Uri.EscapeDataString($"/enroll?groupId={groupId}");
        var enrollUrl = $"{baseUrl}/auth/login?returnUrl={returnUrl}";
        return $"You need to register first! Click the link below, sign in with Discord, and you'll be automatically enrolled.\n\n{enrollUrl}";
    }
}
