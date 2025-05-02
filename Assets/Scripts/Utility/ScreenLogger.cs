/* Copyright (C) 2022 Christian Burberry - All Rights Reserved
 * 
 * You may use, distribute and modify this code under the
 * terms of the MIT license.
 */

using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Utility class to print console logs to screen
    /// Useful when running as a Standalone Application.
    /// </summary>
    public class ScreenLogger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Duration the messages live on-screen")]
        private float duration = 2.5f;

        [SerializeField]
        [Tooltip("Show type of log in the log body? e.g. [Log]:")]
        private bool showLogType = false;

        [SerializeField]
        [Tooltip("Normalized screen pivot position for log offset")]
        private Vector2 normalizedPivot = Vector2.zero;

        [SerializeField]
        [Tooltip("Screen position offset (from pivot vector, default top-left)")]
        private Vector2 offset = new Vector2(10f, 10f);

        #region Colors
        [SerializeField]
        [BoxGroup("Output Colors")]
        [Tooltip("Standard log color")]
        private Color log = Color.white;

        [SerializeField]
        [BoxGroup("Output Colors")]
        [Tooltip("Warning log color")]
        private Color warning = Color.yellow;

        [SerializeField]
        [BoxGroup("Output Colors")]
        [Tooltip("Error log color")]
        private Color error = Color.red;

        [SerializeField]
        [BoxGroup("Output Colors")]
        [Tooltip("Assert log color")]
        private Color assert = Color.red;

        [SerializeField]
        [BoxGroup("Output Colors")]
        [Tooltip("Exception log color")]
        private Color exception = Color.red;
        #endregion

        string myLog;
        Queue<TimedLog> myLogQueue = new Queue<TimedLog>();
        bool logQueueLocked = false;

        private static ScreenLogger instance;

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning($"[{nameof(ScreenLogger)}]: Multiple instances detected!");
                Destroy(this);
            }
            else
            {
                instance = this;
            }
        }

        private void Start()
        {
            DontDestroyOnLoad(this);
            StartCoroutine(ProcessLogs());
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            myLog = logString;
            string newString = "\n";

            if (showLogType)
            {
                newString += "[" + type + "]: ";
            }
            newString += myLog;

            //Wrap newString with coloring to match the LogType
            switch (type)
            {

                case LogType.Warning:
                    newString = $"<color=#{ColorUtility.ToHtmlStringRGBA(warning)}>{newString}</color>";
                    break;
                case LogType.Error:
                    newString = $"<color=#{ColorUtility.ToHtmlStringRGBA(error)}>{newString}</color>";
                    break;
                case LogType.Assert:
                    newString = $"<color=#{ColorUtility.ToHtmlStringRGBA(assert)}>{newString}</color>";
                    break;
                case LogType.Exception:
                    newString = $"<color=#{ColorUtility.ToHtmlStringRGBA(exception)}>{newString}\n{stackTrace}</color>";
                    break;
                case LogType.Log:
                default:
                    newString = $"<color=#{ColorUtility.ToHtmlStringRGBA(log)}>{newString}</color>";
                    break;
            }

            //Lock the queue primitively such that it is not concurrently edited by coroutine.
            logQueueLocked = true;
            myLogQueue.Enqueue(new TimedLog(newString, duration));
            UpdateLogFromQueue();
            logQueueLocked = false;
        }

        private IEnumerator ProcessLogs()
        {
            int frameCount = 1;
            while (true)
            {
                if (myLogQueue.Count == 0)
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                }

                var queueAsList = myLogQueue.ToList();
                int i = queueAsList.Count - 1;
                bool isExpired = false;

                //Reverse iterate
                for (; i > -1; --i)
                {
                    var timedLog = queueAsList[i];
                    timedLog.TimeToLive -= (Time.deltaTime * frameCount);
                    if (timedLog.TimeToLive < 0f)
                    {
                        isExpired = true;
                        break;
                    }
                }

                frameCount = 1;
                if (isExpired)
                {
                    //Wait while other handler is using the queue
                    while (logQueueLocked)
                    {
                        yield return new WaitForEndOfFrame();
                        frameCount++;
                    }

                    //Remove elements at and before index
                    queueAsList.RemoveRange(0, i + 1);
                    myLogQueue = new Queue<TimedLog>(queueAsList);
                    UpdateLogFromQueue();
                }
            }
        }

        private void UpdateLogFromQueue()
        {
            myLog = string.Empty;
            foreach (var timedLog in myLogQueue)
            {
                myLog += timedLog.Log;
            }
        }

        private void OnGUI()
        {
            float absolutePivotX = normalizedPivot.x * Screen.width;
            float absolutePivotY = normalizedPivot.y * Screen.height;
            GUILayout.BeginArea(new Rect(absolutePivotX + offset.x, absolutePivotY + offset.y, Screen.width, Screen.height));
            GUIStyle style = new GUIStyle();
            style.richText = true;
            GUILayout.Label(myLog, style);
            GUILayout.EndArea();
        }

        private class TimedLog
        {
            public string Log;
            public float TimeToLive;

            public TimedLog(string log, float ttl)
            {
                Log = log;
                TimeToLive = ttl;
            }
        }
    }

}