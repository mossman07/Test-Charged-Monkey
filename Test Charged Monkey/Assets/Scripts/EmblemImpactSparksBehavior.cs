// Emits a short radial particle burst from the emblem contact rim and converts its positions into Canvas projection space.

using UnityEngine;

namespace Scripts
{
    /// <summary>
    /// Emits short radial spark bursts along the emblem contact rim and converts their positions into UI particle coordinates.
    /// </summary>
    /// <remarks>
    /// Contact points approximate the shield rim with an ellipse rather than following the sprite outline.
    /// When the projection has a UIParticleGraphicBehavior, its reference width is used for both coordinate conversions.
    /// The source must use local simulation space and remain aligned with the projection.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class EmblemImpactSparksBehavior : MonoBehaviour
    {
        #region Public Variables

        /// <summary>
        /// Gets the number of valid contact bursts emitted, including bursts whose particles have already expired.
        /// </summary>
        /// <value>Number of contact bursts emitted by this component instance.</value>
        public int ImpactCount
        {
            get;
            private set;
        }

        #endregion

        #region Private Variables

        /// <summary>
        /// Local-space Particle System receiving explicitly positioned contact sparks.
        /// </summary>
        [SerializeField]
        private ParticleSystem source;

        /// <summary>
        /// UI RectTransform used to convert contact positions into particle coordinates.
        /// </summary>
        [SerializeField]
        private RectTransform projection;

        /// <summary>
        /// Fallback simulation width for a projection without a UIParticleGraphicBehavior.
        /// </summary>
        /// <remarks>The projection renderer reference width takes precedence when present.</remarks>
        [SerializeField, Min(1)]
        private float referenceWidth = 1000;

        /// <summary>
        /// Number of particles emitted by each contact burst.
        /// </summary>
        [SerializeField, Range(1, 100)]
        private int sparkCount = 36;

        /// <summary>
        /// Fraction of the rectangle size used for each elliptical contact radius; 0.5 reaches the rectangle edges.
        /// </summary>
        [SerializeField, Range(0, .5f)]
        private float contactRadiusRatio = .42f;

        /// <summary>
        /// Upward direction bias added to the outward impulse before applying speed.
        /// </summary>
        [SerializeField]
        private float upwardBias = .35f;

        /// <summary>
        /// Random contact speed range in UI units per second, before the directional bias.
        /// </summary>
        [SerializeField]
        private Vector2 speedRange = new Vector2(110f, 290f);

        /// <summary>
        /// Random initial particle width range in UI units.
        /// </summary>
        [SerializeField]
        private Vector2 sizeRange = new Vector2(12f, 27f);

        /// <summary>
        /// Random particle lifetime range in seconds.
        /// </summary>
        [SerializeField]
        private Vector2 lifetimeRange = new Vector2(.35f, .8f);

        /// <summary>
        /// Orange endpoint of the random start-color blend.
        /// </summary>
        [SerializeField]
        private Color warmColor = new Color(1, .22f, .02f);

        /// <summary>
        /// Hot yellow endpoint of the random start-color blend.
        /// </summary>
        [SerializeField]
        private Color hotColor = new Color(1, .8f, .26f);

        /// <summary>
        /// Cached renderer on the projection, used to share its simulation-to-UI scale.
        /// </summary>
        private UIParticleGraphicBehavior particleGraphic;

        /// <summary>
        /// Minimum emission conversion scale retained for zero-width or collapsed projections.
        /// </summary>
        private const float MinimumProjectionScale = .001f;

        #endregion

        #region Main Methods

        /// <summary>
        /// Stops the source and clears remaining sparks when this component is disabled.
        /// </summary>
        private void OnDisable()
        {
            Clear();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Assigns the particle source and UI projection used by contact bursts.
        /// </summary>
        /// <param name="value">Particle System receiving emitted contact sparks.</param>
        /// <param name="canvasProjection">UI rectangle defining the particle projection scale and origin.</param>
        public void Configure(ParticleSystem value, RectTransform canvasProjection)
        {
            source = value;
            projection = canvasProjection;
        }

        /// <summary>
        /// Emits outward-moving sparks from the emblem rim in UI projection coordinates.
        /// </summary>
        /// <param name="emblem">Emblem RectTransform whose rim defines the spark contact positions.</param>
        public void EmitImpact(RectTransform emblem)
        {
            if (source == null || projection == null || emblem == null)
            {
                return;
            }

            ImpactCount++;

            source.Play();

            float scale = GetProjectionScale();
            Rect bounds = emblem.rect;

            for (int i = 0; i < sparkCount; i++)
            {
                float angle = (i + Random.value) * Mathf.PI * 2 / sparkCount;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var rim = bounds.center + Vector2.Scale(outward, bounds.size * contactRadiusRatio);
                Vector3 local = projection.InverseTransformPoint(emblem.TransformPoint(rim));
                Vector3 direction = projection.InverseTransformVector(emblem.TransformVector(outward));
                direction.z = 0;

                var particle = new ParticleSystem.EmitParams
                {
                    position = local / scale,
                    velocity = (direction.normalized + Vector3.up * upwardBias) * (Random.Range(speedRange.x, speedRange.y) / scale),
                    startSize = Random.Range(sizeRange.x, sizeRange.y) / scale,
                    startLifetime = Random.Range(lifetimeRange.x, lifetimeRange.y),
                    startColor = Color.Lerp(warmColor, hotColor, Random.value)
                };

                source.Emit(particle, 1);
            }
        }

        /// <summary>
        /// Stops emission and removes every live contact particle.
        /// </summary>
        public void Clear()
        {
            if (source != null)
            {
                source.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// Resolves the projection renderer scale so emission remains aligned when its reference width changes.
        /// </summary>
        /// <returns>UI units per simulation unit, clamped to the minimum emission scale.</returns>
        private float GetProjectionScale()
        {
            if (particleGraphic == null || particleGraphic.rectTransform != projection)
            {
                particleGraphic = projection.GetComponent<UIParticleGraphicBehavior>();
            }

            float width = particleGraphic != null ? particleGraphic.ReferenceWidth : referenceWidth;

            return Mathf.Max(MinimumProjectionScale, projection.rect.width / width);
        }

        #endregion
    }
}
