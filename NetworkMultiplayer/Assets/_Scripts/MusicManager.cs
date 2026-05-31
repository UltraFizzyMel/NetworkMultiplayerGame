using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SFXType
{
    // Pickup sounds
    PickupBucket,
    PickupDuctTape,
    DropItem,

    // Interaction sounds
    WaterInBucket,
    WaterOutBucket,
    GeneratorFixed,
    GeneratorBreak,

    // Gameplay sounds
    Swop,
    SwopWarning,
    Leak,
    GeneratorRunning,
    Crash,

    // Win/Lose sounds
    Win,
    Lose
}

public enum LoopType
{
    Leak,
    GeneratorRunning
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Assign these in Inspector")]
    public AudioSource musicSourceA;  // Drag first Audio Source here
    public AudioSource musicSourceB;  // Drag second Audio Source here
    public AudioSource sfxSource;
    public AudioClip _lobbyMusic;     // Assign lobby/main menu music clip
    public AudioClip _gameMusic;      // Assign in-game music clip

    private AudioSource _activeSource;
    private AudioSource _inactiveSource;
    private string _currentSongName; // Track what's playing

    private bool _isCrossfading = false;

    public float _duration = 5f; // Expose crossfade duration for tweaking

    [Header("SFX Library")]
    public List<SFXEntry> sfxLibrary;

    private Dictionary<SFXType, AudioClip> _sfxDictionary;
    private string _currentSongID;

    [System.Serializable]
    public class SFXEntry
    {
        public SFXType type;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.5f, 2f)]
        public float pitch = 1f;
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSFXDictionary();  
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Set up initial active source
        _activeSource = musicSourceA;
        _inactiveSource = musicSourceB;

        // Configure both for looping background music
        musicSourceA.loop = true;
        musicSourceB.loop = true;
        musicSourceA.playOnAwake = false;
        musicSourceB.playOnAwake = false;
    }

    private void InitializeSFXDictionary()
    {
        _sfxDictionary = new Dictionary<SFXType, AudioClip>();
        foreach (var entry in sfxLibrary)
        {
            if (!_sfxDictionary.ContainsKey(entry.type))
            {
                _sfxDictionary.Add(entry.type, entry.clip);
            }
        }
    }


    void Start()
    {
        // Start playing the lobby/main menu music
        PlayContinuousMusic(_lobbyMusic, "MainMenuLobbyMusic");
    }

    public void PlayContinuousMusic(AudioClip clip, string songID)
    {
        // If this exact song is already playing, do nothing
        if (_currentSongName == songID && _activeSource.isPlaying)
            return;

        // If no music is playing, start it
        if (!_activeSource.isPlaying)
        {
            _activeSource.clip = clip;
            _activeSource.Play();
            _currentSongName = songID;
        }
        // If different song is playing, crossfade
        else if (_currentSongName != songID)
        {
            CrossfadeToNewSong(clip, songID);
        }
    }

    public void CrossfadeToNewSong(AudioClip newClip, string newSongID)
    {
        if (_isCrossfading) return;
        StartCoroutine(CrossfadeCoroutine(newClip, newSongID, _duration));
    }

    private IEnumerator CrossfadeCoroutine(AudioClip newClip, string newSongID, float duration)
    {
        _isCrossfading = true;

        // Setup inactive source with new clip
        _inactiveSource.clip = newClip;
        _inactiveSource.volume = 0f;
        _inactiveSource.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            _activeSource.volume = Mathf.Lerp(1f, 0f, t);
            _inactiveSource.volume = Mathf.Lerp(0f, 1f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Complete the swap
        _activeSource.Stop();
        _activeSource.volume = 1f;

        // Swap references
        var temp = _activeSource;
        _activeSource = _inactiveSource;
        _inactiveSource = temp;

        _currentSongName = newSongID;
        _isCrossfading = false;
    }

    // Play SFX by type
    public void PlaySFX(SFXType sfxType)
    {
        if (_sfxDictionary.TryGetValue(sfxType, out AudioClip clip))
        {
            // Find volume/pitch settings for this SFX
            var entry = sfxLibrary.Find(e => e.type == sfxType);

            if (entry != null)
            {
                // Apply pitch variation (optional)
                float originalPitch = sfxSource.pitch;
                sfxSource.pitch = entry.pitch;
                sfxSource.PlayOneShot(clip, entry.volume);
                sfxSource.pitch = originalPitch;
            }
            else
            {
                sfxSource.PlayOneShot(clip);
            }
        }
        else
        {
            Debug.LogWarning($"SFX type {sfxType} not found in library!");
        }
    }
}
