using System;

namespace Game.Editor
{
    internal static class JsonEnumNames
    {
        public static bool TryParse<T>(string text, out T value) where T : struct, Enum => throw new NotImplementedException();
    }
}
