using Bgg.Sdk;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Hermod.Bot.Extensions;
using Hermod.Data.Context;
using Hermod.Core.Features;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Collection = Hermod.Core.Features.Collection;
using static Bgg.Sdk.Models.Collection.CollectionItem;
using Bgg.Sdk.Models;

namespace Hermod.Bot.Modules
{
    internal class CollectionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IBggClient _bggClient;
        private readonly IMediator _mediator;
        private readonly ILogger<CollectionModule> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly HermodContext _hermodContext;
        private readonly Random _random = new();

        public CollectionModule(IBggClient bggClient, IMediator mediator, ILogger<CollectionModule> logger, IMemoryCache memoryCache, HermodContext hermodContext)
        {
            _bggClient = bggClient;
            _mediator = mediator;
            _logger = logger;
            _memoryCache = memoryCache;
            _hermodContext = hermodContext;
        }

        [SlashCommand("search", "Search bgg users in this guild for a given game")]
        public async Task SearchAsync(string searchTerm)
        {
            await DeferAsync(ephemeral: true);

            var result = await _mediator.Send(new Core.Features.Search.Query { SearchTerm = searchTerm });

            if (result.IsFailed)
            {
                await FollowupAsync("Something went wrong");
                return;
            }

            var searchResults = result.Value;

            if (!searchResults.Any())
            {
                await FollowupAsync($"{searchTerm} was not found on BGG. Try being less specific in your search");
                return;
            }

            var totalPages = searchResults.Count / 25;

            await ChangePageAsync(searchTerm, 1, totalPages, true);
        }

        [ComponentInteraction("not-found-button")]
        public async Task NotFoundAsync()
        {
            await RespondAsync("Discord limits us to 25 options per page. Try refining your search.");
            await Context.Interaction.DeleteOriginalResponseAsync();
        }

        [ComponentInteraction("next-page-button:*,*,*")]
        public async Task NextPageAsync(string searchTerm, string page, string total)
        {
            if (int.TryParse(page, out var loadPage) && int.TryParse(total, out var totalPages))
            {
                await ChangePageAsync(searchTerm, loadPage, totalPages);
            }
        }

        [ComponentInteraction("previous-page-button:*,*,*")]
        public async Task PreviousPageAsync(string searchTerm, string page, string total)
        {
            if (int.TryParse(page, out var loadPage) && int.TryParse(total, out var totalPages))
            {
                await ChangePageAsync(searchTerm, loadPage, totalPages);
            }
        }

        [ComponentInteraction("game-selection")]
        public async Task GameSelectedAsync(string[] selectedGame)
        {
            await DeferAsync(ephemeral: true);
            if (!int.TryParse(selectedGame[0], out var id))
            {
                await FollowupAsync("Something went wrong");
            }

            var query = new Collection.Search.Query
            {
                GameId = id,
                GuildId = Context.Guild.Id
            };

            var usersThatHaveGameInCollection = await _mediator.Send(query);

            if (usersThatHaveGameInCollection.IsFailed)
            {
                await FollowupAsync("", embed: usersThatHaveGameInCollection.ToResult().ToEmbed());
                return;
            }

            if (await _bggClient.ThingAsync(id) is not ThingCollection thingCollection || !thingCollection.Things.Any())
            {
                await FollowupAsync("Failed to retrieve game from BGG");
                return;
            }

            var game = thingCollection.Things.First();
            var gameName = game.Names.FirstOrDefault(n => n.NameType == Bgg.Sdk.Core.NameType.Primary)?.Value ?? "Unknown";
            var author = (Context.User as SocketGuildUser)?.Nickname ?? Context.User.Username;

            var embed = new EmbedBuilder()
                .WithTitle(gameName)
                .WithThumbnailUrl(game.ThumbnailUrl)
                .WithUrl($"https://boardgamegeek.com/boardgame/{id}")
                .WithColor(RandRgb(), RandRgb(), RandRgb())
                .WithFields(CreateFields(usersThatHaveGameInCollection.Value))
                .WithFooter($"Searched by {author}")
                .Build();

            await FollowupAsync(embed: embed, ephemeral: false);

            int RandRgb() => _random.Next(0, 255);
        }

        IEnumerable<EmbedFieldBuilder> CreateFields(List<Collection.Search.Model> collections)
        {
            var statuses = new List<CollectionItemStatus>()
            {
                CollectionItemStatus.Owned,
                CollectionItemStatus.Preordered,
                CollectionItemStatus.Wishlist,
                CollectionItemStatus.WantToPlay,
                CollectionItemStatus.ForTrade,
            };

            bool atLeastOne = false;
            foreach (var status in statuses)
            {
                var collectionsContaining = collections.Where(x => x.Item!.Status.HasFlag(status)).Select(x => x.User).ToList();
                if (!collectionsContaining.Any()) { continue; }

                yield return new EmbedFieldBuilder()
                    .WithName(status.ToString())
                    .WithValue(UserList(collectionsContaining));

                atLeastOne = true;
            }

            if (!atLeastOne)
            {
                yield return new EmbedFieldBuilder()
                    .WithName("No users found")
                    .WithValue("There were no users found for this game in the guild");
            }

            string UserList(IEnumerable<Hermod.Data.Models.User> users) => users.Any() ? string.Join("\r\n", users.Select(u => $"<@{u.DiscordId}> - `{u.BggUsername}`")) : "None";
        }

        private async Task ChangePageAsync(string searchTerm, int loadPage, int totalPages, bool initial = false)
        {
            var result = await _mediator.Send(new Core.Features.Search.Query { SearchTerm = searchTerm });

            if (result.IsFailed)
            {
                await FollowupAsync("Something went wrong");
                return;
            }

            var searchResults = result.Value;

            var previous = Math.Max(1, loadPage - 1);
            var next = Math.Min(loadPage + 1, totalPages);

            _logger.LogDebug("Loading page {Page} of {Total} : {Next} : {Previous}", loadPage, totalPages, next, previous);

            var component = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId("game-selection")
                    .WithPlaceholder("Select game")
                    .WithOptions(searchResults
                        .Select(i => new SelectMenuOptionBuilder()
                            .WithLabel($"{i.Name.Truncate(93)} - {i.YearPublished}")
                            .WithValue(i.Id.ToString()))
                        .Skip((loadPage - 1) * 25)
                        .Take(25)
                        .ToList()))
                .WithButton("Prev.", $"previous-page-button:{searchTerm},{previous},{totalPages}", ButtonStyle.Secondary)
                .WithButton("Next.", $"next-page-button:{searchTerm},{next},{totalPages}", ButtonStyle.Secondary)
                .WithButton("I don't see it", $"not-found-button", ButtonStyle.Danger)
                .Build();

            if (initial)
            {
                await FollowupAsync("Select a game from the list", ephemeral: true, components: component);
                return;
            }

            if (Context.Interaction is not SocketMessageComponent ctx)
            {
                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    [nameof(searchTerm)] = searchTerm,
                    [nameof(loadPage)] = loadPage,
                    [nameof(totalPages)] = totalPages,
                });

                _logger.LogWarning("Something went wrong changing the page");
                return;
            }

            await ctx.UpdateAsync(message =>
            {
                message.Components = component;
            });
        }
    }
}
