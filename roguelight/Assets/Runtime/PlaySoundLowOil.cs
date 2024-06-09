using JSAM;
using UnityEngine;

[RequireComponent(typeof(Oil))]
public class PlaySoundLowOil : MonoBehaviour
{
    public Oil oil;

    private bool lastValue = false;

    void Start()
    {
        TryGetComponent(out oil);
        oil.OnCurrentChanged += PlaySound;
    }

    void PlaySound(float value) {
        if (oil.IsLow && lastValue == false) AudioManager.PlaySound(Audio.Sounds.WarningOil);
        lastValue = oil.IsLow;
    }

}
