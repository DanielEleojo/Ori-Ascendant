using System;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Core
{
    /// <summary>
    /// App lifecycle orchestration (TECH_DESIGN §4): load → offline progress →
    /// begin ticking. Owns NO game state. Offline progress runs on cold start
    /// AND on resume-from-background (iOS apps resume far more often than they
    /// cold-launch); the Welcome Back modal listens to the offline event and
    /// applies the ≥60s display threshold itself.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameplayConfig _config;

        private SaveManager _saveManager;
        private AseGenerationSystem _aseGeneration;
        private CultivationSystem _cultivation;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        private void Start()
        {
            _saveManager = ServiceLocator.Get<SaveManager>();
            _aseGeneration = ServiceLocator.Get<AseGenerationSystem>();
            _cultivation = ServiceLocator.Get<CultivationSystem>();

            SaveData save = _saveManager.Load();

            // Order matters: cultivation first (the offline modifier and rate
            // inputs come from it), then AseGen, then offline apply at the
            // CACHED rate the player left with, then recalc for the live session.
            _cultivation.Begin(save);
            if (ServiceLocator.TryGet(out AncestralCouncilSystem council)) council.Begin(save);
            if (ServiceLocator.TryGet(out TribulationSystem tribulation)) tribulation.Begin(save);
            _aseGeneration.Begin(save);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Two intents (issue #17): a fresh save STAMPS the timestamps with
            // zero Àṣẹ credited; any existing save runs resume accrual.
            if (save.lastSaveTimestamp == 0)
            {
                OfflineProgressCalculator.InitializeFirstLaunch(save, now);
            }
            else
            {
                OfflineProgressCalculator.ApplyAccrual(save, now, _cultivation.PathOfflineRateModifier);
            }
            _aseGeneration.RecalculateRate();

            // Persist immediately so a fresh install has a save file (and its
            // first-launch timestamps) on disk before anything else happens.
            _saveManager.Save();

            // Cloud reconcile runs in the BACKGROUND — gameplay is already live
            // on the local save and never waits on auth. Only a strictly-newer
            // cloud save (device swap) triggers adoption. Inert in the editor.
            if (ServiceLocator.TryGet(out CloudSaveManager cloud))
            {
                cloud.OnCloudSaveAdopted += AdoptCloudSave;
                cloud.BeginBackgroundReconcile(save);
            }
        }

        /// <summary>Rare path: a strictly-newer cloud save arrived after launch.
        /// Re-seat every system on it and refresh the rate. Never fires in the
        /// editor (Null provider).</summary>
        private void AdoptCloudSave(SaveData cloud)
        {
            _saveManager.Adopt(cloud);
            _cultivation.Begin(cloud);
            if (ServiceLocator.TryGet(out AncestralCouncilSystem council)) council.Begin(cloud);
            if (ServiceLocator.TryGet(out TribulationSystem tribulation)) tribulation.Begin(cloud);
            _aseGeneration.Begin(cloud);
            _aseGeneration.RecalculateRate();
            _saveManager.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            // pause(true) saving is SaveManager's job. On resume, credit the
            // suspended time through the same pure offline path. The modifier is
            // the live path's offlineRateModifier — it scales the RATE, never the cap.
            if (paused || _saveManager?.Current == null) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Resume always means accrual — Start() has already stamped the
            // timestamps via InitializeFirstLaunch for a fresh save, so this
            // path can never see lastSaveTimestamp == 0.
            OfflineProgressCalculator.ApplyAccrual(_saveManager.Current, now,
                _cultivation != null ? _cultivation.PathOfflineRateModifier : 1.0);
            _aseGeneration.ResyncAfterResume();
        }
    }
}
