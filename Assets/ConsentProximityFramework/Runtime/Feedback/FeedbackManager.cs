using UnityEngine;
using System.Collections;
using ConsentProximity.Core;
using ConsentProximityFramework.Runtime.Networking;
#if UNITY_XR_INTERACTION_TOOLKIT
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace ConsentProximityFramework.Runtime.Feedback
{
    /// <summary>
    /// Concrete feedback implementation: color changes, haptic impulses,
    /// audio cues, and a pulse animation driven by consent state changes.
    /// Author: Oscar Canoa
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        [Header("Flow Manager")]
        public ConsentFlowManager flowManager;

        [Header("Visual")]
        public Renderer visualRenderer;

        [Tooltip("GameObject (ring, glow, particle system, etc.) shown ONLY while interaction is Active.")]
        public GameObject activeVFX;

        [Header("Haptics")]
#if UNITY_XR_INTERACTION_TOOLKIT
        public XRBaseController controller;
#endif

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip requestSound;
        public AudioClip acceptSound;
        public AudioClip rejectSound;

        void Awake()
        {
            SetActiveVFX(false);

            if (flowManager != null)
            {
                flowManager.OnStateChanged += HandleStateChanged;
            }
            else
            {
                Debug.LogWarning("FeedbackManager: FlowManager reference is missing.");
            }
        }

        void OnDisable()
        {
            if (flowManager != null)
            {
                flowManager.OnStateChanged -= HandleStateChanged;
            }
        }

        void HandleStateChanged(ConsentState newState)
        {
            Debug.Log("Feedback received new state: " + newState);

            // Active-only VFX: visible exclusively during the Active state
            SetActiveVFX(newState == ConsentState.Active);

            switch (newState)
            {
                case ConsentState.Idle:
                    SetColor(Color.white);
                    break;

                case ConsentState.InRange:
                    SetColor(Color.blue);
                    break;

                case ConsentState.Requested:
                    SetColor(Color.yellow);
                    Vibrate(0.2f, 0.1f);
                    AnimatePulse();
                    PlaySound(requestSound);
                    break;

                case ConsentState.Active:
                    SetColor(Color.green);
                    Vibrate(0.5f, 0.2f);
                    PlaySound(acceptSound);
                    break;

                case ConsentState.Terminated:
                    SetColor(Color.red);
                    Vibrate(0.3f, 0.15f);
                    PlaySound(rejectSound);
                    break;
            }
        }

        void SetActiveVFX(bool visible)
        {
            if (activeVFX != null)
            {
                activeVFX.SetActive(visible);
            }
        }

        void SetColor(Color color)
        {
            if (visualRenderer != null)
            {
                visualRenderer.material.color = color;
            }
        }

        void Vibrate(float intensity, float duration)
        {
#if UNITY_XR_INTERACTION_TOOLKIT
            if (controller != null)
            {
                controller.SendHapticImpulse(intensity, duration);
            }
#else
            Debug.Log($"[Haptic] intensity={intensity} duration={duration} (XR Interaction Toolkit not installed)");
#endif
        }

        void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        void AnimatePulse()
        {
            StopAllCoroutines();
            StartCoroutine(PulseEffect());
        }

        IEnumerator PulseEffect()
        {
            float time = 0f;
            Vector3 originalScale = transform.localScale;

            while (time < 0.5f)
            {
                float scale = 1f + Mathf.Sin(time * 10f) * 0.1f;
                transform.localScale = originalScale * scale;
                time += Time.deltaTime;
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}
