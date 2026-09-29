namespace Game.Gameplay
{
    public static class CollisionLayers
    {
        public const string SquadBody = "SquadBody";
        public const string PlayerProjectile = "PlayerProjectile";
        public const string Enemy = "Enemy";
        public const string EnemyProjectile = "EnemyProjectile";
        public const string Pickup = "Pickup";

        public const int SquadBodyLayer = 8;
        public const int PlayerProjectileLayer = 9;
        public const int EnemyLayer = 10;
        public const int EnemyProjectileLayer = 11;
        public const int PickupLayer = 12;

        public const int SquadBodyMask = 1 << SquadBodyLayer;
        public const int PlayerProjectileMask = 1 << PlayerProjectileLayer;
        public const int EnemyMask = 1 << EnemyLayer;
        public const int EnemyProjectileMask = 1 << EnemyProjectileLayer;
        public const int PickupMask = 1 << PickupLayer;
    }
}
