using Bgg.Sdk;
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Core.Features
{
    public class Search
    {
        public record class Query : IRequest<Result<List<Bgg.Sdk.Models.SearchResult.Item>>>
        {
            public string SearchTerm { get; init; } = "";
        }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(query => query.SearchTerm).NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Query, Result<List<Bgg.Sdk.Models.SearchResult.Item>>>
        {
            private readonly IBggClient _bggClient;
            private readonly IMemoryCache _memoryCache;

            public Handler(IBggClient bggClient, IMemoryCache memoryCache)
            {
                _bggClient = bggClient;
                _memoryCache = memoryCache;
            }

            public Task<Result<List<Bgg.Sdk.Models.SearchResult.Item>>> Handle(Query request, CancellationToken cancellationToken)
            {
                return Result.Try(async () =>
                {
                    return await _memoryCache.GetOrCreateAsync(request.SearchTerm, async entry =>
                    {
                        entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

                        var query = new Bgg.Sdk.Core.Search.QueryParameters(request.SearchTerm)
                        {
                            Types = new() { Bgg.Sdk.Core.ThingType.Boardgame, Bgg.Sdk.Core.ThingType.BoardgameExpansion }
                        };

                        var results =  await _bggClient.SearchAsync(query);
                        return results.Items.DistinctBy(i => i.Id).ToList();
                    });
                });
            }
        }
    }
}
