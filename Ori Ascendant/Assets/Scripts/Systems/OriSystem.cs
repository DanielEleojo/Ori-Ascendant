using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Àkùnlẹ̀yàn — the at-birth virtue vow (Dynasty PRD Phase 1, slice 1).
    /// Owns SaveData.chosenOri writes; the UI never touches the field directly.
    /// One choice per generation; TribulationSystem resets the field to -1
    /// inside its atomic Resolve, then the next life surfaces the modal again.
    /// </summary>
    public class OriSystem : MonoBehaviour
    {
        [SerializeField] private OriConfig _config;

        /// <summary>Raised once per generation when an Ori is committed (virtue index).</summary>
        public event Action<int> OnOriChosen;

        private SaveData _save;

        public OriConfig Config => _config;

        /// <summary>True once the player has committed this life's vow.</summary>
        public bool HasChosen => _save != null && _save.chosenOri >= 0;

        /// <summary>The live virtue index, or -1 when no vow is held yet.</summary>
        public int ChosenIndex => _save != null ? _save.chosenOri : -1;

        /// <summary>The live virtue, or null when no vow is held yet (or config is missing).</summary>
        public OriVirtue ChosenVirtue =>
            _save != null && _config != null ? _config.GetVirtue(_save.chosenOri) : null;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        /// <summary>Called by GameManager after the save is loaded.</summary>
        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        /// <summary>Commits the life's vow. One-shot per generation: rejects when
        /// already chosen or the index is out of range. Writes through the SaveManager
        /// (progression event), so the choice survives a crash before the next save.</summary>
        public bool ChooseOri(int virtueIndex)
        {
            if (_save == null || _config == null) return false;
            if (_save.chosenOri >= 0) return false; // already vowed for this life
            if (virtueIndex < 0 || virtueIndex >= _config.Count) return false;

            _save.chosenOri = virtueIndex;
            OnOriChosen?.Invoke(virtueIndex);

            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save();
            return true;
        }
    }
}
