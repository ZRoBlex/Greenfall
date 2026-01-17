using UnityEngine;

public class WeaponAudio : MonoBehaviour
{
    [SerializeField] AudioSource source;

    [Header("Sounds")]
    public AudioClip shoot;
    public AudioClip impact;
    public AudioClip critical;

    public void PlayShoot()
    {
        if (source && shoot)
            source.PlayOneShot(shoot);
    }

    public void PlayImpact(bool crit)
    {
        if (!source) return;

        if (crit && critical)
            source.PlayOneShot(critical);
        else if (impact)
            source.PlayOneShot(impact);
    }
}
