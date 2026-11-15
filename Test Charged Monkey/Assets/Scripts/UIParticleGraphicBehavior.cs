// Draws Particle System particles as masked UI quads, with Canvas scaling, velocity alignment, flicker, and optional edge fading.

using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    /// <summary>
    /// Draws a Particle System as masked UI quads with Canvas scaling, velocity alignment, flicker, and optional vertical fading.
    /// </summary>
    /// <remarks>
    /// Particle positions are expected in local simulation space with the source aligned to this projection.
    /// Horizontal coordinates are centered, so the RectTransform must use a horizontal pivot of 0.5.
    /// Native particle renderer features such as trails, meshes, and texture-sheet animation are not reproduced.
    /// </remarks>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIParticleGraphicBehavior : MaskableGraphic
    {
        #region Public Variables

        /// <summary>
        /// Gets the simulation width mapped to the full UI rectangle width.
        /// </summary>
        /// <value>The reference width shared with emitters that convert UI coordinates back into simulation space.</value>
        public float ReferenceWidth
        {
            get
            {
                return referenceWidth;
            }
        }
        #endregion

        #region Private Variables

        /// <summary>
        /// Local-space simulation to draw; this component does not start or stop its emission.
        /// </summary>
        [Header("Particle Simulation")]

        [SerializeField]
        private ParticleSystem source;

        /// <summary>
        /// Simulation width mapped to this rectangle; changing it scales both particle positions and sizes.
        /// </summary>
        [Header("Canvas Projection")]
        [SerializeField, Min(1)]
        private float referenceWidth = 1000f;

        /// <summary>
        /// Length multiplier applied along each particle movement direction.
        /// </summary>
        [SerializeField, Range(1f, 5f)]
        private float sparkStretch = 1.8f;

        /// <summary>
        /// Whether particle vertical coordinates start at the bottom of the UI rectangle.
        /// </summary>
        [SerializeField]
        private bool useBottomOrigin = true;

        /// <summary>
        /// Whether particles fade between 70 and 95 percent of the UI rectangle height.
        /// </summary>
        [SerializeField]
        private bool fadeAtTop = true;

        /// <summary>
        /// Reused particle buffer that grows to accommodate the source capacity.
        /// </summary>
        private ParticleSystem.Particle[] particles;

        /// <summary>
        /// Whether the last generated mesh contains live-particle quads and may require a final clearing rebuild.
        /// </summary>
        private bool meshHasParticles;

        /// <summary>
        /// Normalized rectangle height at which the optional upper-edge fade begins.
        /// </summary>
        private const float UpperFadeStart = .7f;

        /// <summary>
        /// Normalized rectangle height at which the optional upper-edge fade reaches zero opacity.
        /// </summary>
        private const float UpperFadeEnd = .95f;

        /// <summary>
        /// Baseline opacity multiplier used by the ember flicker.
        /// </summary>
        private const float FlickerBaseOpacity = .78f;

        /// <summary>
        /// Opacity variation around the baseline during the ember flicker.
        /// </summary>
        private const float FlickerAmplitude = .22f;

        /// <summary>
        /// Angular flicker frequency measured over one normalized particle lifetime.
        /// </summary>
        private const float FlickerLifetimeFrequency = 42f;

        /// <summary>
        /// Number of seed-derived phase offsets used to keep particle flicker out of sync.
        /// </summary>
        private const uint FlickerSeedPhases = 100;

        /// <summary>
        /// Squared direction magnitude below which a stationary particle is drawn upright.
        /// </summary>
        private const float MinimumDirectionSqrMagnitude = .01f;

        #endregion

        #region Main Methods

        /// <summary>
        /// Refreshes live particles and clears the last visible mesh once after particles stop or the source is removed.
        /// </summary>
        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (meshHasParticles || (source != null && source.particleCount > 0))
            {
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Builds velocity-aligned UI quads for all live particles, applying projection, tint, flicker, and optional fading.
        /// </summary>
        /// <param name="mesh">UI vertex helper receiving particle vertices and triangles.</param>
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            meshHasParticles = false;

            if (source == null)
            {
                return;
            }

            int capacity = source.main.maxParticles;

            if (particles == null || particles.Length < capacity)
            {
                particles = new ParticleSystem.Particle[capacity];
            }

            int count = source.GetParticles(particles);
            meshHasParticles = count > 0;

            Rect bounds = rectTransform.rect;
            float scale = bounds.width / referenceWidth;

            for (int i = 0; i < count; i++)
            {
                var particle = particles[i];

                Vector2 position = new Vector2(particle.position.x * scale, (useBottomOrigin ? bounds.yMin : 0) + particle.position.y * scale);
                float size = particle.GetCurrentSize(source) * scale;
                Color tint = (Color)particle.GetCurrentColor(source) * color;

                if (fadeAtTop)
                {
                    tint.a *= 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(bounds.height * UpperFadeStart, bounds.height * UpperFadeEnd, position.y - bounds.yMin));
                }

                float age = 1f - particle.remainingLifetime / particle.startLifetime;
                tint.a *= FlickerBaseOpacity + FlickerAmplitude * Mathf.Sin(age * FlickerLifetimeFrequency + particle.randomSeed % FlickerSeedPhases);

                Vector2 up = ((Vector2)particle.totalVelocity).normalized;

                if (up.sqrMagnitude < MinimumDirectionSqrMagnitude)
                {
                    up = Vector2.up;
                }

                Vector2 right = new Vector2(up.y, -up.x) * size * .5f;
                up *= size * sparkStretch * .5f;
                int start = mesh.currentVertCount;

                mesh.AddVert(position - right - up, tint, new Vector2(0, 0));
                mesh.AddVert(position - right + up, tint, new Vector2(0, 1));
                mesh.AddVert(position + right + up, tint, new Vector2(1, 1));
                mesh.AddVert(position + right - up, tint, new Vector2(1, 0));

                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start + 2, start + 3, start);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Assigns the simulated Particle System and requests a UI mesh refresh.
        /// </summary>
        /// <param name="value">Particle System whose live particles will be drawn.</param>
        public void SetSource(ParticleSystem value)
        {
            source = value;
            SetVerticesDirty();
        }

        /// <summary>
        /// Uses the UI rectangle local origin and disables the upper-edge fade for contact bursts.
        /// </summary>
        public void SetCenteredProjection()
        {
            useBottomOrigin = false;
            fadeAtTop = false;
            SetVerticesDirty();
        }

        #endregion
    }
}
