using Hermod.Data.Context;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;
using Bgg.Sdk.Extensions;

namespace Hermod.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHermod(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);
            services.AddMediatR(typeof(ServiceCollectionExtensions).Assembly);
            services.AddBgg();
            return services;
        }
    }
}
