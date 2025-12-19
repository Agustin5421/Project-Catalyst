using UnityEngine;
using System.Collections;

public class AudioFader : MonoBehaviour {
    public void StartFade(float duration) {
        StartCoroutine(FadeOut(duration));
    }
    
    private IEnumerator FadeOut(float duration) {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null) yield break;
        
        float startVol = audio.volume;
        float rate = 1.0f / duration;
        float progress = 0.0f;
        
        while (progress < 1.0f) {
            progress += Time.deltaTime * rate;
            audio.volume = Mathf.Lerp(startVol, 0f, progress);
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
