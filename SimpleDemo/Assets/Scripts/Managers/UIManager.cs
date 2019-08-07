using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vertigo.Utilities;

namespace Vertigo.Managers
{
    /// <summary>
    /// Handles the UI actions
    /// </summary>
    public class UIManager : Manager<UIManager>
    {
#pragma warning disable 0649
        [SerializeField]
        private TextMeshProUGUI scoreText;

        [SerializeField]
        private TextMeshProUGUI _endGameText;

        [SerializeField]
        private Toggle _playMusicUI;

        [SerializeField]
        private Toggle _playSoundFXUI;

        [SerializeField]
        private Button _restartButton;

        [SerializeField]
        private InputReceiver _inputReceiver;

        [SerializeField]
        private Animation animator;
#pragma warning restore 0649

        protected override void Awake()
        {
            base.Awake();
            _restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }

        public void ToggleSettingGroup(bool isSettingGroupOn)
        {
            _playMusicUI.gameObject.SetActive(isSettingGroupOn);
            _playSoundFXUI.gameObject.SetActive(isSettingGroupOn);
        }

        public void UpdateScore(int score)
        {
            scoreText.text = score.ToString();
        }

        public void EndGame(int score, int highscore)
        {
            _inputReceiver.gameObject.SetActive(false);
            _endGameText.text = string.Format(_endGameText.text, score, highscore); 
            animator.Play("EndAnim");
        }
    }
}