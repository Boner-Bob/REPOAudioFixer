# Audio Rework

Audio Rework is a lightweight audio enhancement mod that improves how 3D sounds behave in-game. It adds occlusion (sounds being blocked by objects), directional awareness, and smoother spatial audio transitions using runtime hooks.

---

## Features

* **Occlusion**

  * Sounds are reduced in volume and clarity when blocked by objects between the player and the source.
  * Uses multiple raycasts to estimate how obstructed a sound is.

* **Directional Audio**

  * Sounds behind the player are more muffled than those in front.
  * Uses the camera’s forward direction to determine how sound should be affected.

* **Diffraction (Corner Behavior)**

  * Prevents sounds from being completely cut off when slightly obstructed.
  * Allows partial “wrapping” of sound around edges.

* **Stereo Panning**

  * Adjusts left/right balance based on where the sound is relative to the player.
  * Helps with spatial awareness.

* **Low-Pass Filtering**

  * Applies a filter to reduce high frequencies when sounds are obstructed, creating a muffled effect.

---

## How It Works

* Hooks into audio playback using Harmony.
* When a 3D `AudioSource` plays, the mod attaches a handler component.
* Each active sound:

  1. Casts rays from the player to the sound source.
  2. Calculates how many rays are blocked.
  3. Adjusts volume and filter cutoff based on obstruction.
  4. Applies directional and stereo effects.

---

## Configuration

All settings are configurable via the generated config file.

### General

#### `Ray Count` (int, default: 7)

Number of rays cast between the player and the sound source.

* Higher values:

  * More accurate occlusion
  * Higher performance cost
* Lower values:

  * Less accurate
  * Better performance

---

#### `Min Volume` (float, default: 0.25)

The lowest volume a sound can reach when fully occluded.

* `1.0` = no volume reduction
* `0.0` = completely silent when blocked

---

#### `Max Cutoff` (float, default: 22000)

Maximum low-pass filter frequency (clear sound).

* Higher values = less filtering
* Typically should remain at default

---

#### `Min Cutoff` (float, default: 1200)

Minimum low-pass filter frequency (muffled sound).

* Lower values = more muffled audio
* Higher values = less noticeable occlusion

---

#### `Smoothing Speed` (float, default: 5)

Controls how quickly audio transitions between states.

* Higher values:

  * Faster response
  * More abrupt changes
* Lower values:

  * Smoother transitions
  * Slight delay in response

---

#### `Direction Strength` (float, default: 1.8)

Controls how strongly sound is affected based on whether it is in front of or behind the player.

* `1.0` = minimal directional effect
* Higher values = stronger muffling behind the player

---

#### `Diffraction Softness` (float, default: 0.2)

Controls how much sound “wraps” around obstacles.

* Higher values:

  * Less harsh cutoffs
  * More audible around corners
* Lower values:

  * Sharper occlusion
  * More realistic blocking

---

#### `Ear Responsiveness` (float, default: 5)

Controls how quickly stereo panning updates.

* Higher values:

  * Faster left/right changes
* Lower values:

  * Smoother but slower response

---

## Notes

* Only affects **3D audio sources** (UI and 2D sounds are ignored).
* Uses Unity object tags (`Glass`, `Wood`, `Metal`) to adjust occlusion strength.

  * If tags are not present, a default value is used.
* Designed to run efficiently by attaching only to active audio sources.

---

## Compatibility

* Client-side only
* Does not modify game files directly
* Should work with most mods unless they heavily modify audio systems

---

## Known Limitations

* Material-based occlusion depends on object tags being set correctly.
* Some audio sources may not be affected if they use non-standard playback methods.
* Performance may vary depending on the number of active sound sources and ray count.

---

## License

This project is provided as-is for personal use and modification.
