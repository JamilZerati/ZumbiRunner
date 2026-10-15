namespace Game.Meta.Save
{
    public interface ISaveStore
    {
        int CurrentVersion { get; }
        void Save(MetaState state, string filePath);
        MetaState Load(string filePath);
        bool ValidateIntegrity(string filePath);
    }
}
