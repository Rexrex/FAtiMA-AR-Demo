﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Add to any Object to spawn a prefab to it, according to preference
    /// </summary>
    [AddComponentMenu("Scripts/MRTK/SDK/PrefabSpawner")]
    public class PrefabSpawner :
    BaseFocusHandler,
    IMixedRealityInputHandler,
    IMixedRealityInputHandler<float>
    {
        private enum VanishType
        {
            /// <summary>
            /// Causes the tooltip to vanish immediately after disappearing. Ignores vanishDelay.
            /// </summary>
            VanishOnFocusExit = 0,
            VanishOnTap,

            /// <summary>
            /// Equivalent to VanishOnFocusExit. but honors vanishDelay and will only disappear
            /// after that many seconds elapses after focus loss.
            /// </summary>
            VanishOnFocusExitWithDelay,
        }

        private enum AppearType
        {
            AppearOnFocusEnter = 0,
            AppearOnTap,
        }

        public enum RemainType
        {
            Indefinite = 0,
            Timeout,
        }

        [SerializeField, FormerlySerializedAs("toolTipPrefab")]
        protected GameObject prefab = null;

        [Header("Input Settings")]
        [SerializeField]
        [Tooltip("The action that will be used for when to spawn or toggle the tooltip.")]
        private MixedRealityInputAction tooltipToggleAction = MixedRealityInputAction.None;

        [Header("Appear / Vanish Behavior Settings")]
        [SerializeField]
        private AppearType appearType = AppearType.AppearOnFocusEnter;
        [SerializeField]
        private VanishType vanishType = VanishType.VanishOnFocusExit;
        [SerializeField]
        private RemainType remainType = RemainType.Timeout;
        [Header("Timing")]
        [SerializeField]
        [Range(0f, 5f)]
        private float appearDelay = 0.0f;
        [SerializeField]
        [Range(0f, 5f)]
        [Tooltip("The number of seconds that must elapse before the tooltip will disappear. Only used when vanishType is VanishOnFocusExitWithDelay.")]
        private float vanishDelay = 2.0f;
        [SerializeField]
        [Range(0.5f, 10.0f)]
        private float lifetime = 1.0f;
        [Header("Orientation")]
        [SerializeField]
        private bool keepWorldRotation = true;

        private float focusEnterTime = 0f;
        private float focusExitTime = 0f;
        private float tappedTime = 0f;

        private GameObject spawnable;

        private async void ShowSpawnable()
        {
            await UpdateSpawnable(focusEnterTime, tappedTime);
        }

        private async Task UpdateSpawnable(float focusEnterTimeOnStart, float tappedTimeOnStart)
        {
            if (spawnable == null)
            {
                spawnable = Instantiate(prefab);
                spawnable.transform.parent = transform;
                spawnable.transform.localPosition = Vector3.zero;
                if (!keepWorldRotation)
                {
                    spawnable.transform.localRotation = Quaternion.identity;
                }
                spawnable.gameObject.SetActive(false);
            }
            if (appearType == AppearType.AppearOnFocusEnter)
            {
                // Wait for the appear delay
                await new WaitForSeconds(appearDelay);
                // If we don't have focus any more, get out of here

                if (!HasFocus)
                {
                    return;
                }
            }

            spawnable.gameObject.SetActive(true);

            SpawnableActivated(spawnable);

            while (spawnable.gameObject.activeSelf)
            {
                if (remainType == RemainType.Timeout)
                {
                    switch (appearType)
                    {
                        case AppearType.AppearOnTap:
                            if (Time.unscaledTime - tappedTime >= lifetime)
                            {
                                spawnable.gameObject.SetActive(false);
                                return;
                            }

                            break;
                        case AppearType.AppearOnFocusEnter:
                            if (Time.unscaledTime - focusEnterTime >= lifetime)
                            {
                                spawnable.gameObject.SetActive(false);
                                return;
                            }

                            break;
                    }
                }

                // Check whether we're supposed to disappear
                switch (vanishType)
                {
                    case VanishType.VanishOnFocusExit:
                        if (!HasFocus)
                        {
                            spawnable.gameObject.SetActive(false);
                        }

                        break;

                    case VanishType.VanishOnTap:
                        if (!tappedTime.Equals(tappedTimeOnStart))
                        {
                            spawnable.gameObject.SetActive(false);
                        }

                        break;

                    case VanishType.VanishOnFocusExitWithDelay:
                    default:
                        if (!HasFocus && HasVanishDelayElapsed())
                        {
                            spawnable.gameObject.SetActive(false);
                        }
                        break;
                }

                await new WaitForUpdate();
            }
        }

        protected virtual void SpawnableActivated(GameObject spawnable) { }

        /// <inheritdoc />
        public override void OnFocusEnter(FocusEventData eventData)
        {
            base.OnFocusEnter(eventData);

            HandleFocusEnter();
        }

        /// <inheritdoc />
        public override void OnFocusExit(FocusEventData eventData)
        {
            base.OnFocusExit(eventData);

            HandleFocusExit();
        }

        /// <inheritdoc />
        void IMixedRealityInputHandler<float>.OnInputChanged(InputEventData<float> eventData)
        {
            if (eventData.InputData > .95f)
            {
                HandleTap();
            }
        }

        /// <inheritdoc />
        void IMixedRealityInputHandler.OnInputDown(InputEventData eventData)
        {
            if (tooltipToggleAction.Id == eventData.MixedRealityInputAction.Id)
            {
                HandleTap();
            }
        }

        /// <inheritdoc />
        void IMixedRealityInputHandler.OnInputUp(InputEventData eventData) { }

        protected virtual void HandleTap()
        {
            tappedTime = Time.unscaledTime;

            if (spawnable == null || !spawnable.gameObject.activeSelf)
            {
                switch (appearType)
                {
                    case AppearType.AppearOnTap:
                        ShowSpawnable();
                        break;
                }
            }
            else
            {
                switch (vanishType)
                {
                    case VanishType.VanishOnTap:
                        spawnable.gameObject.SetActive(false);
                        break;
                }
            }
        }

        private void HandleFocusEnter()
        {
            focusEnterTime = Time.unscaledTime;

            if (spawnable == null || !spawnable.gameObject.activeSelf)
            {
                switch (appearType)
                {
                    case AppearType.AppearOnFocusEnter:
                        ShowSpawnable();
                        break;
                }
            }
        }

        private void HandleFocusExit()
        {
            focusExitTime = Time.unscaledTime;
        }

        private Boolean HasVanishDelayElapsed()
        {
            return Time.unscaledTime - focusExitTime > vanishDelay;
        }
    }
}
