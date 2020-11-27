﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using AntiLagMod.settings.views;
using AntiLagMod.settings;
using UnityEngine.Events;
using BS_Utils.Utilities;

namespace AntiLagMod
    
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class AntiLagModController : MonoBehaviour
    {
        #region global variables
        public static AntiLagModController Instance { get; private set; }

        private bool criticalError;

        private bool modEnabled;
        private float frameThreshold;
        private bool frameDropDetection;
        private float waitThenActiveTime;

        private bool isLevel;
        private bool test = true;

        private int frameRate;
        private int frameCounter = 0;
        private float timeCounter = 0.0f;
        private float lastFrameRate;
        private float refreshRate = 0.25f;

        private bool activePause = false;
        private bool waitThenActiveFireOnce;

        // frame drop stuff here ^^^
        // tracking issues here vvv

        private bool driftDetection;
        private float driftThreshold;

        public Saber rSaber;
        public Saber lSaber;
        private Vector3 rSaberPos;
        private Vector3 lSaberPos;
        private Vector3 prevRSaberPos;
        private Vector3 prevLSaberPos;

        public PlayerHeightDetector PlayerHeightDetector;



        public static PauseController PauseController;
        #endregion

        #region Monobehaviour Messages
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (Instance != null)
            {
                Plugin.Log?.Warn($"Instance of {GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            Instance = this;
            Plugin.Log?.Debug($"{name}: Awake()");

        }
        private void Start()
        {
            if (!criticalError)
                Refresh();
            waitThenActiveFireOnce = true;
        }
        private void Update()
        {
            CheckFrameRate();
            CheckEvents();
            if (isLevel && modEnabled)
            {
                #region frame drop detection
                if (frameDropDetection)
                {
                    //Plugin.Log.Debug("FR: " + frameRate);
                    if (waitThenActiveFireOnce)
                    {
                        Plugin.Log.Debug("Lag Detected...");
                        StartCoroutine(WaitThenActive());
                        waitThenActiveFireOnce = false;
                    }
                    if (activePause && (frameRate < frameThreshold))
                    {
                        activePause = false;
                        PauseController.Pause();
                        Plugin.Log.Warn("FPS DROP DETECTED");
                        Plugin.Log.Debug("FPS 1 frame before drop: " + frameRate);
                    }
                    if (!activePause)
                    {
                        PauseController.didResumeEvent += OnLevelResume;
                    }
                }
                #endregion

                #region tracking issues detection

                if (driftDetection)
                {

                }

                #endregion
            }
        }
        private void FixedUpdate()
        {
            if (isLevel && modEnabled)
            {
                //
            }
        }
        private void OnEnable()
        {
            isLevel = false;
        }
        private void OnDestroy()
        {
            Plugin.Log?.Debug($"{name}: OnDestroy()");
            if (Instance == this)
                Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

        }
        #endregion

        public static void Refresh() // refresh the class variables to equal the property variables
        {
            // I farted really hard when I wrote this
            Instance.modEnabled = Configuration.ModEnabled;
            Instance.frameDropDetection = Configuration.FrameDropDetectionEnabled;
            Instance.frameThreshold = Configuration.FrameThreshold;
            Instance.waitThenActiveTime = Configuration.WaitThenActive;

            Instance.driftDetection = Configuration.TrackingErrorDetectionEnabled;
            Instance.driftThreshold = Configuration.DriftThreshold;
        }
        private void CheckFrameRate()
        {
            if (frameCounter < refreshRate)
            {
                timeCounter += Time.deltaTime;
                frameCounter++;
            } else
            {
                lastFrameRate = frameCounter / timeCounter;
                frameCounter = 0;
                timeCounter = 0.0f;
            }
            frameRate = (int)lastFrameRate; // cast lastFrameRate as an int;
        }

        private IEnumerator WaitThenActive() // wait a certain amount of time before activating the pause mechanism
        {
            Plugin.Log.Debug("Waiting for the start of the level...");
            yield return new WaitForSeconds(waitThenActiveTime);
            activePause = true;
        }

        private IEnumerator WaitThenReActivate() // wait 2 seconds before reactivating the pause mechanism after its been fired
        {
            Plugin.Log.Debug("Resuming complete, reactivating active pause in 2 seconds");
            yield return new WaitForSeconds(2);
            activePause = true;
        }

        private void CheckEvents() // simply checks the events
        {
            BSEvents.gameSceneLoaded += OnLevelStart; 
            BSEvents.levelFailed += OnLevelFail;
            BSEvents.levelCleared += OnLevelClear;
            BSEvents.levelQuit += OnLevelQuit;
        }
        private void OnLevelStart() // level start delegate
        {
            //Plugin.Log.Debug("Level Started");
            isLevel = true;

            PauseController = Resources.FindObjectsOfTypeAll<PauseController>().FirstOrDefault();
            if (PauseController == null)
            {
                Plugin.Log.Error("Could not find PauseController object... Disabling mod...");
                modEnabled = false;
                criticalError = true;
            }

        }

        private void OnLevelFail(StandardLevelScenesTransitionSetupDataSO unused, LevelCompletionResults unusedResult) // level failed delegate
        {
            //Plugin.Log.Debug("LevelFailed");
            isLevel = false;
            activePause = false;
        }

        private void OnLevelClear(StandardLevelScenesTransitionSetupDataSO unused, LevelCompletionResults unusedResult) // level cleared delegate
        {
            //Plugin.Log.Debug("Level Ended");
            isLevel = false;
            activePause = false;
        }

        private void OnLevelQuit(StandardLevelScenesTransitionSetupDataSO unused, LevelCompletionResults unusedResult) // level quit delegate
        {
            //Plugin.Log.Debug("Level Quit");
            isLevel = false;
            activePause = false;
        }

        private void OnLevelResume() // level resumed delegate
        {
            StartCoroutine(WaitThenReActivate());
        }
    }
}
