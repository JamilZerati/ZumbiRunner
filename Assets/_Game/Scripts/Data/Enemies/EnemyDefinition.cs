using System;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Horde Runner/Data/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        public string Id;
        public int BaseHp;
        public float Speed;
        public float ContactDps;
        public int CoinValue;
        [SerializeReference] public IEnemyBehavior[] Behaviors = Array.Empty<IEnemyBehavior>();

        public void SetData(string id, int baseHp, float speed, float contactDps, int coinValue, IEnemyBehavior[] behaviors)
        {
            Id = id;
            BaseHp = baseHp;
            Speed = speed;
            ContactDps = contactDps;
            CoinValue = coinValue;
            Behaviors = behaviors ?? Array.Empty<IEnemyBehavior>();
        }
    }
}
