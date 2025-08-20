using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayRandomSoundOnDestroy : MonoBehaviour
{
    public AudioClip[] clips;
    [Range(0f,1f)] public float volume = 1f;

    private void OnDestroy()
    {
        
        if (!Application.isPlaying) return;

        if (clips != null && clips.Length > 0)
        {
            var clip = clips[Random.Range(0, clips.Length)];
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
    }
}
