using System.Collections.Generic;
using Game.Core.Stats;

namespace Game.Tests.EditMode
{
    // Números copiados da tabela "Conteúdo inicial" do plano NEX-510, seção 4; não derivar do JSON importado.
    internal sealed class WeaponTestCatalog : IWeaponCatalog
    {
        public static WeaponProfile Pistol => new WeaponProfile("pistol", 2f, 10, 15f, 40f, 1, 0f);
        public static WeaponProfile Shotgun => new WeaponProfile("shotgun", 1.2f, 8, 14f, 22f, 3, 1.2f);
        public static WeaponProfile Smg => new WeaponProfile("smg", 6f, 4, 20f, 35f, 1, 0f);

        private readonly Dictionary<string, WeaponProfile> profiles = new();

        public static WeaponTestCatalog CreateDefault()
        {
            var catalog = new WeaponTestCatalog();
            catalog.Add(Pistol);
            catalog.Add(Shotgun);
            catalog.Add(Smg);
            return catalog;
        }

        public void Add(WeaponProfile profile)
        {
            profiles[profile.Id] = profile;
        }

        public bool TryGet(string weaponId, out WeaponProfile profile)
        {
            if (weaponId != null && profiles.TryGetValue(weaponId, out profile))
            {
                return true;
            }

            profile = default;
            return false;
        }
    }
}
