using Hermod.Data.Context;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Bgg.Sdk.Extensions;
using MediatR.Pipeline;
using Hermod.Core.Features;

namespace Hermod.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHermod(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);
            services.AddMediatR(typeof(ServiceCollectionExtensions).Assembly);
            services.AddTransient(typeof(IRequestExceptionHandler<,>), typeof(ExceptionLogger<,>));

            services.AddBgg();
            services.AddHttpClient<Features.Share.Post.Handler>();
            return services;
        }
    }
}
