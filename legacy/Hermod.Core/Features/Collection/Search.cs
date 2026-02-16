using Bgg.Sdk;
using Bgg.Sdk.Models;
using FluentResults;
using Hermod.Data.Context;
using Hermod.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static Bgg.Sdk.Models.Collection;

namespace Hermod.Core.Features.Collection
{
    public class Search
    {
        public record class Query : IRequest<Result<List<Model>>> 
        {
            public int GameId { get; init; }
            public ulong GuildId { get; init; }
        }

        public class Model
        {
            public Model(Data.Models.User user, CollectionItem? item)
            {
                User = user;
                Item = item;
            }

            public Data.Models.User User { get; init; }
            public CollectionItem? Item { get; init; }
        }

        public class Handler : IRequestHandler<Query, Result<List<Model>>>
        {
            private readonly HermodContext _hermodContext;
            private readonly IMemoryCache _memoryCache;
            private readonly IBggClient _bggClient;

            public Handler(HermodContext hermodContext, IMemoryCache memoryCache, IBggClient bggClient)
            {
                _hermodContext = hermodContext;
                _memoryCache = memoryCache;
                _bggClient = bggClient;
            }

            public async Task<Result<List<Model>>> Handle(Query request, CancellationToken cancellationToken)
            {




                var usersInGuild = await _hermodContext
                    .Guilds
                    .Where(g => g.GuildId == request.GuildId)
                    .SelectMany(g => g.Users)
                    .ToListAsync();

                var collectionTasks = usersInGuild
                    .Select(x => CheckUserCollectionAsync(x, request.GameId))
                    .ToList();

                var userWithCollectionItems = await Task.WhenAll(collectionTasks);

                var usersThatHadGame = userWithCollectionItems
                    .Where(x => x.Item != null)
                    .ToList();

                return Result.Ok(usersThatHadGame);
            }

            private async Task<Model> CheckUserCollectionAsync(Data.Models.User user, int id)
            {
                var userCollection = await _memoryCache.GetOrCreateAsync(user.NormalizedBggUsername, async entry =>
                {
                    entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                    return await _bggClient.CollectionAsync(user.BggUsername);
                });

                return new(user, userCollection?.CollectionItems?.FirstOrDefault(i => i.ObjectId == id));
            }
        }
    }
}
