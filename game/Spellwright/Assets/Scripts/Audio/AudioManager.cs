using Spellwright.Core;
using Spellwright.Data;
using UnityEngine;

namespace Spellwright.Audio
{
    /// <summary>
    /// Manages all game audio: ambient CRT hum, keyboard/input feedback, and
    /// screen transition effects. Subscribes to EventBus events for automatic
    /// audio triggers.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; set; }

        [Header("Ambient")]
        [SerializeField] private AudioClip ambientHumClip;
        [SerializeField, Range(0f, 0.3f)] private float ambientHumVolume = 0.07f;

        [Header("Keyboard Feedback")]
        [SerializeField] private AudioClip[] keyPressSounds;
        [SerializeField] private AudioClip[] errorSounds;
        [SerializeField, Range(0f, 1f)] private float keyPressVolume = 0.4f;
        [SerializeField, Range(0f, 1f)] private float errorVolume = 0.5f;

        [Header("Transitions")]
        [SerializeField] private AudioClip bootSound;
        [SerializeField] private AudioClip shutdownSound;
        [SerializeField] private AudioClip degaussSound;
        [SerializeField, Range(0f, 1f)] private float transitionVolume = 0.6f;

        [Header("UI Feedback")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.3f;

        private AudioSource _ambientSource;
        private AudioSource _sfxSource;
        private AudioSource _transitionSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
            _ambientSource.volume = ambientHumVolume;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _transitionSource = gameObject.AddComponent<AudioSource>();
            _transitionSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            var bus = EventBus.Instance;
            bus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            bus.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            bus.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            bus.Subscribe<LetterRevealedEvent>(OnLetterRevealed);
            bus.Subscribe<RunStartedEvent>(OnRunStarted);
            bus.Subscribe<RunEndedEvent>(OnRunEnded);
        }

        private void OnDisable()
        {
            var bus = EventBus.Instance;
            bus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            bus.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            bus.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            bus.Unsubscribe<LetterRevealedEvent>(OnLetterRevealed);
            bus.Unsubscribe<RunStartedEvent>(OnRunStarted);
            bus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Event Handlers ──────────────────────────────────

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case GameState.Encounter:
                case GameState.Boss:
                    PlayTransition(bootSound);
                    StartAmbientHum();
                    break;

                case GameState.Map:
                case GameState.Shop:
                    // Keep hum running during map/shop
                    break;

                case GameState.MainMenu:
                case GameState.RunEnd:
                    PlayTransition(shutdownSound);
                    StopAmbientHum();
                    break;
            }
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            if (evt.Result == null) return;

            bool isCorrect = evt.Result.IsLetterInPhrase || evt.Result.IsCorrect;
            if (isCorrect)
                PlayKeyPress();
            else
                PlayError();
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            if (evt.Won)
                PlayTransition(degaussSound);
        }

        private void OnLetterRevealed(LetterRevealedEvent evt)
        {
            PlayKeyPress();
        }

        private void OnRunStarted(RunStartedEvent evt)
        {
            PlayTransition(bootSound);
            StartAmbientHum();
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            PlayTransition(shutdownSound);
            StopAmbientHum();
        }

        // ── Public API (for manual triggers like button hover/click) ──

        public void PlayKeyPress()
        {
            PlayRandom(keyPressSounds, keyPressVolume);
        }

        public void PlayError()
        {
            PlayRandom(errorSounds, errorVolume);
        }

        public void PlayButtonClick()
        {
            PlayOneShot(buttonClickSound, uiVolume);
        }

        public void PlayButtonHover()
        {
            PlayOneShot(buttonHoverSound, uiVolume);
        }

        public void StartAmbientHum()
        {
            if (ambientHumClip == null || _ambientSource.isPlaying) return;
            _ambientSource.clip = ambientHumClip;
            _ambientSource.volume = ambientHumVolume;
            _ambientSource.Play();
        }

        public void StopAmbientHum()
        {
            _ambientSource.Stop();
        }

        // ── Helpers ──────────────────────────────────────────

        private void PlayTransition(AudioClip clip)
        {
            if (clip == null) return;
            _transitionSource.PlayOneShot(clip, transitionVolume);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, volume);
        }

        private void PlayRandom(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
