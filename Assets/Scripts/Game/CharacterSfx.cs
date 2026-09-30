using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CharacterSfx : MonoBehaviour
{
    [SerializeField] private AudioClip swingClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip parryClip;
    [SerializeField] private AudioClip rollClip;
    [SerializeField] private AudioClip chargePrepareClip;
    [SerializeField] private AudioClip specialImpactClip;
    [SerializeField] private AudioClip[] footstepClips;

    private AudioSource audioSource;


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning($"{name}: 효과음 슬롯이 비어 있음", this);
            return;
        }

        if (!isActiveAndEnabled)
            return;

        Debug.Log($"{name}: 효과음 재생 요청 - {clip.name}", this);
        audioSource.PlayOneShot(clip);
    }

    public void PlaySwing()
    {
        PlayClip(swingClip);
    }

    public void PlayHit()
    {
        PlayClip(hitClip);
    }

    public void PlayParry()
    {
        PlayClip(parryClip);
    }

    public void PlayRoll()
    {
        PlayClip(rollClip);
    }

    public void PlayChargePrepare()
    {
        PlayClip(chargePrepareClip);
    }

    public void PlaySpecialImpact()
    {
        PlayClip(specialImpactClip);
    }

    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return;

        int index = Random.Range(0, footstepClips.Length);
        PlayClip(footstepClips[index]);
    }
}