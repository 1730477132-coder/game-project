using System.Collections;
using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    public AudioSource source;
    public AudioClip introBGM, normalBGM;

    void Awake() { if (!source) source = GetComponent<AudioSource>(); source.playOnAwake = false; }
    IEnumerator Start()
    {
        if (introBGM)
        {
            source.loop = false; source.clip = introBGM; source.Play();
            yield return new WaitForSeconds(Mathf.Min(introBGM.length, 3f));
        }
        if (normalBGM) { source.loop = true; source.clip = normalBGM; source.Play(); }
    }
}
