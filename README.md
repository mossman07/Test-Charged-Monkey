# Charged Monkey - Unity Senior Technical Art Test

A Unity technical art test built around a golden crown and interchangeable faction emblems. The effect combines a shader-driven reveal and dissolve, a striking emblem animation, UI particles, and synchronized audio.

The test was completed over the weekend of Saturday, November 14, and Sunday, November 15, 2026.

## Visual sequence

- **Appearance:** the crown reveals through a noisy mask while the emblem approaches from above and strikes into place. A brief rebound, crown shake, radial sparks, and metallic impact sound emphasize contact.
- **Idle:** a diagonal highlight periodically sweeps across the gold, accompanied by a chime. Background embers rise from the bottom of the screen.
- **Disappearance:** the emblem retreats, rotates, and fades while the crown dissolves with an illuminated edge. A whoosh fades out with the transition.
- **Emblem change:** the current crown and emblem disappear before the next emblem is selected and the appearance sequence restarts.

## Run the demo

1. Open `Test Charged Monkey` through Unity Hub using **Unity 6000.6.8f1**.
2. Allow Unity to restore the packages and import the assets. Git must be available to resolve the LitMotion dependency.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Enter Play Mode. The appearance sequence starts automatically.

| Control | Action |
| --- | --- |
| **Replay** | Cancels the current sequence and restarts the appearance animation. |
| **Change emblem** | Plays disappearance, selects the next of three emblems, and plays appearance. Additional change requests are ignored until the sequence finishes. |

The scene is configured using standard Unity components and serialized references. No custom editor tools are required.

## Implementation

### Animation and audio

`EmblemObjectController` coordinates the crown material, emblem transform and opacity, impact sparks, and audio through LitMotion timelines. It supports appearance, disappearance, replay, and periodic highlights, with cancellation when the component is disabled or a sequence is interrupted.

The controller creates its own crown material instance so separate emblem objects can animate independently. Animation uses unscaled time by default.

Each sound has a dedicated `AudioSource`. Clips and playback volumes are configured on those sources; the controller triggers playback at the relevant animation events and applies the disappearance and shimmer volume envelopes.

### UI particles

`UIParticleGraphicBehavior` draws particles from a standard Unity `ParticleSystem` as quads through `MaskableGraphic`. This places the particles inside the Canvas rendering order and supports UI masking. It reuses a particle buffer and applies velocity alignment, stretching, flicker, and optional fading near the top of the screen.

The same renderer serves both background embers and contact sparks. `EmblemImpactSparksBehavior` emits the impact burst around an elliptical approximation of the emblem rim and converts the emission coordinates into the UI renderer's projection space.

### Shaders

Both custom shaders extend Unity 6000.6.8f1's built-in `UI/Default`, retaining its UI rendering structure and adding the effect-specific properties and calculations.

- **Corona:** combines the painted gold texture, packed effect masks, and erosion noise to produce reveal, dissolve edges, shimmer, and halo.
- **Embers:** creates a hot core and soft outer glow procedurally from particle UVs, with adjustable intensity and additive blending.

## Texture sources

No visual assets were supplied for the demo, so the textures were created or sourced as follows:

- **`corona.png`:** the crown image was created with AI assistance.
- **`corona_VFX.png`:** the crown effect masks were created in Photoshop.
- **`emblem_1`, `emblem_2`, and `emblem_3`:** images sourced from the internet depicting heraldic shields from a Warhammer faction.
- **`noise_fractal.png`:** generated using a custom noise-generation tool I developed previously.

## Dependencies and third-party assets

- **Unity Editor:** 6000.6.8f1.
- **Universal Render Pipeline:** 17.6.0.
- **Unity UI (uGUI):** 2.6.0.
- **[LitMotion](https://github.com/annulusgames/LitMotion):** 2.0.2, referenced through the package manifest.
- **Unity built-in shader source:** the original Unity copyright notice and MIT license are retained in the adapted shaders.
