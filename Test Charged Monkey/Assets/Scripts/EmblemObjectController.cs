// Coordinates emblem strike and retreat, crown dissolve, impact sparks, periodic shimmer, and synchronized audio with LitMotion.

using UnityEngine;
using UnityEngine.UI;
using LitMotion;

namespace Scripts
{

    /// <summary>
    /// Coordinates crown reveal and dissolve, emblem strike and retreat, particle contacts, periodic reflection, and synchronized audio using LitMotion.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class EmblemObjectController : MonoBehaviour
    {
        #region Public Variables

        /// <summary>
        /// Visibility phases used by the crown and emblem animation timeline.
        /// </summary>
        public enum VisibilityState
        {
            /// <summary>
            /// Both visuals are hidden.
            /// </summary>
            Hidden,

            /// <summary>
            /// The crown is revealing and the emblem is striking into place.
            /// </summary>
            Appearing,

            /// <summary>
            /// Both visuals are fully visible and reflection sweeps may run.
            /// </summary>
            Visible,

            /// <summary>
            /// The emblem is retreating while the crown dissolves.
            /// </summary>
            Disappearing
        }

        /// <summary>
        /// Gets the current visibility phase of the crown and emblem.
        /// </summary>
        /// <value>Current visibility phase of the crown and emblem.</value>
        public VisibilityState State
        {
            get;
            private set;
        } = VisibilityState.Hidden;

        /// <summary>
        /// Gets the current normalized crown reveal value.
        /// </summary>
        /// <value>Current normalized crown reveal value.</value>
        public float Reveal
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the index of the currently selected emblem sprite.
        /// </summary>
        /// <value>Index of the currently selected emblem sprite.</value>
        public int CurrentEmblemIndex
        {
            get
            {
                return currentEmblemIndex;
            }
        }

        /// <summary>
        /// Gets whether an emblem change is currently hiding, swapping, or appearing.
        /// </summary>
        /// <value>Whether an emblem change is currently hiding, swapping, or appearing.</value>
        public bool IsChangingEmblem
        {
            get
            {
                return changingEmblem;
            }
        }

        /// <summary>
        /// Gets the emitter used for the contact spark burst.
        /// </summary>
        /// <value>Emitter used for the contact spark burst.</value>
        public EmblemImpactSparksBehavior ImpactSparks
        {
            get
            {
                return impactSparks;
            }
        }

        /// <summary>
        /// Gets the audio source assigned to metallic contact playback.
        /// </summary>
        /// <value>Audio source assigned to metallic contact playback.</value>
        public AudioSource ImpactAudioSource
        {
            get
            {
                return impactAudioSource;
            }
        }

        /// <summary>
        /// Gets the audio source assigned to the disappearance envelope.
        /// </summary>
        /// <value>Audio source assigned to the disappearance envelope.</value>
        public AudioSource DisappearAudioSource
        {
            get
            {
                return disappearAudioSource;
            }
        }

        /// <summary>
        /// Gets the audio source assigned to the reflection chime.
        /// </summary>
        /// <value>Audio source assigned to the reflection chime.</value>
        public AudioSource ShimmerAudioSource
        {
            get
            {
                return shimmerAudioSource;
            }
        }

        #endregion

        #region Private Variables

        /// <summary>
        /// Crown Image whose material receives reveal and reflection values.
        /// </summary>
        [Header("Visual References")]
        [SerializeField]
        private Image coronaImage;

        /// <summary>
        /// Foreground emblem Image animated during the strike and retreat.
        /// </summary>
        [SerializeField]
        private Image emblemImage;

        [SerializeField] Sprite[]
        emblems = new Sprite[0];
        /// <summary>
        /// Index of the currently selected emblem sprite.
        /// </summary>
        [SerializeField, Min(0)]
        int currentEmblemIndex;

        /// <summary>
        /// Whether enabling this controller starts the appearance animation.
        /// </summary>
        [Header("Startup and Timing")]
        [SerializeField]
        private bool showOnEnable = true;

        /// <summary>
        /// Whether animation and audio envelopes ignore the game time scale.
        /// </summary>
        [SerializeField]
        private bool useUnscaledTime = true;

        /// <summary>
        /// Duration of the complete crown reveal and emblem strike, in seconds.
        /// </summary>
        [Header("Reveal and Dissolve")]
        [SerializeField, Min(0)]
        private float appearDuration = 0.8f;

        /// <summary>
        /// Duration of the emblem retreat and crown dissolve, in seconds.
        /// </summary>
        [SerializeField, Min(0)]
        private float disappearDuration = 0.6f;

        /// <summary>
        /// Easing applied to the crown reveal during appearance.
        /// </summary>
        [SerializeField]
        private Ease appearEase = Ease.OutCubic;

        /// <summary>
        /// Easing applied to the crown reveal during disappearance.
        /// </summary>
        [SerializeField]
        private Ease disappearEase = Ease.InCubic;

        /// <summary>
        /// Emitter that creates the radial spark burst at contact.
        /// </summary>
        [Header("Emblem Impact")]
        [SerializeField]
        private EmblemImpactSparksBehavior impactSparks;

        /// <summary>
        /// Multiplier applied to the authored emblem scale before the strike.
        /// </summary>
        [SerializeField, Min(1)]
        private float entryScale = 1.8f;

        /// <summary>
        /// Upward displacement from the authored emblem position before the strike, in UI units.
        /// </summary>
        [SerializeField, Min(0)]
        private float entryOffset = 85f;

        /// <summary>
        /// Normalized appearance time at which the emblem contacts the crown.
        /// </summary>
        [SerializeField, Range(.3f, .85f)]
        private float impactTime = .65f;

        /// <summary>
        /// Maximum crown shake amplitude after contact, in UI units.
        /// </summary>
        [SerializeField, Min(0)]
        private float impactShake = 6f;

        /// <summary>
        /// Upward displacement used by the emblem retreat, in UI units.
        /// </summary>
        [SerializeField, Min(0)]
        private float exitOffset = 55f;

        /// <summary>
        /// Dedicated 2D source used for the metallic contact sound.
        /// </summary>
        [Header("Impact Audio")]
        [SerializeField]
        private AudioSource impactAudioSource;

        /// <summary>
        /// Dedicated 2D source used for the retreat and dissolve sound.
        /// </summary>
        [Header("Disappearance Audio")]
        [SerializeField]
        private AudioSource disappearAudioSource;

        /// <summary>
        /// Dedicated 2D source used for the periodic reflection sound.
        /// </summary>
        [Header("Shimmer Audio")]
        [SerializeField]
        private AudioSource shimmerAudioSource;

        /// <summary>
        /// Whether a visible crown schedules repeating reflection sweeps.
        /// </summary>
        [Header("Periodic Shimmer")]
        [SerializeField]
        private bool periodicShimmer = true;

        /// <summary>
        /// Duration of one reflection sweep, in seconds.
        /// </summary>
        [SerializeField, Min(0.01f)]
        private float shimmerDuration = 0.65f;

        /// <summary>
        /// Time between reflection sweep starts, including the sweep duration.
        /// </summary>
        [Tooltip("Time between sweep starts, including the sweep duration.")]
        [SerializeField, Min(0.01f)]
        private float shimmerPeriod = 4.5f;

        /// <summary>
        /// Cached shader property identifier for the crown reveal.
        /// </summary>
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");

        /// <summary>
        /// Cached shader property identifier for the automatic reflection toggle.
        /// </summary>
        private static readonly int AutoShimmerId = Shader.PropertyToID("_AutoShimmer");

        /// <summary>
        /// Cached shader property identifier for the manually driven reflection position.
        /// </summary>
        private static readonly int ShimmerPositionId = Shader.PropertyToID("_ShimmerPosition");

        /// <summary>
        /// Resolved crown Image used by the material controller.
        /// </summary>
        private Image image;

        /// <summary>
        /// Authored crown material restored when the controller is disabled.
        /// </summary>
        private Material originalMaterial;

        /// <summary>
        /// Per-instance crown material modified by this controller.
        /// </summary>
        private Material ownedMaterial;

        /// <summary>
        /// Active LitMotion handle for appearance or disappearance.
        /// </summary>
        private MotionHandle revealMotion;

        /// <summary>
        /// Active LitMotion handle for the reflection sweep and its delay.
        /// </summary>
        private MotionHandle shimmerMotion;

        /// <summary>
        /// Whether a hide, sprite swap, and appearance sequence owns the transition.
        /// </summary>
        private bool changingEmblem;

        /// <summary>
        /// Authored CanvasRenderer alpha restored after animation cleanup.
        /// </summary>
        private float originalEmblemAlpha = 1;

        /// <summary>
        /// Cached foreground emblem RectTransform.
        /// </summary>
        private RectTransform emblemRect;

        /// <summary>
        /// Authored emblem anchored position used as the animation rest pose.
        /// </summary>
        private Vector3 restingPosition;

        /// <summary>
        /// Authored emblem local scale used as the animation rest pose.
        /// </summary>
        private Vector3 restingScale;

        /// <summary>
        /// Authored crown anchored position restored after impact shake.
        /// </summary>
        private Vector3 restingCoronaPosition;

        /// <summary>
        /// Authored emblem local rotation restored after the rebound.
        /// </summary>
        private Quaternion restingRotation;

        /// <summary>
        /// Emblem anchored position captured at the start of the current transition.
        /// </summary>
        private Vector3 transitionPosition;

        /// <summary>
        /// Emblem local scale captured at the start of the current transition.
        /// </summary>
        private Vector3 transitionScale;

        /// <summary>
        /// Emblem local rotation captured at the start of the current transition.
        /// </summary>
        private Quaternion transitionRotation;

        /// <summary>
        /// Emblem alpha captured at the start of the current transition.
        /// </summary>
        private float transitionAlpha;

        /// <summary>
        /// Crown reveal captured at the start of the current transition.
        /// </summary>
        private float transitionReveal;

        /// <summary>
        /// Whether the current appearance has already emitted its contact effects.
        /// </summary>
        private bool impactTriggered;

        /// <summary>
        /// Whether the current reflection sweep has already started its chime.
        /// </summary>
        private bool shimmerSoundPlayed;

        /// <summary>
        /// Disappearance source volume captured on enable and used as the fade envelope peak.
        /// </summary>
        private float initialDisappearVolume;

        /// <summary>
        /// Shimmer source volume captured on enable and used as the reflection envelope peak.
        /// </summary>
        private float initialShimmerVolume;

        /// <summary>
        /// Gets litMotion scheduler selected by the unscaled-time setting.
        /// </summary>
        /// <value>LitMotion scheduler selected by the unscaled-time setting.</value>
        private IMotionScheduler Scheduler
        {
            get
            {
                return useUnscaledTime ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update;
            }
        }

        /// <summary>
        /// Gets whether runtime control is allowed and the crown material is ready.
        /// </summary>
        /// <value>True when the application is playing, the component is active, and material initialization succeeds.</value>
        private bool CanControl
        {
            get
            {
                return Application.isPlaying && isActiveAndEnabled && InitializeMaterial();
            }
        }

        #endregion

        #region Main Methods

        /// <summary>
        /// Initializes the crown material and rest pose, then optionally starts appearance.
        /// </summary>
        private void OnEnable()
        {
            initialDisappearVolume = disappearAudioSource != null ? disappearAudioSource.volume : 0;
            initialShimmerVolume = shimmerAudioSource != null ? shimmerAudioSource.volume : 0;

            if (!InitializeMaterial())
            {
                return;
            }

            changingEmblem = false;

            if (emblemImage != null)
            {
                originalEmblemAlpha = emblemImage.canvasRenderer.GetAlpha();
            }

            CacheRestPose();
            ApplyEmblem();

            SetReveal(0);
            SetEmblemAlpha(0);
            SetFloat(ShimmerPositionId, -1);
            State = VisibilityState.Hidden;

            if (showOnEnable)
            {
                Show();
            }
        }

        /// <summary>
        /// Cancels active effects and restores owned resources when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            StopEffects();
            SetReveal(0);

            if (disappearAudioSource != null)
            {
                disappearAudioSource.volume = initialDisappearVolume;
            }

            if (shimmerAudioSource != null)
            {
                shimmerAudioSource.volume = initialShimmerVolume;
            }

            RestoreRestPose();

            if (emblemImage != null)
            {
                emblemImage.canvasRenderer.SetAlpha(originalEmblemAlpha);
            }

            State = VisibilityState.Hidden;

            ReleaseMaterial();
        }

        /// <summary>
        /// Releases the per-instance crown material when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            ReleaseMaterial();
        }

        /// <summary>
        /// Clamps serialized animation timings to valid ranges in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            appearDuration = Mathf.Max(0, appearDuration);
            disappearDuration = Mathf.Max(0, disappearDuration);

            shimmerDuration = Mathf.Max(0.01f, shimmerDuration);
            shimmerPeriod = Mathf.Max(shimmerDuration, shimmerPeriod);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Resolves the crown Image and creates an independent material for runtime animation.
        /// </summary>
        /// <returns>True when the Image and its required shader properties are available; otherwise, false.</returns>
        private bool InitializeMaterial()
        {
            if (ownedMaterial != null)
            {
                return true;
            }

            image = coronaImage != null ? coronaImage : GetComponent<Image>();

            if (image == null)
            {
                Debug.LogError("Emblem Parent: assign the child corona Image.", this);
                return false;
            }

            originalMaterial = image.material;

            if (originalMaterial == null || !originalMaterial.HasProperty(RevealId)
                || !originalMaterial.HasProperty(AutoShimmerId) || !originalMaterial.HasProperty(ShimmerPositionId))
            {
                Debug.LogError("Emblem Parent: assign an Image material with _Reveal, _AutoShimmer and _ShimmerPosition.", this);
                return false;
            }

            ownedMaterial = new Material(originalMaterial)
            {
                name = originalMaterial.name + " (Corona Instance)",
                hideFlags = HideFlags.DontSave
            };

            ownedMaterial.DisableKeyword("CORONA_EDITOR_PREVIEW");
            ownedMaterial.SetFloat(AutoShimmerId, 0);
            image.material = ownedMaterial;

            return true;
        }

        /// <summary>
        /// Captures the authored emblem and crown transforms before animation changes them.
        /// </summary>
        private void CacheRestPose()
        {
            emblemRect = emblemImage != null ? emblemImage.rectTransform : null;

            if (emblemRect != null)
            {
                restingPosition = emblemRect.anchoredPosition3D;
                restingScale = emblemRect.localScale;
                restingRotation = emblemRect.localRotation;
            }

            restingCoronaPosition = image.rectTransform.anchoredPosition3D;
        }

        /// <summary>
        /// Restores the cached emblem and crown transforms without changing visibility.
        /// </summary>
        private void RestoreRestPose()
        {
            if (emblemRect != null)
            {
                emblemRect.anchoredPosition3D = restingPosition;
                emblemRect.localScale = restingScale;
                emblemRect.localRotation = restingRotation;
            }

            if (image != null)
            {
                image.rectTransform.anchoredPosition3D = restingCoronaPosition;
            }
        }

        /// <summary>
        /// Hides both visuals, selects the next sprite, and starts its strike appearance. Additional clicks are ignored until the sequence finishes.
        /// </summary>
        public void NextEmblem()
        {
            if (!CanControl || changingEmblem || emblemImage == null || emblems == null || emblems.Length == 0)
            {
                return;
            }

            changingEmblem = true;
            TransitionTo(false);
        }

        /// <summary>
        /// Assigns the selected sprite and preserves its aspect ratio without blocking UI raycasts.
        /// </summary>
        private void ApplyEmblem()
        {
            if (emblemImage == null || emblems == null || emblems.Length == 0)
            {
                return;
            }

            currentEmblemIndex = Mathf.Clamp(currentEmblemIndex, 0, emblems.Length - 1);

            emblemImage.sprite = emblems[currentEmblemIndex];
            emblemImage.preserveAspect = true;
            emblemImage.raycastTarget = false;
        }

        /// <summary>
        /// Cancels pending transitions and restarts appearance from a hidden state.
        /// </summary>
        public void Replay()
        {
            if (!CanControl)
            {
                return;
            }

            HideImmediate();
            Show();
        }

        /// <summary>
        /// Starts appearance unless the visuals are already visible or appearing.
        /// </summary>
        public void Show()
        {
            if (!CanControl)
            {
                return;
            }

            changingEmblem = false;

            if (State == VisibilityState.Visible || State == VisibilityState.Appearing)
            {
                return;
            }

            TransitionTo(true);
        }

        /// <summary>
        /// Starts retreat and dissolve unless the visuals are already hidden or disappearing.
        /// </summary>
        public void Hide()
        {
            if (!CanControl)
            {
                return;
            }

            changingEmblem = false;

            if (State == VisibilityState.Hidden || State == VisibilityState.Disappearing)
            {
                return;
            }

            TransitionTo(false);
        }

        /// <summary>
        /// Starts one LitMotion timeline coordinating reveal, emblem movement, and transition audio.
        /// </summary>
        /// <param name="visible">True to appear; false to retreat and dissolve.</param>
        private void TransitionTo(bool visible)
        {
            StopMotion(ref revealMotion);
            StopShimmer();

            float duration = Mathf.Max(0, visible ? appearDuration : disappearDuration);
            transitionReveal = Reveal;
            transitionAlpha = emblemImage != null ? emblemImage.canvasRenderer.GetAlpha() : 0;

            if (disappearAudioSource != null)
            {
                disappearAudioSource.Stop();
                if (!visible && duration > 0 && (Reveal > .001f || transitionAlpha > .001f) && disappearAudioSource.clip != null)
                {
                    disappearAudioSource.volume = initialDisappearVolume;
                    disappearAudioSource.Play();
                }
            }

            if (emblemRect != null)
            {
                if (visible && transitionAlpha <= .001f)
                {
                    emblemRect.anchoredPosition3D = restingPosition + Vector3.up * entryOffset;
                    emblemRect.localScale = restingScale * entryScale;
                    emblemRect.localRotation = restingRotation * Quaternion.Euler(0, 0, -7);
                }
                transitionPosition = emblemRect.anchoredPosition3D;
                transitionScale = emblemRect.localScale;
                transitionRotation = emblemRect.localRotation;
            }

            impactTriggered = false;
            State = visible ? VisibilityState.Appearing : VisibilityState.Disappearing;

            if (duration <= 0)
            {
                FinishTransition();
                return;
            }

            revealMotion = LMotion.Create(0f, 1f, duration)
                .WithEase(Ease.Linear)
                .WithScheduler(Scheduler)
                .WithOnComplete(FinishTransition)
                .Bind(this, static (value, self) => self.ApplyTransition(value));
        }

        /// <summary>
        /// Applies the current timeline value to the crown reveal, emblem pose, contact effects, and disappearance audio.
        /// </summary>
        /// <param name="progress">Normalized progress of the current transition, from zero to one.</param>
        private void ApplyTransition(float progress)
        {
            bool appearing = State == VisibilityState.Appearing;

            SetReveal(Mathf.LerpUnclamped(transitionReveal, appearing ? 1 : 0,
                EaseUtility.Evaluate(progress, appearing ? appearEase : disappearEase)));
            if (!appearing && disappearAudioSource != null)
            {
                float fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1, progress));
                disappearAudioSource.volume = initialDisappearVolume * (1 - fade);
            }

            if (emblemRect == null)
            {
                return;
            }

            if (!appearing)
            {
                float retreat = EaseUtility.Evaluate(progress, Ease.InCubic);
                emblemRect.anchoredPosition3D = Vector3.Lerp(transitionPosition, restingPosition + Vector3.up * exitOffset, retreat);
                emblemRect.localScale = Vector3.Lerp(transitionScale, restingScale * 1.18f, retreat);
                emblemRect.localRotation = Quaternion.Slerp(transitionRotation, restingRotation * Quaternion.Euler(0, 0, -8), retreat);
                SetEmblemAlpha(transitionAlpha * (1 - Mathf.SmoothStep(0, 1, progress)));
                image.rectTransform.anchoredPosition3D = Vector3.Lerp(image.rectTransform.anchoredPosition3D, restingCoronaPosition, progress);
                return;
            }

            const float anticipation = .22f;

            if (progress < impactTime)
            {
                float approach = Mathf.Clamp01((progress - anticipation) / (impactTime - anticipation));
                float strike = approach * approach * approach;
                emblemRect.anchoredPosition3D = Vector3.Lerp(transitionPosition, restingPosition, strike);
                emblemRect.localScale = Vector3.Lerp(transitionScale, restingScale, strike);
                emblemRect.localRotation = Quaternion.Slerp(transitionRotation, restingRotation, strike);
                SetEmblemAlpha(Mathf.Lerp(transitionAlpha, originalEmblemAlpha, Mathf.Clamp01(approach * 4)));
                return;
            }

            if (!impactTriggered)
            {
                impactTriggered = true;
                emblemRect.anchoredPosition3D = restingPosition;
                emblemRect.localScale = restingScale;
                emblemRect.localRotation = restingRotation;
                if (impactSparks != null)
                {
                    impactSparks.EmitImpact(emblemRect);
                }
                if (impactAudioSource != null && impactAudioSource.clip != null)
                {
                    impactAudioSource.Play();
                }
            }

            float settle = Mathf.InverseLerp(impactTime, 1, progress);
            float rebound = Mathf.Sin(settle * Mathf.PI * 2) * (1 - settle);
            emblemRect.anchoredPosition3D = restingPosition + Vector3.down * (rebound * 5);
            emblemRect.localScale = restingScale * (1 - rebound * .08f);
            emblemRect.localRotation = restingRotation;
            SetEmblemAlpha(originalEmblemAlpha);
            image.rectTransform.anchoredPosition3D = restingCoronaPosition + new Vector3(
                Mathf.Sin(settle * 28), Mathf.Sin(settle * 36), 0) * (impactShake * (1 - settle) * (1 - settle));
        }

        /// <summary>
        /// Applies exact transition endpoints, restores the rest pose, and continues a pending sprite change or reflection cycle.
        /// </summary>
        private void FinishTransition()
        {
            revealMotion = default;

            bool visible = State == VisibilityState.Appearing;
            if (!visible && disappearAudioSource != null)
            {
                disappearAudioSource.Stop();
            }

            if (visible)
            {
                ApplyTransition(1);
            }

            SetReveal(visible ? 1 : 0);
            SetEmblemAlpha(visible ? originalEmblemAlpha : 0);
            RestoreRestPose();
            State = visible ? VisibilityState.Visible : VisibilityState.Hidden;
            if (!visible && changingEmblem)
            {
                currentEmblemIndex = (currentEmblemIndex + 1) % emblems.Length;
                ApplyEmblem();
                TransitionTo(true);
                return;
            }

            changingEmblem = false;

            if (visible && periodicShimmer)
            {
                ScheduleShimmer(0);
            }
        }

        /// <summary>
        /// Cancels pending effects and makes both visuals fully visible immediately.
        /// </summary>
        public void ShowImmediate()
        {
            SetImmediate(true);
        }

        /// <summary>
        /// Cancels pending effects and hides both visuals immediately.
        /// </summary>
        public void HideImmediate()
        {
            SetImmediate(false);
        }

        /// <summary>
        /// Cancels animation and audio before applying an exact visibility state.
        /// </summary>
        /// <param name="visible">True for full visibility; false for complete hiding.</param>
        private void SetImmediate(bool visible)
        {
            if (!CanControl)
            {
                return;
            }

            StopEffects();

            SetReveal(visible ? 1 : 0);
            SetEmblemAlpha(visible ? originalEmblemAlpha : 0);
            RestoreRestPose();
            State = visible ? VisibilityState.Visible : VisibilityState.Hidden;
            if (visible && periodicShimmer)
            {
                ScheduleShimmer(0);
            }
        }

        /// <summary>
        /// Cancels pending transitions and reflection, clears contact particles, and stops transition audio.
        /// </summary>
        private void StopEffects()
        {
            changingEmblem = false;

            StopMotion(ref revealMotion);
            StopShimmer();

            if (impactSparks != null)
            {
                impactSparks.Clear();
            }

            if (impactAudioSource != null)
            {
                impactAudioSource.Stop();
            }

            if (disappearAudioSource != null)
            {
                disappearAudioSource.Stop();
            }
        }

        /// <summary>
        /// Restarts a reflection sweep when the crown is fully visible.
        /// </summary>
        public void PlayShimmer()
        {
            if (!CanControl || State != VisibilityState.Visible)
            {
                return;
            }

            StopShimmer();
            ScheduleShimmer(0);
        }

        /// <summary>
        /// Schedules a reflection sweep and resets its one-shot audio trigger.
        /// </summary>
        /// <param name="delay">Delay before the reflection begins, in scheduler seconds.</param>
        private void ScheduleShimmer(float delay)
        {
            SetFloat(ShimmerPositionId, -1);
            shimmerSoundPlayed = false;

            shimmerMotion = LMotion.Create(-0.9f, 0.9f, Mathf.Max(0.01f, shimmerDuration))
                .WithDelay(delay)
                .WithEase(Ease.Linear)
                .WithScheduler(Scheduler)
                .WithOnComplete(OnShimmerComplete)
                .Bind(this, static (value, self) => self.ApplyShimmer(value));
        }

        /// <summary>
        /// Updates the reflection shader position and synchronizes the chime trigger and volume envelope.
        /// </summary>
        /// <param name="position">Reflection position along the sweep, from -0.9 to 0.9.</param>
        private void ApplyShimmer(float position)
        {
            SetFloat(ShimmerPositionId, position);
            if (State != VisibilityState.Visible || shimmerAudioSource == null || shimmerAudioSource.clip == null)
            {
                return;
            }

            if (!shimmerSoundPlayed && position >= -.18f)
            {
                shimmerSoundPlayed = true;
                shimmerAudioSource.volume = initialShimmerVolume;
                shimmerAudioSource.Play();
            }

            if (shimmerSoundPlayed)
            {
                shimmerAudioSource.volume = initialShimmerVolume * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, .9f, position)));
            }
        }

        /// <summary>
        /// Stops the finished chime and schedules the next periodic sweep when appropriate.
        /// </summary>
        private void OnShimmerComplete()
        {
            if (shimmerAudioSource != null)
            {
                shimmerAudioSource.Stop();
            }

            SetFloat(ShimmerPositionId, -1);
            if (isActiveAndEnabled && State == VisibilityState.Visible && periodicShimmer)
            {
                ScheduleShimmer(Mathf.Max(0, shimmerPeriod - Mathf.Max(0.01f, shimmerDuration)));
            }
        }

        /// <summary>
        /// Cancels the reflection timeline, stops its audio, and resets the shader highlight.
        /// </summary>
        private void StopShimmer()
        {
            StopMotion(ref shimmerMotion);
            if (shimmerAudioSource != null)
            {
                shimmerAudioSource.Stop();
            }

            shimmerSoundPlayed = false;
            SetFloat(ShimmerPositionId, -1);
        }

        /// <summary>
        /// Clamps and writes the normalized crown reveal to its owned and rendered materials.
        /// </summary>
        /// <param name="value">Requested crown reveal, clamped between zero and one.</param>
        private void SetReveal(float value)
        {
            Reveal = Mathf.Clamp01(value);

            SetFloat(RevealId, Reveal);
        }

        /// <summary>
        /// Sets foreground CanvasRenderer alpha while preserving the authored Image tint.
        /// </summary>
        /// <param name="value">Alpha value applied to the foreground emblem.</param>
        private void SetEmblemAlpha(float value)
        {
            if (emblemImage != null)
            {
                emblemImage.canvasRenderer.SetAlpha(value);
            }
        }

        /// <summary>
        /// Writes a shader value to the owned material and any separate UI masking material.
        /// </summary>
        /// <param name="property">Cached shader property identifier.</param>
        /// <param name="value">Floating-point value assigned to the shader property.</param>
        private void SetFloat(int property, float value)
        {
            if (ownedMaterial == null)
            {
                return;
            }

            ownedMaterial.SetFloat(property, value);

            var rendered = image != null ? image.materialForRendering : null;
            if (rendered != null && rendered != ownedMaterial)
            {
                rendered.SetFloat(property, value);
            }
        }

        /// <summary>
        /// Cancels an active LitMotion handle and clears the caller reference.
        /// </summary>
        /// <param name="handle">Motion handle to cancel and reset.</param>
        private static void StopMotion(ref MotionHandle handle)
        {
            if (handle.IsActive())
            {
                handle.Cancel();
            }

            handle = default;
        }

        /// <summary>
        /// Restores the authored material and destroys the runtime material owned by this controller.
        /// </summary>
        private void ReleaseMaterial()
        {
            if (ownedMaterial == null)
            {
                return;
            }

            if (image != null && image.material == ownedMaterial)
            {
                image.material = originalMaterial;
            }

            if (Application.isPlaying)
            {
                Destroy(ownedMaterial);
            }

            else
            {
                DestroyImmediate(ownedMaterial);
            }

            ownedMaterial = null;
        }

        #endregion
    }
}
