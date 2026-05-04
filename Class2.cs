using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;
using System.Linq;

[BepInPlugin("com.supernova.audiorework", "Audio Rework", "1.0.0")]
public class AudioRework : BaseUnityPlugin
{
    public static ConfigEntry<int> rayCount;
    public static ConfigEntry<float> minVolume;
    public static ConfigEntry<float> maxCutoff;
    public static ConfigEntry<float> minCutoff;
    public static ConfigEntry<float> smoothingSpeed;
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("REPOAudioFixer_Class2");

    void Awake()
    {
        Logger.LogInfo("Audio Rework Loaded");

        // Config
        rayCount = Config.Bind("General", "Ray Count", 7, "Number of rays used for occlusion");
        minVolume = Config.Bind("General", "Min Volume", 0.25f, "Lowest volume when fully occluded");
        maxCutoff = Config.Bind("General", "Max Cutoff", 22000f, "Clear sound cutoff frequency");
        minCutoff = Config.Bind("General", "Min Cutoff", 1200f, "Muffled sound cutoff frequency");
        smoothingSpeed = Config.Bind("General", "Smoothing Speed", 5f, "How fast audio transitions");

        // Harmony setup
        var harmony = new Harmony("com.supernova.audiorework");
        harmony.PatchAll();

        Logger.LogInfo("Harmony patches applied");
        Logger.LogInfo("Patches: " + harmony.GetPatchedMethods().Count());
    }

    // 🔊 Hook: AudioSource.Play
    [HarmonyPatch(typeof(AudioSource))]
    class Patch_Play
    {
        [HarmonyPatch("Play", new System.Type[] { })] // explicitly no parameters
        [HarmonyPostfix]
        static void Postfix(AudioSource __instance)
        {
            AudioRework.TryAttach(__instance);
        }
    }

    // 🔊 Hook: AudioSource.PlayOneShot
    [HarmonyPatch(typeof(AudioSource))]
    class Patch_PlayOneShot
    {
        [HarmonyPatch("PlayOneShot", new System.Type[] { typeof(AudioClip) })]
        [HarmonyPostfix]
        static void Postfix(AudioSource __instance)
        {
            AudioRework.TryAttach(__instance);
        }
    }

    static void TryAttach(AudioSource source)
    {
        if (source == null)
            return;
        Logger.LogInfo("Attached to: " + source.name);

        // Ignore 2D audio
        if (source.spatialBlend < 0.1f)
            return;

        if (source.GetComponent<AudioReworkTag>() == null)
        {
            source.gameObject.AddComponent<AudioReworkTag>();
        }
    }
}

// 🎧 Per-audio behavior
public class AudioReworkTag : MonoBehaviour
{
    private AudioSource source;
    private AudioLowPassFilter filter;
    private Camera playerCam;

    void Awake()
    {
        source = GetComponent<AudioSource>();

        filter = GetComponent<AudioLowPassFilter>();
        if (filter == null)
            filter = gameObject.AddComponent<AudioLowPassFilter>();
    }

    void Update()
    {
        if (source == null || !source.isPlaying)
            return;

        if (playerCam == null)
            playerCam = Camera.main;

        if (playerCam == null)
            return;

        Debug.Log("Occlusion running on: " + source.name);
        ApplyOcclusion();
    }

    void ApplyOcclusion()
    {
        Vector3 origin = playerCam.transform.position;
        Vector3 target = transform.position;
        Vector3 dir = target - origin;
        float distance = dir.magnitude;

        if (distance <= 0.1f)
            return;

        int rays = AudioRework.rayCount.Value;
        int hits = 0;

        for (int i = 0; i < rays; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.3f;
            Vector3 rayDir = (dir + offset).normalized;

            if (Physics.Raycast(origin, rayDir, distance))
                hits++;
        }

        float occlusion = (float)hits / rays;

        // 🎯 Directional factor
        Vector3 forward = playerCam.transform.forward;
        Vector3 toSource = dir.normalized;
        float directionFactor = Vector3.Dot(forward, toSource);
        directionFactor = Mathf.Clamp01((directionFactor + 1f) * 0.5f);

        float finalOcclusion = Mathf.Lerp(occlusion, 1f, 1f - directionFactor);

        // 🔊 Volume
        float targetVolume = Mathf.Lerp(1f, AudioRework.minVolume.Value, finalOcclusion);
        source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * AudioRework.smoothingSpeed.Value);

        // 🔉 Muffle
        float targetCutoff = Mathf.Lerp(AudioRework.maxCutoff.Value, AudioRework.minCutoff.Value, finalOcclusion);
        filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, targetCutoff, Time.deltaTime * AudioRework.smoothingSpeed.Value);
    }
}