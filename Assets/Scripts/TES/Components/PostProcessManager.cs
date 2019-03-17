﻿using Demonixis.Toolbox.XR;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TESUnity.Components
{
    [RequireComponent(typeof(PostProcessLayer))]
    public sealed class PostProcessManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool m_BypassEditor = true;
#endif

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();

#if UNITY_EDITOR
            if (TESManager.instance._bypassINIConfig && m_BypassEditor)
                yield break;
#endif
            UpdateEffects();
        }

        public void UpdateEffects()
        {
            var settings = TESManager.instance;
            var layer = GetComponent<PostProcessLayer>();
            var volume = FindObjectOfType<PostProcessVolume>();
            var profile = volume.profile;
            var xrEnabled = XRManager.Enabled;
            var mobile = false;

#if UNITY_ANDROID || UNITY_IOS
            mobile = true;
#endif

            if (settings.postProcessingQuality != PostProcessingQuality.None)
            {
                if (mobile)
                {
                    settings.postProcessingQuality = PostProcessingQuality.Low;
                    layer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                }

                var asset = Resources.Load<PostProcessProfile>($"Rendering/Effects/PostProcessVolume-{settings.postProcessingQuality}");
                volume.profile = asset;

                // SMAA is not supported in VR.
                if (xrEnabled && settings.antiAliasing == PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing)
                    settings.antiAliasing = PostProcessLayer.Antialiasing.TemporalAntialiasing;

                if (xrEnabled)
                {
                    // We use MSAA on mobile.
                    if (!mobile)
                    {
                        layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                        layer.fastApproximateAntialiasing.fastMode = true;
                    }

                    UpdateEffect<Bloom>(profile, (bloom) =>
                    {
                        bloom.dirtTexture.value = null;
                        bloom.dirtIntensity.value = 0;
                        bloom.fastMode.value = true;
                    });

                    DisableEffect<ScreenSpaceReflections>(profile);
                    DisableEffect<Vignette>(profile);
                    DisableEffect<MotionBlur>(profile);
                }
            } 
            else
            {
                volume.enabled = false;
                layer.enabled = false;
            }
        }

        private void SetEffectEnabled<T>(bool isEnabled) where T : MonoBehaviour
        {
            var component = GetComponent<T>();
            if (component != null)
                component.enabled = isEnabled;
            else
                Debug.LogWarningFormat("The component {0} doesn't exists", typeof(T));
        }

        private static void UpdateEffect<T>(PostProcessProfile profile, Action<T> callback) where T : PostProcessEffectSettings
        {
            T outParam;
            if (profile.TryGetSettings<T>(out outParam))
                callback(outParam);
        }

        public static void SetPostProcessEffectEnabled<T>(PostProcessProfile profile, bool isEnabled) where T : PostProcessEffectSettings
        {
            UpdateEffect<T>(profile, (e) => e.enabled.value = isEnabled);
        }

        private static void DisableEffect<T>(PostProcessProfile profile) where T : PostProcessEffectSettings
        {
            if (!profile.HasSettings<T>())
                return;

            profile.RemoveSettings<T>();
        }
    }
}
