using Game.Core;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using Game.Presentation;
using UnityEngine;

namespace Game.Composition
{
    // Roda antes de todo consumidor: o EventBus que o SceneBuilder usa só existe em tempo de edição
    // e não é serializado, então a cena carregada do disco precisa do próprio barramento (GH #84).
    [DefaultExecutionOrder(-1000)]
    public class GreyboxRunComposer : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private HordeSpawner hordeSpawner;
        [SerializeField] private StatusEffectDirector statusDirector;
        [SerializeField] private GeneralAbilityController abilityController;
        [SerializeField] private GeneralAbilityHud abilityHud;

        public IEventBus EventBus { get; private set; }

        public void Configure(
            CombatDirector director,
            HordeSpawner spawner,
            StatusEffectDirector status,
            GeneralAbilityController controller,
            GeneralAbilityHud hud)
        {
            combatDirector = director;
            hordeSpawner = spawner;
            statusDirector = status;
            abilityController = controller;
            abilityHud = hud;
        }

        public void Compose(IEventBus eventBus)
        {
            EventBus = eventBus;

            if (combatDirector != null)
            {
                combatDirector.EventBus = eventBus;
            }

            if (hordeSpawner != null)
            {
                hordeSpawner.EventBus = eventBus;
            }

            if (statusDirector != null)
            {
                statusDirector.Initialize(statusDirector.Catalog, statusDirector.Interactions, eventBus);
            }

            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enemy.SetEventBus(eventBus);
            }

            if (abilityController != null)
            {
                abilityController.Initialize(abilityController.Definition, eventBus, generalTransform: abilityController.transform);
            }

            if (abilityHud != null)
            {
                abilityHud.Initialize(abilityController, eventBus);
            }
        }

        private void Awake()
        {
            Compose(new EventBus());
        }
    }
}
