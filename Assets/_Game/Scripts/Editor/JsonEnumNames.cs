using System;

namespace Game.Editor
{
    internal static class JsonEnumNames
    {
        // TryParse aceita número ("7", "1") e lista de flags ("Damage, FireRate"); só o nome exato passa.
        public static bool TryParse<T>(string text, out T value) where T : struct, Enum
        {
            value = default;
            return !string.IsNullOrEmpty(text)
                && Enum.TryParse(text, true, out value)
                && Enum.IsDefined(typeof(T), value)
                && string.Equals(value.ToString(), text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
