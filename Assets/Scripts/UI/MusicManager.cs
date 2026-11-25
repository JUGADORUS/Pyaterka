using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioClip[] _musicClips;
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioSource _pickSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (_musicClips.Length > 0)
        {
            Switch(0);
        }
    }

    public void Switch(int index)
    {
        if (index < 0 || index >= _musicClips.Length || _source == null || _musicClips[index] == null) return;
        if (_source.clip == _musicClips[index]) return;
        _source.clip = _musicClips[index];
        _source.Stop();
        _source.Play();
    }

    public void PickCar()
    {
        if (_pickSound != null) _pickSound.Play();
    }
}
