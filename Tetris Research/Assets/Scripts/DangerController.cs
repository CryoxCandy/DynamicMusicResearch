using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class DangerController : MonoBehaviour
{
    public StudioEventEmitter emitter;   // drag MusicManager here
    public Slider dangerSlider;          // drag your Slider here

    void Update()
    {
        emitter.SetParameter("Danger", dangerSlider.value);
    }
}