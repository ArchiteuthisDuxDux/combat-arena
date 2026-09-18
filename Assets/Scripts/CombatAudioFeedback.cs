using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CombatAudioFeedback : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip blockClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float hitVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float blockVolume = 1f;

    [Header("Pitch randomization")]
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.05f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        CombatResolver.OnDamageDealt += HandleDamageDealt;
        CombatResolver.OnBlocked += HandleBlocked;
    }

    private void OnDisable()
    {
        CombatResolver.OnDamageDealt -= HandleDamageDealt;
        CombatResolver.OnBlocked -= HandleBlocked;
    }

    private void HandleDamageDealt(FighterAgent attacker, FighterAgent defender, int damage)
    {
        if (attacker == null || attacker.gameObject != gameObject)
            return;

        PlayClip(hitClip, hitVolume);
    }

    private void HandleBlocked(FighterAgent attacker, FighterAgent defender)
    {
        if (attacker == null || attacker.gameObject != gameObject)
            return;

        PlayClip(blockClip, blockVolume);
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, volume);
    }
}