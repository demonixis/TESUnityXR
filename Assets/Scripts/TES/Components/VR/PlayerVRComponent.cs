﻿using System.Collections;
using TESUnity.UI;
using UnityEngine;
using UnityEngine.XR;

namespace TESUnity.Components.VR
{
    /// <summary>
    /// This component is responsible to enable the VR feature and deal with VR SDKs.
    /// VR SDKs allows us to provide more support (moving controller, teleportation, etc.)
    /// To enable a VR SDKs, please read the README.md file located in the Vendors folder.
    /// </summary>
    public class PlayerVRComponent : MonoBehaviour
    {
        private Transform _camTransform = null;
        private RectTransform _canvas = null;
        private Transform _pivotCanvas = null;
        private RectTransform _hud = null;

        [SerializeField]
        private bool _isSpectator = false;
        [SerializeField]
        private Canvas _mainCanvas = null;


        /// <summary>
        /// Intialize the VR support for the player.
        /// - The HUD and UI will use a WorldSpace Canvas
        /// - The HUD canvas is not recommanded, it's usefull for small informations
        /// - The UI is for all other UIs: Menu, Life, etc.
        /// </summary>
        void Start()
        {
            if (!XRSettings.enabled)
            {
                Destroy(this);
                return;
            }

            var uiManager = FindObjectOfType<UIManager>();

            if (_mainCanvas == null)
                _mainCanvas = uiManager.GetComponent<Canvas>();

            if (_mainCanvas == null)
                throw new UnityException("The Main Canvas Is Null");

            uiManager.Crosshair.Enabled = false;

            _canvas = _mainCanvas.GetComponent<RectTransform>();
            _pivotCanvas = _canvas.parent;

            // Put the Canvas in WorldSpace and Attach it to the camera.
            _camTransform = Camera.main.GetComponent<Transform>();

            // Add a pivot to the UI. It'll help to rotate it in the inverse direction of the camera.
            var uiPivot = new GameObject("UI Pivot");
            _pivotCanvas = uiPivot.GetComponent<Transform>();
            _pivotCanvas.parent = transform;
            _pivotCanvas.localPosition = Vector3.zero;
            _pivotCanvas.localRotation = Quaternion.identity;
            _pivotCanvas.localScale = Vector3.one;
            GUIUtils.SetCanvasToWorldSpace(_canvas.GetComponent<Canvas>(), _pivotCanvas, 1.0f, 0.002f);

            // Add the HUD, its fixed to the camera.
            if (_isSpectator)
                ShowUICursor(true);

            // Setup the camera
            Camera.main.nearClipPlane = 0.1f;

            ResetOrientationAndPosition();
        }

        void Update()
        {
            // At any time, the user might want to reset the orientation and position.
            if (Input.GetButtonDown("Recenter"))
                ResetOrientationAndPosition();

            RecenterUI(true);
        }

        public void ShowUICursor(bool visible)
        {
            // TODO: Add hand selector for the Touchs and the Vive.
            var uiCursor = GetComponentInChildren<VRGazeUI>(true);
            uiCursor.SetActive(visible);
        }

        /// <summary>
        /// Recenter the Main UI.
        /// </summary>
        private void RecenterUI(bool onlyPosition = false)
        {
            if (!XRSettings.enabled)
                return;

            if (!onlyPosition)
            {
                var pivotRot = _pivotCanvas.localRotation;
                pivotRot.y = _camTransform.localRotation.y;
                _pivotCanvas.localRotation = pivotRot;
            }

            var camPosition = _camTransform.position;
            var targetPosition = _pivotCanvas.position;
            targetPosition.y = camPosition.y;
            _pivotCanvas.position = targetPosition;
        }

        private IEnumerator ResetOrientationAndPosition(float delay)
        {
            yield return new WaitForSeconds(delay);

            InputTracking.Recenter();

            RecenterUI();
        }

        /// <summary>
        /// Reset the orientation and the position of the HMD with a delay of 0.1ms.
        /// </summary>
        public void ResetOrientationAndPosition()
        {
            StartCoroutine(ResetOrientationAndPosition(0.1f));
        }

        /// <summary>
        /// Sent by the PlayerComponent when the pause method is called.
        /// </summary>
        /// <param name="paused">Boolean: Indicates if the player is paused.</param>
        void OnPlayerPause(bool paused)
        {
            if (paused)
                RecenterUI();
        }
    }
}