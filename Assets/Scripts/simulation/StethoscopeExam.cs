using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class StethoscopeExam : MonoBehaviour
{
    private AudioSource heartbeatAudio;

    void Start()
    {
        heartbeatAudio = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HeartZone"))
        {
            if (!heartbeatAudio.isPlaying)
            {
                heartbeatAudio.Play();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HeartZone"))
        {
            heartbeatAudio.Pause();
        }
    }
}