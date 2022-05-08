using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Core.Features
{
    internal class ExceptionLogger<TRequest, TResponse> : IRequestExceptionHandler<TRequest, TResponse>
        where TRequest : MediatR.IRequest<TResponse>
    {
        private readonly ILogger<ExceptionLogger<TRequest, TResponse>> _logger;

        public ExceptionLogger(ILogger<ExceptionLogger<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public Task Handle(TRequest request, Exception exception, RequestExceptionHandlerState<TResponse> state, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Exception occured when executing {@Request}", request);
            return Task.CompletedTask;
        }
    }
}
