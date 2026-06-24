using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Ọjà — the Marketplace (issue #38). Mirrors CrossroadsSystem: rival Houses surface at
    /// Àṣẹ milestones, one at a time, and wait patiently (persisted across save/load). The
    /// queue is implicit — "owed = milestones crossed − contests resolved this life" — so no
    /// stored queue is needed. A clash resolves via ContestResolver; renown is applied (floored
    /// at 0, so a loss never costs core progression) and the rate recomputed. Per-life cadence:
    /// pendingContest/contestsResolved reset at the Crossing.
    /// </summary>
    public class MarketplaceSystem : MonoBehaviour
    {
        [SerializeField] private ContestConfig _config;
        [SerializeField] private RemembranceConfig _remembranceConfig; // House name pool (reused)

        /// <summary>A rival House has surfaced and awaits a stance. Fires on Begin() if one
        /// survived a previous session, and each time the next owed contest is surfaced.</summary>
        public event Action<PendingContest> OnContestReady;

        /// <summary>A clash resolved (renown applied, state saved). Notification-only.</summary>
        public event Action<ContestOutcome> OnContestResolved;

        private SaveData _save;
        private IRandomSource _random = new UnityRandomSource();

        public bool HasPending => _save?.pendingContest != null;
        public PendingContest Pending => _save?.pendingContest;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy()
        {
            UnsubscribeAse();
            ServiceLocator.Unregister(this);
        }

        /// <summary>Test seam — production keeps the default UnityRandomSource.</summary>
        public void SetRandomSource(IRandomSource random) =>
            _random = random ?? throw new ArgumentNullException(nameof(random));

        /// <summary>Called by GameManager after the save is loaded.</summary>
        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            SubscribeAse();
            if (HasPending) { OnContestReady?.Invoke(_save.pendingContest); return; }
            EvaluateMilestone();
        }

        /// <summary>Re-checks milestones from current Àṣẹ. Tests call this directly after SetAse().</summary>
        public void EvaluateMilestone()
        {
            if (_save == null) return;
            CheckMilestones(_save.GetAse());
        }

        /// <summary>Odds the player would face with this stance — for the card to disclose
        /// before committing. 0 when no contest is pending.</summary>
        public double PreviewOdds(Stance playerStance)
        {
            var pc = _save?.pendingContest;
            if (pc == null) return 0.0;
            return ContestResolver.ComputeOdds(playerStance, (Stance)pc.houseStance, pc.housePowerRatio, _config);
        }

        /// <summary>Resolves the pending clash with the chosen stance. Applies renown (floored
        /// at 0), recomputes the rate, records the resolution, saves, then surfaces the next
        /// owed contest. Returns the outcome; no-op default when nothing is pending.</summary>
        public ContestOutcome ChooseStance(Stance playerStance)
        {
            var pc = _save?.pendingContest;
            if (pc == null || _config == null) return default;

            double odds = ContestResolver.ComputeOdds(playerStance, (Stance)pc.houseStance, pc.housePowerRatio, _config);
            ContestOutcome outcome = ContestResolver.Resolve(odds, _config, _random);

            _save.lineage.renown = Math.Max(0.0, _save.lineage.renown + outcome.RenownDelta); // floored — a loss never goes negative
            _save.contestsResolved++;
            _save.pendingContest = null;

            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen)) aseGen.RecalculateRate(); // renown changed → rate
            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save();

            OnContestResolved?.Invoke(outcome);
            EvaluateMilestone(); // surface the next owed contest, if any
            return outcome;
        }

        private void SubscribeAse()
        {
            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen))
            {
                aseGen.OnAseChanged -= HandleAseChanged;
                aseGen.OnAseChanged += HandleAseChanged;
            }
        }

        private void UnsubscribeAse()
        {
            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen))
                aseGen.OnAseChanged -= HandleAseChanged;
        }

        private void HandleAseChanged(BigNumber ase) => EvaluateMilestone();

        private void CheckMilestones(BigNumber ase)
        {
            if (_save == null || _config == null) return;
            if (_save.pendingContest != null) return; // one at a time; owed ones wait their turn
            if (_config.CountMilestonesCrossed(ase) > _save.contestsResolved)
                Surface();
        }

        private void Surface()
        {
            string[] names = _remembranceConfig != null ? _remembranceConfig.personalNames : null;
            int pathCount = ServiceLocator.TryGet(out CultivationSystem cultivation) ? cultivation.Paths.Length : 1;
            House house = HouseGenerator.Generate(names, pathCount, _config, _random);

            _save.pendingContest = new PendingContest
            {
                houseName = house.Name,
                housePath = house.PathIndex,
                housePowerRatio = house.PowerRatio,
                houseStance = (int)house.Stance,
            };
            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save();
            OnContestReady?.Invoke(_save.pendingContest);
        }
    }
}
