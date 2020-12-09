﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using BeatSaberMarkupLanguage;

namespace AntiLagMod
{
    public class TextObj : MonoBehaviour
    {
        private Canvas dasCanvas;
        private Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector3 scale = new Vector3(1.0f, 1.0f, 1.0f);
        private Vector2 canvAnchorPos = new Vector2(0.0f, 0.0f);
        private Vector2 canvScale = new Vector2(1.0f, 1.0f);
        private Vector3 rectTransformPos = new Vector3(0.0f, 0.0f, 0.0f);

        private float textFontSize = 10f;
        private static TMP_Text indicatorTMPText; 
        private void OnLoad()
        {
            new GameObject("IndTextObj").AddComponent<TextObj>();
        }
        private void Awake() // make pub if didnt fire
        {
            Plugin.Log.Debug("TextObject Awake()");

        }
        public void Create()
        {
            try
            {
                // setup text obj
                gameObject.transform.localScale = scale;
                dasCanvas = gameObject.AddComponent<Canvas>();
                dasCanvas.renderMode = RenderMode.WorldSpace;
                var rectTransform = (RectTransform) dasCanvas.transform;
                rectTransform.sizeDelta = canvScale;

                indicatorTMPText = BeatSaberUI.CreateText(rectTransform, "", canvAnchorPos);
                indicatorTMPText.alignment = TextAlignmentOptions.Center;
                indicatorTMPText.overflowMode = TextOverflowModes.Overflow;
                indicatorTMPText.fontSize = textFontSize;
                //indicatorTMPText.enableWordWrapping = false;
                indicatorTMPText.rectTransform.position = rectTransformPos;

            } catch (Exception exception)
            {
                AntiLagModController.ExternalCriticalError("TextObj.cs", 0, exception);
            }
        }
    }
}
