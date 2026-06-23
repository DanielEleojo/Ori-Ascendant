using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Passive Àṣẹ production (TECH_DESIGN §4). Owns the 1-second logical tick
    /// (Update() frame-accumulator — amended §6 decision) and is the SOLE WRITER
    /// of the cached asePerSecond in SaveData via <see cref="RecalculateRate"/>.
    /// Also owns the tap-to-channel grant (GAMEPLAY §5.3).
    /// Idle until <see cref="Begin"/> hands it the loaded save (GameManager
    /// orchestrates, so Start-order races cannot happen).
    /// </summary>
    public class AseGenerationSystem : MonoBehaviour
    {
        [SerializeField] private GameplayConfig _config;

        /// <summary>Raised after any Àṣẹ total change (tick, channel, offline refresh).</summary>
        public event Action<BigNumber> OnAseChanged;

        /// <summary>Raised after the cached production rate is rewritten.</summary>
        public event Action<BigNumber> OnRateRecalculated;

        /// <summary>Raised when a channel tap grants Àṣẹ (the granted amount).</summary>
        public event Action<BigNumber> OnAseChanneled;

        /// <summary>Cheap change counter so views can poll without event-order races.</summary>
        public int StateVersion { get; private set; }

        public BigNumber CurrentAse => _save?.GetAse() ?? BigNumber.Zero;
        public BigNumber CurrentRate => _save?.GetAsePerSecond() ?? BigNumber.Zero;

        private readonly TickAccumulator _ticker = new TickAccumulator(1.0);
        private SaveData _save;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        /// <summary>Called once by GameManager after the save is loaded.</summary>
        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _ticker.Reset();
            StateVersion++;
        }

        /// <summary>Drops partial tick progress after an app resume so suspended
        /// wall-time never double-counts with the offline calculation.</summary>
        public void ResyncAfterResume()
        {
            _ticker.Reset();
            StateVersion++;
        }

        private void Update()
        {
            if (_save == null) return;

            int ticks = _ticker.Advance(Time.unscaledDeltaTime);
            if (ticks <= 0) return;

            BigNumber rate = _save.GetAsePerSecond();
            if (!rate.IsZero)
            {
                _save.SetAse(_save.GetAse() + rate * (double)ticks);
            }
            StateVersion++;
            OnAseChanged?.Invoke(_save.GetAse());
        }

        /// <summary>
        /// Recomputes and caches the production rate (full GAMEPLAY §2.1 formula).
        /// Stage/path inputs come from CultivationSystem (currentPath == -1 reads
        /// as 1.0 everywhere); the council sum arrives in Phase C. Called
        /// imperatively at the four real recompute sites: GameManager cold start,
        /// GameManager cloud-save adoption, CultivationSystem stage advance, and
        /// TribulationSystem resolve (synchronous council mutation lives in that
        /// atomic write — there is no event-driven council-change path).
        /// TryGet keeps bare test hosts neutral.
        /// </summary>
        public void RecalculateRate()
        {
            if (_save == null) return;
            if (_config == null)
            {
                Debug.LogError("AseGenerationSystem: _config not assigned — rate cannot be computed.");
                return;
            }

            double stageMultiplier = 1.0;
            double pathMultiplier = 1.0;
            double councilBonusModifier = 1.0;
            if (ServiceLocator.TryGet(out CultivationSystem cultivation))
            {
                stageMultiplier = cultivation.StageProductionMultiplier;
                pathMultiplier = cultivation.PathOnlineMultiplier;
                councilBonusModifier = cultivation.CouncilBonusModifier;
            }
            double activeCouncilSum = 0.0;
            if (ServiceLocator.TryGet(out AncestralCouncilSystem council))
            {
                activeCouncilSum = council.ActiveCouncilSum;
            }

            double renownBonus = LineageRenown.ToBonus(_save.lineage.renown, _config.renownBonusCap);

            var inputs = new RateInputs(
                _config.baseRate,
                stageMultiplier,
                pathMultiplier,
                councilBonusModifier,
                _save.lineage.permanentAseBonus,
                activeCouncilSum,
                renownBonus);
            BigNumber rate = RateCalculator.ComputeRate(in inputs);

            _save.SetAsePerSecond(rate);
            StateVersion++;
            OnRateRecalculated?.Invoke(rate);
        }

        /// <summary>Tap-to-channel: grants tapChannelSeconds of current production.
        /// Reuses the full cached rate, so stage/path/council all flow through.</summary>
        public void ChannelTap()
        {
            if (_save == null) return;

            BigNumber rate = _save.GetAsePerSecond();
            if (rate.IsZero) return;

            BigNumber granted = rate * _config.tapChannelSeconds;
            _save.SetAse(_save.GetAse() + granted);
            StateVersion++;
            OnAseChanneled?.Invoke(granted);
            OnAseChanged?.Invoke(_save.GetAse());
        }
    }
}
