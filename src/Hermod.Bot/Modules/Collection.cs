using Bgg.Sdk;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Hermod.Data.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Bgg.Sdk.Models.Collection;

namespace Hermod.Bot.Modules
{
    internal class Collection : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IBggClient _bggClient;
        private readonly IMediator _mediator;
        private readonly ILogger<Collection> _logger;

        public Collection(IBggClient bggClient, IMediator mediator, ILogger<Collection> logger)
        {
            _bggClient = bggClient;
            _mediator = mediator;
            _logger = logger;
        }

        [SlashCommand("search", "Search bgg users in this guild for a given game")]
        public async Task SearchAsync(string searchTerm)
        {
            await DeferAsync(ephemeral: true);
            //get top 25

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

            var component = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId("game-selection")
                    .WithPlaceholder("Select game")
                    .WithOptions(searchResults
                        //.Where(x => !x.Name.Contains(':'))
                        .Select(i => new SelectMenuOptionBuilder()
                            .WithLabel(ItemName(i))
                            .WithValue(i.Id.ToString()))
                        .Take(25)
                        .ToList()))
                ;

            if (totalPages > 1)
            {
                component
                    .WithButton("Prev.", $"previous-page-button:{searchTerm},1,{totalPages}", ButtonStyle.Secondary)
                    .WithButton("Next.", $"next-page-button:{searchTerm},{Math.Min(2, totalPages)},{totalPages}", ButtonStyle.Secondary);
            }

            component
                .WithButton("I don't see it", $"not-found-button", ButtonStyle.Danger);

            await FollowupAsync("Select a game from the list", ephemeral: true, components: component.Build(), options: new() { Timeout = 5000 });
            
            string ItemName(Bgg.Sdk.Models.SearchResult.Item item)
            {
                //var output = JsonEncodedText.Encode($"{item.Name.Replace(':', ' ')} - {item.YearPublished}".Truncate(100)).ToString();
                var output = $"{item.Name.Truncate(93)} - {item.YearPublished}";
                _logger.LogDebug(output);
                return output;
            }
            //selector to pick
            //query with selection type
        }
    }

    internal class CollectionComponent : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IBggClient _bggClient;
        private readonly IMediator _mediator;
        private readonly IMemoryCache _memoryCache;
        private readonly HermodContext _hermodContext;
        private readonly ILogger<CollectionComponent> _logger;

        public CollectionComponent(IBggClient bggClient, ILogger<CollectionComponent> logger, IMediator mediator, IMemoryCache memoryCache, HermodContext hermodContext)
        {
            _bggClient = bggClient;
            _logger = logger;
            _mediator = mediator;
            _memoryCache = memoryCache;
            _hermodContext = hermodContext;
        }

        [ComponentInteraction("game-selection")]
        public async Task GameSelectedAsync(string[] selectedGame)
        {
            
            await DeferAsync(ephemeral: true);
            if (!int.TryParse(selectedGame[0], out var id))
            {
                await FollowupAsync("Something went wrong");
            }

            //await DeleteOriginalResponseAsync();

            var things = await _bggClient
                .ThingAsync(id);

            var game = things.Things.First();
            var gameName = game.Names.FirstOrDefault(n => n.NameType == Bgg.Sdk.Core.NameType.Primary);

            var bggUsernames = await _hermodContext
                .Users
                .ToListAsync();

            List<Test> collections = new();
            foreach (var user in bggUsernames)
            {
                var userCollection = await _memoryCache.GetOrCreateAsync(user.NormalizedBggUsername, async entry =>
                {
                    entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                    return await _bggClient.CollectionAsync(user.BggUsername);
                });


                if (userCollection?.CollectionItems.FirstOrDefault(i => i.ObjectId == id) is not CollectionItem match) continue;
                collections.Add(new Test(match, user));
            }

            var statuses = new List<CollectionItem.CollectionItemStatus>()
            {
                CollectionItem.CollectionItemStatus.Owned,
                CollectionItem.CollectionItemStatus.Preordered,
                CollectionItem.CollectionItemStatus.Wishlist,
                CollectionItem.CollectionItemStatus.WantToPlay,
                CollectionItem.CollectionItemStatus.ForTrade,
            };

            var rand = new Random();

            var author = (Context.User as SocketGuildUser)?.Nickname ?? Context.User.Username;

            var embed = new EmbedBuilder()
                .WithTitle(gameName?.Value ?? "Unknown")
                //.WithImageUrl(game.ImageUrl)
                .WithThumbnailUrl(game.ThumbnailUrl)
                .WithUrl($"https://boardgamegeek.com/boardgame/{id}")
                .WithColor(RandRgb(), RandRgb(), RandRgb())
                .WithFields(CreateFields(collections))
                //.WithAuthor($"Searched by {author}")
                .WithFooter($"Searched by {author}")
                .Build();

            var component = new ComponentBuilder()
                .WithButton("View on BGG", style: ButtonStyle.Link, url: $"https://boardgamegeek.com/boardgame/{id}");

            await FollowupAsync(embed:embed, ephemeral: false);

            int RandRgb() => rand.Next(0, 255);
            //string UserList(IEnumerable<Hermod.Data.Models.User> users) => users.Any() ? string.Join("\r\n", users.Select(u => $"<@{u.DiscordId}> - `{u.BggUsername}`")) : "None";
        }

        //statuses.Select(s => new EmbedFieldBuilder()
        //            .WithName(s.ToString())
        //            .WithValue(UserList(collections.Where(x => x.Item.Status.HasFlag(s)).Select(x => x.User))

        IEnumerable<EmbedFieldBuilder> CreateFields(List<Test> collections)
        {
            var statuses = new List<CollectionItem.CollectionItemStatus>()
            {
                CollectionItem.CollectionItemStatus.Owned,
                CollectionItem.CollectionItemStatus.Preordered,
                CollectionItem.CollectionItemStatus.Wishlist,
                CollectionItem.CollectionItemStatus.WantToPlay,
                CollectionItem.CollectionItemStatus.ForTrade,
            };

            bool atLeastOne = false;
            foreach (var status in statuses)
            {
                var collectionsContaining = collections.Where(x => x.Item.Status.HasFlag(status)).Select(x => x.User).ToList();
                if (!collectionsContaining.Any()) { continue; }

                yield return new EmbedFieldBuilder()
                    .WithName(status.ToString())
                    .WithValue(UserList(collectionsContaining));

                atLeastOne = true;
            }

            if (!atLeastOne)
            {
                yield return new EmbedFieldBuilder()
                    .WithValue("There were no users found for this game");
            }

            string UserList(IEnumerable<Hermod.Data.Models.User> users) => users.Any() ? string.Join("\r\n", users.Select(u => $"<@{u.DiscordId}> - `{u.BggUsername}`")) : "None";
        }

        class Test
        {
            public Test(Bgg.Sdk.Models.Collection.CollectionItem item, Hermod.Data.Models.User user)
            {
                Item = item;
                User = user;
            }

            public Hermod.Data.Models.User User { get; init; }
            public Bgg.Sdk.Models.Collection.CollectionItem Item {get;init;}
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

        private async Task ChangePageAsync(string searchTerm, int loadPage, int totalPages)
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
                    //.WithMinValues(1)
                    //.WithMaxValues(5)
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
                //.WithButton("Submit", "submit-button", ButtonStyle.Primary)
                .Build();

            if (Context.Interaction is SocketMessageComponent ctx)
            {
                await ctx.UpdateAsync(message =>
                {
                    message.Components = component;
                });
            }
        }
    }

    internal static class Extensions
    {
        public static string Truncate(this string source, int limit)
        {
            return source[..Math.Min(source.Length, limit)];
        }
    }
}
