namespace Game.Meta.Save
{
    public interface ISaveMigration
    {
        int SourceVersion { get; }
        int TargetVersion { get; }
        string MigrateJson(string sourceJson);
    }
}
