﻿using Demonixis.Toolbox.XR;
using System.Collections;
using TESUnity.Inputs;
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
    public class PlayerXR : MonoBehaviour
    {
        private Transform _camTransform = null;
        private Transform _transform = null;
        private RectTransform _canvas = null;
        private Transform _pivotCanvas = null;
        private Transform m_HUD = null;

        [SerializeField]
        private Canvas _mainCanvas = null;

        /// <summary>
        /// Intialize the VR support for the player.
        /// - The HUD and UI will use a WorldSpace Canvas
        /// - The HUD canvas is not recommanded, it's usefull for small informations
        /// - The UI is for all other UIs: Menu, Life, etc.
        /// </summary>
        private void Start()
        {
            if (!XRManager.Enabled)
            {
                enabled = false;
                return;
            }

            var manager = TESManager.instance;
            var renderScale = manager.renderScale;

            if (renderScale > 0 && renderScale <= 2)
                XRManager.GetActiveDevice().RenderScale = manager.renderScale;

            // Setup RoomScale/Sitted mode.
            var trackingSpaceType = TESManager.instance.roomScale ? TrackingSpaceType.RoomScale : TrackingSpaceType.Stationary;
            XRDevice.SetTrackingSpaceType(trackingSpaceType);
            if (trackingSpaceType == TrackingSpaceType.RoomScale)
                transform.GetChild(0).localPosition = Vector3.zero;

            var uiManager = FindObjectOfType<UIManager>();

            if (uiManager != null)
            {
                if (_mainCanvas == null)
                    _mainCanvas = uiManager.GetComponent<Canvas>();

                if (_mainCanvas == null)
                    throw new UnityException("The Main Canvas Is Null");

                uiManager.Crosshair.Enabled = false;
            }

            _canvas = _mainCanvas.GetComponent<RectTransform>();
            _pivotCanvas = _canvas.parent;
            m_HUD = _canvas.Find("HUD");

            // Put the Canvas in WorldSpace and Attach it to the camera.
            _camTransform = Camera.main.GetComponent<Transform>();
            _transform = transform;

            // Add a pivot to the UI. It'll help to rotate it in the inverse direction of the camera.
            var uiPivot = new GameObject("UI Pivot");
            _pivotCanvas = uiPivot.GetComponent<Transform>();
            _pivotCanvas.parent = transform;
            _pivotCanvas.localPosition = Vector3.zero;
            _pivotCanvas.localRotation = Quaternion.identity;
            _pivotCanvas.localScale = Vector3.one;
            GUIUtils.SetCanvasToWorldSpace(_canvas.GetComponent<Canvas>(), _pivotCanvas, 1.0f, 0.002f);

            // Setup the camera
            Camera.main.nearClipPlane = 0.1f;

            var motionControllers = GetComponentsInChildren<MotionController>(true);
            foreach (var controller in motionControllers)
                controller.transform.parent = _camTransform.parent;

            RecenterOrientationAndPosition();
        }

        private void Update()
        {
            if (_pivotCanvas == null)
                return;

            // At any time, the user might want to reset the orientation and position.
            if (InputManager.GetButtonDown("Recenter"))
                RecenterOrientationAndPosition();

            RecenterUI();

            var centerEye = _camTransform;
            var root = centerEye.parent;
            var prevPos = root.position;
            var prevRot = root.rotation;

            if (TESManager.instance.followHeadDirection)
            {
                _transform.rotation = Quaternion.Euler(0.0f, centerEye.rotation.eulerAngles.y, 0.0f);
                root.rotation = prevRot;
            }

            if (TESManager.instance.roomScale)
            {
                //_transform.position = new Vector3(centerEye.position.x, 0.0f, centerEye.position.z);
                root.position = prevPos;
            }
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

        /// <summary>
        /// Reset the orientation and the position of the HMD with a delay of 0.1ms.
        /// </summary>
        public void RecenterOrientationAndPosition()
        {
            InputTracking.Recenter();
            RecenterUI();
        }

        /// <summary>
        /// Sent by the PlayerComponent when the pause method is called.
        /// </summary>
        /// <param name="paused">Boolean: Indicates if the player is paused.</param>
        private void OnPlayerPause(bool paused)
        {
            if (paused)
                RecenterUI();
        }
    }
}