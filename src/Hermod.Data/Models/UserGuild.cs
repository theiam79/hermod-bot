using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.Data.Models
{
    public class UserGuild
    {
        public Guid Id { get; init; }
        public string UserNickname { get; set; } = "";
        public ulong UserId { get; init; }
        public User User { get; init; } = null!;

        public ulong GuildId { get; init; }
        public Guild Guild { get; init; } = null!;
    }
}
