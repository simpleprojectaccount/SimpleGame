using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  Vertigo.Utilities;
using static Vertigo.Utilities.SoundBank;

namespace Vertigo.Managers
{
    /// <summary>
    /// Handles sounds ingame
    /// </summary>
    public class SoundManager : Manager<SoundManager>
    {
        public SoundBank fxSoundBank;
        public AudioClip soundtrackInGame;
        public AudioClip soundtrackScoreScreen;

        private Sound _lastFx;
        private Dictionary<string, Sound> _fastSoundFx;
        private List<string> _fxToPlay;

        private AudioSource _soundTrackPlayer;
        private AudioSource _soundFXPlayer;
        private bool _isSoundtrackPlay;
        private bool _isFXPlay;
        private bool _isMusicToggleChanged;

        void Start()
        {
            InitSoundFXs();
            InitMusic();
        }

        void Update()
        {
            ControlMusic();
            ConsumeFX();
        }

        /// <summary>
        /// Prepare soundtrack player
        /// </summary>
        private void InitMusic()
        {
            _isSoundtrackPlay = true;
            _soundTrackPlayer = NewAudioSource("SoundTrack");
            if (soundtrackInGame && _soundTrackPlayer && !_soundTrackPlayer.clip)
                _soundTrackPlayer.clip = soundtrackInGame;
            _soundTrackPlayer.Play();
        }

        /// <summary>
        /// Prepare effect player, populate effect dictionary for ease of use and fast access. 
        /// </summary>
        private void InitSoundFXs()
        {
            _isFXPlay = true;
            _lastFx = Sound.EmptySound();
            _fastSoundFx = new Dictionary<string, Sound>();
            _fxToPlay = new List<string>();
            for (int i = 0; i < fxSoundBank.soundEffects.Count; i++)
                _fastSoundFx.Add(fxSoundBank.soundEffects[i].audio.name, fxSoundBank.soundEffects[i]);

            _soundFXPlayer = NewAudioSource("SoundFx");
        }

        private AudioSource NewAudioSource(string name)
        {
            var _go = new GameObject(name);
            _go.transform.SetParent(transform);
            return _go.AddComponent<AudioSource>();
        }

        /// <summary>
        /// Handle soundtrack player status
        /// </summary>
        private void ControlMusic()
        {
            // Early exit
            if (!_isMusicToggleChanged) return;

            // Turn the music on
            if (_isSoundtrackPlay == true)
            {
                _soundTrackPlayer.Play();
                _isMusicToggleChanged = false;
            }
            // Turn the music off
            else
            {
                _soundTrackPlayer.Pause();
                _isMusicToggleChanged = false;
            }
        }

        /// <summary>
        /// Priority queue implementation for sound fx's. Lowest priority (rotation sound) is a special case;
        /// It cannot be overruled, if it is playing, it plays until the end.
        /// </summary>
        private void ConsumeFX()
        {
            // Early exit.
            if (!_isFXPlay || !(_fxToPlay.Count > 0)) return;

            // Consume sound effect.
            string highestPriorityFxname = default;

            // Discover highest priority fx needed to play.
            for (int i = 0; i < _fxToPlay.Count; i++)
                if (_fastSoundFx[_fxToPlay[i]].priority >= _lastFx.priority)
                    highestPriorityFxname = _fastSoundFx[_fxToPlay[i]].audio.name;

            if (string.IsNullOrEmpty(highestPriorityFxname))
            {
                _lastFx.priority = 0;
                return;
            }
            Sound _currentSound = _fastSoundFx[highestPriorityFxname];
            if ((_currentSound.priority == _lastFx.priority) && 
                _currentSound.priority == 0 && _soundFXPlayer.isPlaying)
            {
                return;
            }
            _fxToPlay.Remove(highestPriorityFxname);

            if ((_currentSound.priority >= _lastFx.priority) && _currentSound.priority != 0)
            {
                _fxToPlay.Clear();
            }

            _soundFXPlayer.clip = _currentSound.audio;
            if (_currentSound.playDelay != default)
                _soundFXPlayer.PlayDelayed(_currentSound.playDelay);
            else
                _soundFXPlayer.Play();
            _lastFx = _currentSound;
        }

        public void ToggleMusic(bool play)
        {
            _isSoundtrackPlay = play;
            _isMusicToggleChanged = true;
        }

        public void ToggleFx(bool play)
        {
            _isFXPlay = play;
        }

        public void PlayFx(string fxName)
        {
            // Early exit.
            if (!_isFXPlay)
                return;
            _fxToPlay.Add(fxName);
        }

        public void PlayScoreScreenMusic()
        {
            // Early exit.
            if (!_isSoundtrackPlay) return;
            _soundTrackPlayer.Pause();
            _soundTrackPlayer.clip = soundtrackScoreScreen;
            _soundTrackPlayer.Play();
        }
    }
}