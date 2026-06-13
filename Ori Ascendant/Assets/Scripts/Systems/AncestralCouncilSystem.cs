using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Ancestral Council management (TECH_DESIGN §4): the active-council bonus
    /// sum consumed by AseGenerationSystem, and the Àṣẹ-neutral retirement rule.
    ///
    /// OWNERSHIP NOTE (deviation from TECH_DESIGN's "listens for
    /// OnTribulationComplete"): council mutation happens via the synchronous
    /// <see cref="InductAncestor"/> call INSIDE TribulationSystem's atomic
    /// resolve — an event-driven mutation would run after the save was written,
    /// persisting an over-full council and breaking the persist-first
    /// crash-safety rule (GAMEPLAY §3.5). OnTribulationComplete is therefore a
    /// notification for UI only; this system owns the MATH, not the trigger.
    /// </summary>
    public class AncestralCouncilSystem : MonoBehaviour
    {
        [SerializeField] private CouncilConfig _config;

        /// <summary>Raised after an ancestor is retired into the lineage foundation.</summary>
        public event Action<AncestorData> OnAncestorRetired;

        /// <summary>Raised after any council mutation (induction or retirement).</summary>
        public event Action OnCouncilChanged;

        /// <summary>Cheap change counter for polling views.</summary>
        public int Version { get; private set; }

        private SaveData _save;

        public double W => _config != null ? _config.ancestorBaseBonus : 0.25;
        public int MaxCouncil => _config != null ? _config.maxCouncil : 5;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            Version++;
        }

        /// <summary>Σ(W × bonusMultiplier) over the ACTIVE council — the live term
        /// in the rate formula. Retired ancestors live in lineage.permanentAseBonus.</summary>
        public double ActiveCouncilSum
        {
            get
            {
                if (_save == null) return 0.0;
                double sum = 0.0;
                for (int i = 0; i < _save.council.Count; i++)
                {
                    sum += W * _save.council[i].bonusMultiplier;
                }
                return sum;
            }
        }

        /// <summary>
        /// Adds a completed cultivator to the council, retiring the OLDEST member
        /// first when full (GAMEPLAY §4.4 order: retire BEFORE the new ancestor
        /// lands). Retirement moves W × bonusMultiplier from the active sum into
        /// lineage.permanentAseBonus — both terms live inside the same
        /// (1 + councilBonusModifier × (…)) wrap, so the rate is IDENTICAL before
        /// and after on every path, including Osun. Returns the retired ancestor,
        /// or null if the council had room.
        /// Caller (TribulationSystem) is responsible for the rate recompute and
        /// the save — this stays a pure state mutation inside the atomic write.
        /// </summary>
        public AncestorData InductAncestor(AncestorData ancestor)
        {
            if (_save == null || ancestor == null) return null;

            AncestorData retired = null;
            if (_save.council.Count >= MaxCouncil)
            {
                retired = RetireOldest();
            }

            _save.council.Add(ancestor);
            Version++;
            OnCouncilChanged?.Invoke();
            return retired;
        }

        private AncestorData RetireOldest()
        {
            int oldestIndex = 0;
            for (int i = 1; i < _save.council.Count; i++)
            {
                if (_save.council[i].completedTimestamp < _save.council[oldestIndex].completedTimestamp)
                {
                    oldestIndex = i;
                }
            }

            AncestorData oldest = _save.council[oldestIndex];
            _save.lineage.permanentAseBonus += W * oldest.bonusMultiplier; // "settles into the foundation"
            _save.council.RemoveAt(oldestIndex);
            Version++;
            OnAncestorRetired?.Invoke(oldest);
            return oldest;
        }
    }
}
