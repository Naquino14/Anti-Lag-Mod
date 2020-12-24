﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using AntiLagMod.settings.views;
using AntiLagMod.settings;
using UnityEngine.Events;
using BS_Utils.Utilities;
using System.Reflection;
using AntiLagMod.StreamingAssets;

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

        private BBCollider bbColliderScript;

        public bool criticalError;
        public bool isPaused;

        private bool modEnabled;
        private float frameThreshold;
        private bool frameDropDetection;
        private float waitThenActiveTime;

        private bool isLevel;
        private bool subToResumeFireOnce = true;

        private int frameRate;
        private int frameCounter = 0;
        private float timeCounter = 0.0f;
        private float lastFrameRate;
        private float refreshRate = 0.25f;

        private bool activePause = false;
        private bool waitThenActiveFireOnce;

        // frame drop stuff here ^^^
        // tracking issues here vvv

        private bool trackingActiveFireOnce;

        private bool driftDetected;
        private bool trackingLossDetected = false;

        private bool trackingIssueDetection;
        private float driftThreshold;

        public SaberManager saberManager;
        public Saber rSaber;
        public Saber lSaber;
        private Vector3 rSaberPos;
        private Vector3 lSaberPos;
        private Vector3 prevRSaberPos;
        private Vector3 prevLSaberPos;
        private Quaternion rSaberRot;
        private Quaternion lSaberRot;
        private Quaternion prevRSaberRot;
        private Quaternion prevLSaberRot;
        private Transform rSaberTransform;
        private Transform lSaberTransform;
        private BBCollider rSaberCollider;
        private BBCollider lSaberCollider;
        private int framesSinceLastSaberPosUpdate = 0;

        private float playerHeight;
        
        public static PauseController PauseController;

        // tracking issues here ^^^
        // asset bundle stuff here vvv

        AssetBundle dasCuubenAssetBundle;
        private GameObject cubeHolder;
        private Material cubeMaterial;
        private Shader cubeShader;
        private BoxCollider cubeCollider;

        private GameObject activeCubeHolder;
        private bool activeCubeActive = false; // bruh\

        public bool boundingBoxEnabled;

        private bool bbFireOnce = true;

        private GameObject colliderCubeActive;
        private bool colliderBBFireOnce = true;
        private bool colliderBBactive;

        Vector3 bbScale;
        private float bbScaleDivider = 20;

        private bool attatchColliderScriptFireOnce = true;
        #endregion

        #region Monobehaviour Messages
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (Instance != null)
            {
                Plugin.Log?.Warn($"Instance of {GetType().Name} already exists, destroying.");
                DestroyImmediate(this);
                return;
            }
            DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            Instance = this;
            Plugin.Log?.Debug($"{name}: Awake()");

        }
        private void Start()
        {
            if (!criticalError)
                Refresh();
            waitThenActiveFireOnce = true;
            trackingActiveFireOnce = true;
            CheckEvents();
            LoadAssetBundles();
            TextObj.OnLoad();
        }
        private void Update()
        {
            if (criticalError)
            {
                SettingsView.Disable();
            }
            CheckFrameRate();
            bbScale = new Vector3(driftThreshold / bbScaleDivider, driftThreshold / bbScaleDivider, driftThreshold / bbScaleDivider);
            if (isLevel && modEnabled)
            {
                if (waitThenActiveFireOnce)
                {
                    StartCoroutine(WaitThenActive());
                    waitThenActiveFireOnce = false;
                }
                if (!activePause && subToResumeFireOnce)
                {
                    PauseController.didResumeEvent += OnLevelResume;
                    subToResumeFireOnce = false;
                }

                #region frame drop detection
                if (frameDropDetection)
                {
                    //Plugin.Log.Debug("FR: " + frameRate); // check framerate
                    if (activePause && (frameRate < frameThreshold))
                    {
                        Pause();
                        Plugin.Log.Warn("FPS DROP DETECTED");
                        Plugin.Log.Debug("FPS 1 frame before drop: " + frameRate);
                        SetText("FPS Drop Detected", Color.magenta);
                    }
                }
                #endregion

                #region tracking issues detection

                if (trackingIssueDetection && activePause)
                {
                    trackingLossDetected = false;
                    // check saber positions
                    //Debug.Log("x" + rSaberPos.x + " y" + rSaberPos.y + " z" + rSaberPos.z);
                    //Debug.Log("px" + rSaberPos.x + " py" + rSaberPos.y + " pz" + rSaberPos.z);
                    string whichController = "(error getting which controller)";
                    CheckSaberPos(Frame.First);

                    #region tracking loss detection

                    bool moveOn = false;
                    bool both = false;

                    if (rSaberPos == prevRSaberPos && rSaberRot == prevRSaberRot)
                    {
                        trackingLossDetected = true;
                        whichController = "right";
                        moveOn = true;
                    }
                    if (lSaberPos == prevLSaberPos && lSaberRot == prevLSaberRot)
                    {
                        trackingLossDetected = true;
                        whichController = "left";
                        if (moveOn)
                            both = true;
                    }
                    if (both)
                        whichController = "left & right";
                    if (activePause && trackingLossDetected)
                    {
                        Plugin.Log.Debug("Tracking issues detected with " + whichController + " controller.");
                        Pause();
                        SetText("Tracking issues detected with " + whichController + " controller.", Color.yellow);
                    }

                    CheckSaberPos(Frame.Last);
                    #endregion

                    #region drift detection

                    if (attatchColliderScriptFireOnce)
                    {
                        Plugin.Log.Debug("Attatching collider script...");
                        AttatchColliderScript();
                        attatchColliderScriptFireOnce = false;
                    }

                    // note the actual area where it pauses is a static function down below!

                    #endregion
                }

                if (colliderBBFireOnce)
                {
                    colliderBBFireOnce = false;
                    CreateBBCollider();
                }
                #endregion

            }
            #region drift detection settings

            if (boundingBoxEnabled)
            {
                if (bbFireOnce)
                {
                    Plugin.Log.Debug("Attempting to make menu bb...");
                    bbFireOnce = false;
                    try
                    {
                        activeCubeHolder = Instantiate(cubeHolder);
                    } catch (Exception exception)
                    {
                        CriticalErrorHandler(true, 237, exception);
                    }
                    
                    activeCubeHolder.transform.position = new Vector3(0, playerHeight, 0);
                    activeCubeActive = true;
                }
                if (activeCubeActive)
                {
                    activeCubeHolder.transform.localScale = bbScale;
                    activeCubeHolder.transform.position = new Vector3(0, playerHeight, 0);
                }
            }

            #endregion
            #region diagnostics (this is temporary)

            if (Input.GetKeyDown(KeyCode.H))
            {
                Diagnose();
            }

            #endregion
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
        public void LoadAssetBundles() // dies of bruh
        {
            try
            {
                Plugin.Log.Debug("Locating asset bundle and assigning it to _stream...");
                var _stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AntiLagMod.StreamingAssets.almmod");
                var _assetBundle = AssetBundle.LoadFromStream(_stream);

                Plugin.Log.Debug("Attempting to load assets...");
                dasCuubenAssetBundle = _assetBundle;
                cubeHolder = _assetBundle.LoadAsset<GameObject>("Assets/ALM/CubeContainer.prefab");
                cubeMaterial = _assetBundle.LoadAsset<Material>("Assets/ALM/dascuuben.mat");
                cubeShader = _assetBundle.LoadAsset<Shader>("Assets/ALM/sh_custom_unlit.shader");
                cubeCollider = cubeHolder.GetComponent<BoxCollider>();

                Plugin.Log.Debug("Success! Loaded all assets.");

            } catch(Exception exception)
            {
                CriticalErrorHandler(true, 261, exception);
            } // gabe shut the fi*ck up that wasnt funny (bad wrord))
              // im going to come to your house andd eat all your food
        }

        private enum Frame
        {
            First,
            Last
        }
        private void CheckSaberPos(Frame frame) // not sure wether to simplify or leave this as a second stage of null checks...
        {
            switch (frame)
            {
                case Frame.First:
                    if (rSaber != null || lSaber != null)
                    {
                        rSaberPos = rSaber.handlePos;
                        lSaberPos = lSaber.handlePos;
                        rSaberRot = rSaber.handleRot;
                        lSaberRot = lSaber.handleRot;
                    } else
                    {
                        Plugin.Log.Warn("Sabers could not be found.");
                        CriticalErrorHandler(true, 324);
                    }
                    break;
                case Frame.Last:
                    if (rSaber != null || lSaber != null)
                    {
                        prevRSaberPos = rSaber.handlePos;
                        prevLSaberPos = lSaber.handlePos;
                        prevRSaberRot = rSaber.handleRot;
                        prevLSaberRot = lSaber.handleRot;
                    } else
                    {
                        Plugin.Log.Warn("Sabers could not be found.");
                        CriticalErrorHandler(true, 337);
                    }
                    break;
            }
        }

        private void Pause()
        {
            Plugin.Log.Debug("Pausing...");
            activePause = false;
            isPaused = true;
            PauseController.Pause();
        }

        private void FindSabers()
        {
            Plugin.Log.Debug("Looking for sabers...");
            try
            {
                saberManager = Resources.FindObjectsOfTypeAll<SaberManager>().FirstOrDefault();
                rSaber = saberManager.rightSaber;
                lSaber = saberManager.leftSaber;
            } catch (Exception exception)
            {
                CriticalErrorHandler(true, 324, exception);
            }
            Plugin.Log.Debug("Success! Found sabers.");
        }

        public static void Refresh() // refresh the class variables to equal the property variables
        {
            // I farted really hard when I wrote this
            Plugin.Log.Debug("Refreshing Variables...");
            Instance.modEnabled = Configuration.ModEnabled;
            Instance.frameDropDetection = Configuration.FrameDropDetectionEnabled;
            Instance.frameThreshold = Configuration.FrameThreshold;
            Instance.waitThenActiveTime = Configuration.WaitThenActive;

            Instance.trackingIssueDetection = Configuration.TrackingErrorDetectionEnabled;
            Instance.driftThreshold = Configuration.DriftThreshold;

            Instance.playerHeight = Configuration.PlayerHeight;
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
            Plugin.Log.Debug("Active pause enabled.");
        }

        private IEnumerator WaitThenReActivate() // wait 2 seconds before reactivating the pause mechanism after its been fired
        {
            Plugin.Log.Debug("Resuming complete, reactivating active pause in 2 seconds");
            yield return new WaitForSeconds(2);
            activePause = true;
            isPaused = false;
            Plugin.Log.Debug("Active Pause re-enabled.");
        }

        private void CheckEvents() // simply subscribes to the the events
        {
            BSEvents.gameSceneLoaded += OnLevelStart; 
            BSEvents.levelFailed += OnLevelFail;
            BSEvents.levelCleared += OnLevelClear;
            BSEvents.levelQuit += OnLevelQuit;
            BSEvents.levelRestarted += OnLevelRestart;
        }

        private void OnLevelStart() // level start delegate
        {
            //Plugin.Log.Debug("Level Started");
            isLevel = true;
            isPaused = false;
            if (modEnabled)
            {
                Plugin.Log.Debug("Level started... Looking for PauseController");
                PauseController = Resources.FindObjectsOfTypeAll<PauseController>().FirstOrDefault();
                FindSabers();
                if (PauseController == null)
                {
                    Plugin.Log.Warn("PauseController not found.");
                    CriticalErrorHandler(true, 392);
                }

                if (rSaber == null || lSaber == null)
                {
                    Plugin.Log.Warn("FindSabers() not fired or failed...");
                    CriticalErrorHandler(true, 393);
                }
                if (!criticalError)
                {
                    Plugin.Log.Debug("Success! Found PauseController and Sabers.");
                }
            }

        }

        private void OnLevelFail(StandardLevelScenesTransitionSetupDataSO unused, LevelCompletionResults unusedResult) // level failed delegate
        {
            //Plugin.Log.Debug("LevelFailed");
            isLevel = false;
            activePause = false;
            isPaused = false;
            DestroyBBCollider();
            ResetSaberPos();
            ResetFireOnce();
        }

        private void OnLevelClear(StandardLevelScenesTransitionSetupDataSO unused, LevelCompletionResults unusedResult) // level cleared delegate
        {
            //Plugin.Log.Debug("Level Ended");
            isLevel = false;
            activePause = false;
            isPaused = false;
            DestroyBBCollider();
            ResetSaberPos();
            ResetFireOnce();
        }

        private void OnLevelQuit(StandardLevelScenesTransitionSetupDataSO unused, LevelCompletionResults unusedResult) // level quit delegate
        {
            //Plugin.Log.Debug("Level Quit");
            isLevel = false;
            activePause = false;
            subToResumeFireOnce = true;
            isPaused = false;
            DestroyBBCollider();
            ResetSaberPos();
            ResetFireOnce();
        }

        private void OnLevelRestart(StandardLevelScenesTransitionSetupDataSO unused, LevelCompletionResults unusedResult)
        {
            //Plugin.Log.Debug("Level Restarted");
            OnLevelStart();
        }

        private void OnLevelResume() // level resumed delegate
        {
            StartCoroutine(WaitThenReActivate());
        }
        
        private void CriticalErrorHandler(bool error, int lineNumber = 0, Exception exception = null) // i overdid this but idrc
        {
            int _case = 0;
            if (lineNumber == 0)
            {
                _case = 1;
            } 
            if(exception == null)
            {
                _case = 2;
            }
            if (exception != null && lineNumber != 0)
            {
                _case = 3;
            }
            if (error)
            {
                switch (_case)
                {
                    default:
                        Plugin.Log.Warn("A critical error has been encountered, disabling mod...");
                        break;
                    case 0:
                        Plugin.Log.Warn("A critical error has been encountered, disabling mod...");
                        break;
                    case 1:
                        Plugin.Log.Warn("A critical error has been encountered, disabling mod...");
                        Plugin.Log.Error(exception);
                        break;
                    case 2:
                        Plugin.Log.Warn("A critical error has been encountered around line " + lineNumber + ", disabling mod.");
                        break;
                    case 3:
                        Plugin.Log.Warn("A critical error has been encountered around line " + lineNumber + ", disabling mod.");
                        Plugin.Log.Error(exception);
                        break;

                }
                criticalError = true;
                modEnabled = false;
            }
        }

        public void FlowCoordinatorBackPressed()
        {
            boundingBoxEnabled = false;
            bbFireOnce = true;
            if (activeCubeActive)
                Destroy(activeCubeHolder);
        }

        public static void EnableBB()
        {
            Instance.boundingBoxEnabled = true;
        }

        private void CreateBBCollider()
        {
            Plugin.Log.Debug("Attempting to create scene bb...");
            colliderCubeActive = Instantiate(cubeHolder);
            colliderCubeActive.transform.localScale = bbScale;
            colliderCubeActive.transform.position = new Vector3(0, playerHeight, 0);
            colliderBBactive = true;
            MeshRenderer dasCuuben = colliderCubeActive.GetComponentInChildren<MeshRenderer>();
            
            try
            {
                dasCuuben.enabled = false;
            } catch (Exception exception)
            {
                Plugin.Log.Warn("Gameobject dasCuuben was not found...");
                CriticalErrorHandler(true, 509, exception);
            }
        }

        private void DestroyBBCollider()
        {
            if (colliderBBactive)
            {
                Plugin.Log.Debug("Destroying scene bb...");
                Destroy(colliderCubeActive);
                colliderBBactive = false;
                colliderBBFireOnce = true;
                RemoveColliderScript();
            }
        }

        private void AttatchColliderScript()
        {
            Plugin.Log.Debug("Attatching collider script to the sabers...");
            try
            {
                //bbColliderScript = colliderCubeActive.AddComponent<BBCollider>();
                rSaberCollider = rSaber.gameObject.AddComponent<BBCollider>();
                lSaberCollider = lSaber.gameObject.AddComponent<BBCollider>();
            } catch (Exception exception)
            {
                CriticalErrorHandler(true, 601, exception);
            }
            
        }
        private void RemoveColliderScript()
        {
            Plugin.Log.Debug("Removing collider scripts...");
            Destroy(rSaberCollider);
            Destroy(lSaberCollider);
        }

        private void ResetSaberPos()
        {
            rSaberPos = Vector3.zero;
            lSaberPos = Vector3.zero;
            prevRSaberPos = Vector3.zero;
            prevLSaberPos = Vector3.zero;
            rSaberRot = Quaternion.identity;
            lSaberRot = Quaternion.identity;
            prevRSaberRot = Quaternion.identity;
            prevLSaberRot = Quaternion.identity;
        }

        public enum SaberType
        {
            RightSaber,
            LeftSaber
        }

        public static void SabersLeftBB(SaberType whichSaber)
        {
            if (whichSaber == SaberType.RightSaber)
            {
                Plugin.Log.Warn("Right saber has left the bounding box.");
                Instance.Pause();
                Instance.SetText("Drift detected on right controler.", Color.blue);
                
            }
            if (whichSaber == SaberType.LeftSaber)
            {
                Plugin.Log.Warn("Left saber has left the bounding box.");
                Instance.Pause();
                Instance.SetText("Drift detected on right controler.", Color.red);
            }
        }

        private void ResetFireOnce()
        {
            attatchColliderScriptFireOnce = false;
        }

        private void Diagnose()
        {
            Refresh();
            Plugin.Log.Debug("Active pause: " + activePause);
            Plugin.Log.Debug("Is Level: " + isLevel);
            Plugin.Log.Debug("Bounding box enabled: " + boundingBoxEnabled);
            Plugin.Log.Debug("Is pause: " + isPaused);
            Plugin.Log.Debug("Player Height: " + playerHeight);
            Plugin.Log.Debug("Critical Error: " + criticalError);
        }

        private void SetText(string message, Color color) // doesnt work???
        {
            Plugin.Log.Debug("Updating the text...");
            TextObj.indicatorTMPText.text = message;
            TextObj.indicatorTMPText.color = color;
            StartCoroutine(ResetText());
        }

        private IEnumerator ResetText()
        {
            yield return new WaitForSeconds(10);
            Plugin.Log.Debug("Resettinng text...");
            TextObj.indicatorTMPText.text = "";
        }

        public static void ExternalCriticalError(string ScriptLocation = null, int lineNumber = 0, Exception exception = null)
        {
            Plugin.Log.Warn("A critical error has been encountered in " + ScriptLocation +  ". ");
            Instance.CriticalErrorHandler(true, lineNumber, exception);
        }
    }
}
