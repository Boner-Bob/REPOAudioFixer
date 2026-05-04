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
    public static ConfigEntry<float> directionStrength;
    public static ConfigEntry<float> diffractionSoftness;
    public static ConfigEntry<float> earResponsiveness;
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
        directionStrength = Config.Bind("General", "Direction Strength", 1.8f, "1.0 = weak\r\n2.0 = stronger muffling when sound is behind player");
        diffractionSoftness = Config.Bind("General", "Diffraction Softness", 0.2f, "Higher = sound wraps more around corners");
        earResponsiveness = Config.Bind("General", "Ear Responsiveness", 5f, "Higher = snappier left/right movement");

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

        float materialOcclusion = 0f;

        for (int i = 0; i < rays; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.3f;
            Vector3 rayDir = (dir + offset).normalized;

            if (Physics.Raycast(origin, rayDir, out RaycastHit hit, distance))
            {
                hits++;

                // 🧱 MATERIAL CHECK
                string mat = hit.collider.tag;

                switch (mat)
                {
                    case "Glass":
                        materialOcclusion += 0.2f;
                        break;
                    case "Wood":
                        materialOcclusion += 0.5f;
                        break;
                    case "Metal":
                        materialOcclusion += 0.8f;
                        break;
                    default:
                        materialOcclusion += 1.0f;
                        break;
                }
            }
        }

        float occlusion = (float)hits / rays;

        // Average material influence
        if (hits > 0)
            occlusion *= (materialOcclusion / hits);

        // 🌀 DIFFRACTION (soften harsh blocking)
        float diffraction = Mathf.Lerp(AudioRework.diffractionSoftness.Value, 0f, occlusion); //Change first number higher for more wrap around corners
        occlusion = Mathf.Clamp01(occlusion - diffraction);

        // 🎯 DIRECTION (smoothed)
        Vector3 forward = playerCam.transform.forward;
        Vector3 toSource = dir.normalized;

        float directionFactor = Vector3.Dot(forward, toSource);
        directionFactor = Mathf.Clamp01((directionFactor + 1f) * 0.5f);
        directionFactor = Mathf.Pow(directionFactor, AudioRework.directionStrength.Value); // 1.0 = weak, 2.0 = stronger muffling when sound is behind player

        float directionalOcclusion = Mathf.Lerp(occlusion, 1f, 1f - directionFactor);

        // 👂 EAR SIMULATION (stereo pan)
        Vector3 right = playerCam.transform.right;
        float side = Vector3.Dot(right, toSource);

        source.panStereo = Mathf.Lerp(source.panStereo, side, Time.deltaTime * AudioRework.earResponsiveness.Value);

        float finalOcclusion = directionalOcclusion;

        // 🔊 VOLUME
        float targetVolume = Mathf.Lerp(1f, AudioRework.minVolume.Value, finalOcclusion);
        source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * AudioRework.smoothingSpeed.Value);

        // 🔉 LOWPASS
        float targetCutoff = Mathf.Lerp(AudioRework.maxCutoff.Value, AudioRework.minCutoff.Value, finalOcclusion);
        filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, targetCutoff, Time.deltaTime * AudioRework.smoothingSpeed.Value);
    }
}