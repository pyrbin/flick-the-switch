using JSAM;
using UnityEngine;
using Utilities.Extensions;

[RequireComponent(typeof(Clickable), typeof(Health))]
public class ScaredGuyBehaviour : MonoBehaviour
{
    public Transform AngryModel;

    public Transform NormalModel;

    [ReadOnly]
    public Clickable? Clickable;

    [ReadOnly]
    public Health? Health;

    public ParticleSystem OnDeflectParticles;

    public ParticleSystem WhenAngryParticles;

    public void Awake()
    {
        TryGetComponent(out Clickable);
        TryGetComponent(out Health);
        Clickable!.OnClick += OnClick;
    }

    bool swapped = false;
    bool isAngry = false;
    const float angryThreshold = 0.5f;
    const float angryDuration = 3f;
    public void OnClick(Transform _cursor)
    {
        if (isAngry)
        {
            Player.Instance.ReduceOil(Player.Instance.Oil.AmountFromPercentage(0.1f));
            OnDeflectParticles.Play();
        }

        if (!swapped && Health!.Percentage() <= angryThreshold)
        {
            Angry();
        }
    }

    public void Angry()
    {
        if (swapped) return;

        swapped = true;
        NormalModel.SetActive(false);
        AngryModel.SetActive(true);

        TweenTools.Shake2(AngryModel.transform, angryDuration, 1.2f);
        isAngry = true;

        UniTask.Void(async () => {
            WhenAngryParticles.Play();
            await UniTask.Delay(TimeSpan.FromSeconds(angryDuration), ignoreTimeScale: false);
            isAngry = false;
            WhenAngryParticles?.Stop();
            NormalModel?.SetActive(true);
            AngryModel?.SetActive(false);
        });
    }
}
