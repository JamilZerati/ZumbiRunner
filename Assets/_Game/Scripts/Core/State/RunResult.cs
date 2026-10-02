using System.Collections.Generic;

namespace Game.Core.State
{
    public class RunResult
    {
        public string LevelId;
        public bool Victory;
        public float Distance;
        public int SquadAtEnd;
        public int MultiplierReached;
        public Dictionary<string, int> KillsByArchetype;
        public int Rescued;
        public int CoinsEarned;
        public List<string> MutationIds;
        public float DurationSeconds;
    }
}
