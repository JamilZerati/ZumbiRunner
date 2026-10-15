using System;

namespace Game.Meta.Save
{
    public class SaveStore : ISaveStore
    {
        public int CurrentVersion => 1;
        public void Save(MetaState state, string filePath) => throw new NotImplementedException();
        public MetaState Load(string filePath) => throw new NotImplementedException();
        public bool ValidateIntegrity(string filePath) => throw new NotImplementedException();
    }
}
