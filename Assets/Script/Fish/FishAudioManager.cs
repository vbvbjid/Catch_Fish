using UnityEngine;

public class FishAudioManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip success;
    public AudioClip struggle;
    public AudioClip flee;
    public AudioClip swim;

    [Header("Volume Settings")]
    public float swimVolume = 0.2f;
    public float struggleVolume = 2.0f;
    public float fleeVolume = 10.0f;

    private AudioClip currentClip;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Start with swim sound
        PlaySwimSound();
    }

    public void PlaySuccessSound()
    {
        if (success != null)
        {
            AudioSource.PlayClipAtPoint(success, transform.position);
        }
    }

    public void PlayStruggleSound()
    {
        if (struggle != null && audioSource != null)
        {
            if (currentClip != struggle)
            {
                audioSource.clip = struggle;
                audioSource.volume = struggleVolume;
                audioSource.Play();
                currentClip = struggle;
            }
        }
    }

    public void PlayFleeSound()
    {
        if (flee != null && audioSource != null)
        {
            audioSource.PlayOneShot(flee, fleeVolume);
        }
    }

    public void PlaySwimSound()
    {
        if (swim != null && audioSource != null)
        {
            if (currentClip != swim)
            {
                audioSource.clip = swim;
                audioSource.volume = swimVolume;
                audioSource.Play();
                currentClip = swim;
            }
        }
    }

    public void StopAllSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            currentClip = null;
        }
    }

    public void SwitchFromStruggleToSwim()
    {
        if (currentClip == struggle)
        {
            PlaySwimSound();
        }
    }
}