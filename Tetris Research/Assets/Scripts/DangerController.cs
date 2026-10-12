using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class DangerController : MonoBehaviour
{
    public StudioEventEmitter emitter;
    public Slider dangerSlider;

    void Update()
    {
        emitter.SetParameter("Danger", dangerSlider.value);
    }
}