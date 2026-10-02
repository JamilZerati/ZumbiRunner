using System.Collections.Generic;

namespace Game.Core.State
{
    public class RunResult
    {
        public string LevelId { get; set; }
        public bool Victory { get; set; }
        public float Distance { get; set; }
        public int SquadAtEnd { get; set; }
        public int MultiplierReached { get; set; }
        public IReadOnlyDictionary<string, int> KillsByArchetype { get; set; }
        public int Rescued { get; set; }
        public int CoinsEarned { get; set; }
        public IReadOnlyList<string> MutationIds { get; set; }
        public float DurationSeconds { get; set; }
        public int Stars { get; set; }

        public RunResult()
        {
            KillsByArchetype = new Dictionary<string, int>();
            MutationIds = new List<string>();
        }
    }
}
