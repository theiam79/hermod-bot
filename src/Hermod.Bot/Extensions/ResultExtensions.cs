using Discord;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Bot.Extensions
{
    internal static class ResultExtensions
    {
        public static Embed ToEmbed(this Result result, string? description = null)
        {
            EmbedBuilder builder = new();

            Color color = (result.Successes.Any(), result.Errors.Any()) switch
            {
                (true, true) => Color.Gold,
                (true, _) => Color.Green,
                (_, true) => Color.Red,
                _ => Color.LightGrey
            };

            builder
                .WithColor(color)
                .WithFields(CreateFields(result));

            if (description != null) { builder.WithDescription(description); }

            return builder.Build();
        }

        private static IEnumerable<EmbedFieldBuilder> CreateFields(Result result)
        {
            if (result.Successes.Any())
            {
                yield return new EmbedFieldBuilder()
                    .WithName("Success")
                    .WithValue(string.Join(",", result.Successes.Select(x => x.Message)))
                    .WithIsInline(false);
            }

            if (result.Errors.Any())
            {
                yield return new EmbedFieldBuilder()
                    .WithName("Errors")
                    .WithValue(string.Join(",", result.Errors.Select(r => r.Message)))
                    .WithIsInline(false);
            }
        }
    }
}
