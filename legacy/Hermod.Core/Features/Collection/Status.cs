using FluentResults;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Core.Features.Collection
{
    internal class Status
    {
        public record class Query : IRequest<Result<List<Model>>>
        {

        }

        public record class Model
        {

        }

        public class Handler : IRequestHandler<Query, Result<List<Model>>>
        {
            public async Task<Result<List<Model>>> Handle(Query request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
