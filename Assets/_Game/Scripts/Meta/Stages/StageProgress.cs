namespace Game.Meta.Stages
{
    public class StageProgress
    {
        public string StageId { get; set; }
        public int Stars { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsBossStage { get; set; }
    }
}
