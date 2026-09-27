using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Gameplay;
using Game.Infrastructure;
using UnityEngine;

namespace Game.Presentation
{
    public class SquadVisualController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float spacing = 0.5f;
        [SerializeField, Min(1)] private int maxPerRow = 5;
        [SerializeField, Min(0.1f)] private float followSpeed = 15f;

        [SerializeField] private Transform leaderTransform;
        [SerializeField] private SoldierView soldierPrefab;
        [SerializeField] private SquadController squadController;

        private IObjectPool<SoldierView> soldierPool;
        private IEventBus eventBus;
        private IDisposable squadSizeSubscription;
        private readonly List<SoldierView> activeSoldiers = new List<SoldierView>();

        public IReadOnlyList<SoldierView> ActiveSoldiers => activeSoldiers;
        public float Spacing => spacing;
        public int MaxPerRow => maxPerRow;
        public float FollowSpeed => followSpeed;

        public void Initialize(Transform leader, IObjectPool<SoldierView> pool, IEventBus bus = null, float soldierSpacing = 0.5f, int soldiersPerRow = 5)
        {
            if (squadSizeSubscription != null)
            {
                squadSizeSubscription.Dispose();
                squadSizeSubscription = null;
            }

            leaderTransform = leader;
            soldierPool = pool;
            eventBus = bus;
            spacing = soldierSpacing;
            maxPerRow = soldiersPerRow;

            if (eventBus != null)
            {
                squadSizeSubscription = eventBus.Subscribe<SquadSizeChangedEvent>(OnSquadSizeChanged);
            }
        }

        private void OnSquadSizeChanged(SquadSizeChangedEvent evt)
        {
            SynchronizeSquad(evt.NewCount);
        }

        public void SynchronizeSquad(int targetCount)
        {
            if (soldierPool == null)
            {
                return;
            }

            targetCount = Mathf.Max(0, targetCount);
            Vector3 anchorPos = leaderTransform != null ? leaderTransform.position : transform.position;

            while (activeSoldiers.Count < targetCount)
            {
                var soldier = soldierPool.Rent();
                soldier.gameObject.SetActive(true);
                soldier.SetTargetOffset(Vector3.zero);
                soldier.SnapToTarget(anchorPos);
                activeSoldiers.Add(soldier);
            }

            while (activeSoldiers.Count > targetCount)
            {
                int lastIdx = activeSoldiers.Count - 1;
                var soldier = activeSoldiers[lastIdx];
                activeSoldiers.RemoveAt(lastIdx);
                soldier.SetTargetOffset(Vector3.zero);
                soldier.gameObject.SetActive(false);
                soldierPool.Return(soldier);
            }

            var positions = FormationSolver.CalculatePositions(activeSoldiers.Count, spacing, maxPerRow);
            for (int i = 0; i < activeSoldiers.Count; i++)
            {
                activeSoldiers[i].SetTargetOffset(new Vector3(positions[i].X, 0f, positions[i].Z));
            }
        }

        private void Start()
        {
            if (leaderTransform == null)
            {
                leaderTransform = transform.parent != null ? transform.parent : transform;
            }

            if (leaderTransform != null)
            {
                var leaderRend = leaderTransform.GetComponent<Renderer>();
                if (leaderRend != null && (leaderRend.sharedMaterial == null || leaderRend.sharedMaterial.color == Color.white))
                {
                    var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    if (litShader != null)
                    {
                        leaderRend.material = new Material(litShader) { color = new Color(0.15f, 0.55f, 0.95f) };
                    }
                }
            }

            if (squadController == null && leaderTransform != null)
            {
                squadController = leaderTransform.GetComponent<SquadController>() ?? GetComponentInParent<SquadController>();
            }

            if (soldierPrefab == null)
            {
                soldierPrefab = FindFirstObjectByType<SoldierView>(FindObjectsInactive.Include);
            }

            if (soldierPrefab != null)
            {
                var soldierRend = soldierPrefab.GetComponent<Renderer>();
                if (soldierRend != null && (soldierRend.sharedMaterial == null || soldierRend.sharedMaterial.color == Color.white))
                {
                    var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    if (litShader != null)
                    {
                        soldierRend.material = new Material(litShader) { color = new Color(0.2f, 0.85f, 0.45f) };
                    }
                }
            }

            if (soldierPool == null)
            {
                soldierPool = new ObjectPool<SoldierView>(
                    factory: () =>
                    {
                        if (soldierPrefab != null)
                        {
                            return Instantiate(soldierPrefab, transform);
                        }

                        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.name = "Soldier";
                        sphere.transform.SetParent(transform, false);
                        sphere.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                        var rend = sphere.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                            if (shader != null)
                            {
                                rend.sharedMaterial = new Material(shader) { color = new Color(0.2f, 0.85f, 0.45f) };
                            }
                        }
                        return sphere.AddComponent<SoldierView>();
                    },
                    onRent: s => s.gameObject.SetActive(true),
                    onReturn: s => s.gameObject.SetActive(false),
                    initialCapacity: 5
                );

                if (squadController != null)
                {
                    squadController.SquadSizeChanged += OnSquadSizeChanged;
                    SynchronizeSquad(squadController.SquadCount);
                }
                else
                {
                    SynchronizeSquad(3);
                }
            }
        }

        private void Update()
        {
            UpdatePositions(Time.deltaTime);
        }

        public void UpdatePositions(float deltaTime)
        {
            if (leaderTransform == null)
            {
                return;
            }

            Vector3 leaderPos = leaderTransform.position;
            for (int i = 0; i < activeSoldiers.Count; i++)
            {
                var soldier = activeSoldiers[i];
                if (soldier != null)
                {
                    soldier.UpdatePosition(leaderPos, deltaTime, followSpeed);
                }
            }
        }

        private void OnDestroy()
        {
            if (squadSizeSubscription != null)
            {
                squadSizeSubscription.Dispose();
                squadSizeSubscription = null;
            }

            if (squadController != null)
            {
                squadController.SquadSizeChanged -= OnSquadSizeChanged;
            }

            if (soldierPool != null)
            {
                for (int i = 0; i < activeSoldiers.Count; i++)
                {
                    var soldier = activeSoldiers[i];
                    if (soldier != null)
                    {
                        soldier.SetTargetOffset(Vector3.zero);
                        soldier.gameObject.SetActive(false);
                        soldierPool.Return(soldier);
                    }
                }
            }

            activeSoldiers.Clear();
        }
    }
}
