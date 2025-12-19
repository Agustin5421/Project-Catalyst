using UnityEngine;

namespace ProjectCatalyst
{
    [RequireComponent(typeof(AudioSource))]
    public class AmbientMusic : MonoBehaviour
    {
        [Tooltip("The music clip to play.")]
        [SerializeField] private AudioClip musicClip;
        
        [Tooltip("Volume of the music (0 to 1).")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.5f;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            
            // Configure the audio source for 2D ambient music
            _audioSource.spatialBlend = 0f; // 0.0 makes it fully 2D
            _audioSource.loop = true;       // Loop the music
            _audioSource.playOnAwake = true; // Ensure it starts automatically
        }

        private void Start()
        {
            if (musicClip != null)
            {
                _audioSource.clip = musicClip;
            }

            _audioSource.volume = volume;

            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        // Optional: Method to change volume at runtime
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (_audioSource != null)
            {
                _audioSource.volume = volume;
            }
        }
    }
}
