using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Bot.Extensions
{
    internal static class StringExtensions
    {
        public static string Truncate(this string source, int limit)
        {
            return source[..Math.Min(source.Length, limit)];
        }
    }
}
