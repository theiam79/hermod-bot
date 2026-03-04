using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hermod.BGstats
{
    public class PlayLink
    {
        public string Board { get; init; } = "";
        public int DurationMin { get; init; }
        public GameElement Game { get; init; } = new();
        public DateTime PlayDate { get; init; }
        public List<PlayerElement> Players { get; init; } = new();
        public string SourceName { get; init; } = "Hermod.Bot";
        public string SourcePlayId { get; init; } = "";
        public class PlayerElement
        {
            public bool StartPlayer { get; init; }
            public string Name { get; init; } = "";
            public int Rank { get; init; }
            public string Role { get; init; } = "";
            public double Score { get; init; }
            public string SourcePlayerId { get; init; } = "";
            public bool Winner { get; init; }
        }

        public class GameElement
        {
            public int BggId { get; init; }
            public bool HighestWins { get; init; }
            public string Name { get; init; } = "";
            public bool NoPoints { get; init; }
            public string SourceGameId { get; init; } = "";
        }
    }
}
