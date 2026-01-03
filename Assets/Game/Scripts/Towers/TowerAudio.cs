using UnityEngine;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Handles audio playback for tower repair and damage events.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TowerAudio : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip repairTickClip;
        [SerializeField] private AudioClip damageHitClip;
        [SerializeField] [Range(0f, 1f)] private float repairTickVolume = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float damageHitVolume = 0.65f;
        [SerializeField] private float repairSfxInterval = 0.12f;
        [SerializeField] private float hitSfxInterval = 0.08f;

        private float _lastRepairSfxTime;
        private float _lastHitSfxTime;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound for readability
                audioSource.loop = false;
            }
        }

        public void PlayRepairTickSfx()
        {
            if (repairTickClip == null || audioSource == null) return;
            if (Time.time - _lastRepairSfxTime < Mathf.Max(0.02f, repairSfxInterval)) return;
            _lastRepairSfxTime = Time.time;
            audioSource.PlayOneShot(repairTickClip, repairTickVolume);
        }

        public void PlayHitSfx()
        {
            if (damageHitClip == null || audioSource == null) return;
            if (Time.time - _lastHitSfxTime < Mathf.Max(0.02f, hitSfxInterval)) return;
            _lastHitSfxTime = Time.time;
            audioSource.PlayOneShot(damageHitClip, damageHitVolume);
        }
    }
}
