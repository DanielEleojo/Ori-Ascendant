using System;
using System.IO;
using OriAscendant.Core;
using OriAscendant.Data;
using UnityEngine;

namespace OriAscendant.Save
{
    /// <summary>
    /// Local JSON persistence at persistentDataPath/save.json (TECH_DESIGN §4).
    /// Save triggers: OnApplicationPause(true) — the reliable iOS hook —, an
    /// autosave timer (GameplayConfig.autosaveIntervalSeconds), progression
    /// events (callers invoke <see cref="Save"/>), and OnApplicationQuit as an
    /// editor/desktop convenience. NEVER saves per tick.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private GameplayConfig _config;

        /// <summary>The live save — single source of truth once loaded.</summary>
        public SaveData Current { get; private set; }

        private string _savePath;
        private string _tempPath;
        private float _sinceAutosave;

        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "save.json");
            _tempPath = _savePath + ".tmp";
            ServiceLocator.Register(this);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        /// <summary>Loads from disk, falling back to a fresh SaveData when the file
        /// is missing or corrupt. Idempotent: repeat calls return the live instance.</summary>
        public SaveData Load()
        {
            if (Current != null) return Current;

            SaveData loaded = null;
            try
            {
                if (File.Exists(_savePath))
                {
                    loaded = SaveSerializer.FromJson(File.ReadAllText(_savePath));
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"SaveManager: failed to read save, starting fresh. {e.Message}");
            }

            Current = loaded ?? new SaveData();
            return Current;
        }

        /// <summary>Replaces the live save (cloud adoption only — a strictly-newer
        /// cloud save won the conflict resolve). Callers re-seat their systems.</summary>
        public void Adopt(SaveData adopted)
        {
            if (adopted != null) Current = adopted;
        }

        /// <summary>Stamps lastSaveTimestamp (Unix UTC — the offline-calc anchor)
        /// and writes atomically (temp file + replace) so a mid-write kill can
        /// never corrupt the existing save.</summary>
        public void Save()
        {
            if (Current == null) return;

            Current.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                File.WriteAllText(_tempPath, SaveSerializer.ToJson(Current));
                if (File.Exists(_savePath))
                {
                    File.Replace(_tempPath, _savePath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(_tempPath, _savePath);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"SaveManager: save failed. {e.Message}");
            }
        }

        private void Update()
        {
            if (Current == null || _config == null) return;

            _sinceAutosave += Time.unscaledDeltaTime;
            if (_sinceAutosave >= _config.autosaveIntervalSeconds)
            {
                _sinceAutosave = 0f;
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // iOS suspends rather than quits; pause(true) is the reliable moment
            // to persist. Local write only — cloud pushes happen while foregrounded.
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();
    }
}
