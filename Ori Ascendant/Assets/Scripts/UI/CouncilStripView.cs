using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// MainScreen zone 7: the five council slots rendered as a constellation of
    /// ancestor-stars (issue #22). Path colour + brightness (ascended / fallen /
    /// empty) is driven by ConstellationStarMapper; the skin turns the slot
    /// Images into soft dot-sprites so they read as stars, not chips.
    /// Polls the council Version counter; display-only except the tap into
    /// CouncilScreen.
    /// </summary>
    public class CouncilStripView : MonoBehaviour
    {
        [SerializeField] private Image[] _slots; // 5
        [SerializeField] private Button _stripButton;
        [SerializeField] private CouncilScreenView _councilScreen;

        private AncestralCouncilSystem _council;
        private SaveManager _saveManager;
        private int _lastVersion = -1;

        private void Awake()
        {
            if (_stripButton != null) _stripButton.onClick.AddListener(OpenCouncilScreen);
        }

        private void OnDestroy()
        {
            if (_stripButton != null) _stripButton.onClick.RemoveListener(OpenCouncilScreen);
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _council);
            ServiceLocator.TryGet(out _saveManager);
        }

        private void Update()
        {
            if (_council == null || _council.Version == _lastVersion) return;
            _lastVersion = _council.Version;
            Refresh();
        }

        private void Refresh()
        {
            var save = _saveManager?.Current;
            if (save == null || _slots == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;
                _slots[i].color = i < save.council.Count
                    ? ConstellationStarMapper.StarColor(save.council[i])
                    : ConstellationStarMapper.EmptySeatColor();
            }
        }

        private void OpenCouncilScreen()
        {
            if (_councilScreen != null) _councilScreen.Show();
        }
    }
}
