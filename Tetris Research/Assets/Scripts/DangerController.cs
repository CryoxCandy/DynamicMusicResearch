using System.Collections;
using FMODUnity;
using UnityEngine;

public class DangerController : MonoBehaviour
{
    public StudioEventEmitter emitter;
    [SerializeField] TetrisGame game;
    [SerializeField] string parameterName = "Danger";

    [Header("Adaptive Mode Settings")]
    [SerializeField] bool adaptive = true;
    [SerializeField, Range(0f, 1f)] float staticDanger = 0.5f;

    [Header("Game over")]
    [SerializeField, Min(0f)] float fadeOutSeconds = 1.5f;
    public float SentDanger { get; private set; }
    Coroutine fade;
    void Awake()
    {
        if (emitter == null) emitter = GetComponent<StudioEventEmitter>();
        if (game == null) game = FindAnyObjectByType<TetrisGame>();
    }

    void OnEnable()
    {
        if (game == null) return;
        game.GameStarted += OnGameStarted;
        game.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        if (game == null) return;
        game.GameStarted -= OnGameStarted;
        game.StateChanged -= OnStateChanged;
    }

    void Update()
    {
        if (game != null && emitter != null && game.State == TetrisState.Playing)
            SendDanger(false);
    }

    void SendDanger(bool instant)
    {
        SentDanger = adaptive ? game.Danger : staticDanger;
        emitter.EventInstance.setParameterByName(parameterName, SentDanger, instant);
    }

    void OnGameStarted()
    {
        if (emitter == null) return;
        if (fade != null)
        {
            StopCoroutine(fade);
            fade = null;
        }

        emitter.Play();
        emitter.EventInstance.setVolume(1f);
        emitter.EventInstance.setPaused(false);
        SendDanger(true);
    }

    void OnStateChanged(TetrisState state)
    {
        if (emitter == null) return;
        switch (state)
        {
            case TetrisState.Paused:
                emitter.EventInstance.setPaused(true);
                break;

            case TetrisState.Playing:
                emitter.EventInstance.setPaused(false);
                break;

            case TetrisState.GameOver:
                if (fade != null) StopCoroutine(fade);
                fade = StartCoroutine(FadeOutAndStop());
                break;
        }
    }

    IEnumerator FadeOutAndStop()
    {
        for (float t = 0f; t < fadeOutSeconds; t += Time.unscaledDeltaTime)
        {
            emitter.EventInstance.setVolume(1f - t / fadeOutSeconds);
            yield return null;
        }
        emitter.Stop();
        fade = null;
    }
}