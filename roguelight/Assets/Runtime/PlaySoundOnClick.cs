using JSAM;
using UnityEngine;

public class PlaySoundOnClick : MonoBehaviour
{
    public SoundFileObject soundFile;
    public Clickable clickable; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!clickable) clickable = GetComponent<Clickable>();
        clickable.OnClick += PlaySound;
    }

    // Update is called once per frame
    void PlaySound(Transform transform)
    {
        AudioManager.StopSound(soundFile);
        AudioManager.PlaySound(soundFile);
    }

    void OnDisable() {
        AudioManager.StopSound(soundFile);
    }
}
