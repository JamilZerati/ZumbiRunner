using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Infrastructure;
using UnityEngine;

namespace Game.Presentation
{
    public class SquadVisualController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float spacing = 0.5f;
        [SerializeField, Min(1)] private int maxPerRow = 5;
        [SerializeField, Min(0.1f)] private float followSpeed = 15f;

        private Transform leaderTransform;
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
