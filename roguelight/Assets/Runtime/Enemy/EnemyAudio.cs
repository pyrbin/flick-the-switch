using JSAM;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "EnemyAudio", menuName = "Scriptable Objects/EnemyAudio")]
public class EnemyAudio : ScriptableObject {
    public SoundFileObject OnReveal;
    public SoundFileObject OnIdle;
    public SoundFileObject OnDeath;
    public SoundFileObject OnHit;

    [Range(0, 1)]
    public float HitSoundChance = 0.1f;

    private void PlaySound(SoundFileObject sound, float chance) {
        if (sound == null) return;
        if (Freya.Random.Range(0f, 1f) > chance) return;
        StopAllSounds();
        AudioManager.StopSound(sound); 
        AudioManager.PlaySound(sound);
    }

    public void PlayDeathSound() => PlaySound(OnDeath, 1f);
    public void PlayHitSound() => PlaySound(OnHit, HitSoundChance);
    public void PlayIdleSound() => PlaySound(OnIdle, 1f);
    public void PlayRevealSound() => PlaySound(OnReveal, 1f);

    private void StopAllSounds() {
        StopSound(OnDeath);
        StopSound(OnHit);
        StopSound(OnIdle);
        StopSound(OnReveal);
    }

    private void StopSound(SoundFileObject sound) {
        if (sound != null) AudioManager.StopSound(sound);
    }
}
