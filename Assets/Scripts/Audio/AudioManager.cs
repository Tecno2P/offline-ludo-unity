using UnityEngine;

namespace LudoGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        private AudioSource _sfxSource;

        public float SfxVolume = 1f;
        public float MusicVolume = 1f;

        private AudioClip _diceRoll, _tokenMove, _capture, _click, _turnNotify, _victory, _join, _leave, _gameStart;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AudioManager>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        private void Init()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();

            // Generate every clip once up front - synthesis is cheap but there's no reason
            // to redo it per-play.
            _diceRoll = ProceduralAudio.DiceRoll();
            _tokenMove = ProceduralAudio.TokenMove();
            _capture = ProceduralAudio.Capture();
            _click = ProceduralAudio.ButtonClick();
            _turnNotify = ProceduralAudio.TurnNotification();
            _victory = ProceduralAudio.Victory();
            _join = ProceduralAudio.PlayerJoin();
            _leave = ProceduralAudio.PlayerLeave();
            _gameStart = ProceduralAudio.GameStart();
        }

        private void Play(AudioClip clip) => _sfxSource.PlayOneShot(clip, SfxVolume);

        public void PlayDiceRoll() => Play(_diceRoll);
        public void PlayTokenMove() => Play(_tokenMove);
        public void PlayCapture() => Play(_capture);
        public void PlayButtonClick() => Play(_click);
        public void PlayTurnNotification() => Play(_turnNotify);
        public void PlayVictory() => Play(_victory);
        public void PlayPlayerJoin() => Play(_join);
        public void PlayPlayerLeave() => Play(_leave);
        public void PlayGameStart() => Play(_gameStart);
    }
}
