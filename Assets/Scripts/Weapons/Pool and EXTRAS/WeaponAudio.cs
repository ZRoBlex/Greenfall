using UnityEngine;

public class WeaponAudio : MonoBehaviour
{
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip[] shootClips;
    [SerializeField] AudioClip[] impactClips;

    public void PlayShoot()
    {
        PlayRandom(shootClips);
    }

    public void PlayImpact()
    {
        PlayRandom(impactClips);
    }

    void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
