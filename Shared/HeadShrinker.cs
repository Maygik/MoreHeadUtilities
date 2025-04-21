// ---------------------------------
// HeadShrinker.cs
// Is a component, to be applied to parts of the model
// Determines which part should be hidden by this accessory
// Multiple components should be added for multiple parts, if not all children should be hidden
// ---------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UnityEngine;
using UnityEngine.PlayerLoop;
using MoreHeadUtilities;
using Debug = UnityEngine.Debug;

#if BEPINEX
using BepInEx.Logging;
using MoreHeadUtilities.Plugin;
#endif

namespace MoreHeadUtilities
{
    public class PartShrinker : MonoBehaviour
    {
        // The part to hide
        [SerializeField] private HiddenParts.Part partToHide = HiddenParts.Part.LeftPupil;

        // Should the part hide all children as well?
        [SerializeField] private bool hideChildren = true;

        private Transform TotalParent;
        private HiddenParts parentComponent;

        private static double totalSearchingTime = 0;

        private bool isFrameOne = true;
        private bool isFrameTwo = false;

#if BEPINEX
            public static ManualLogSource bepInExLog;

            public static void Init(ManualLogSource source)
            {
                bepInExLog = source;
                bepInExLog.LogInfo("LoggerUtil initialized!");
            }
#endif

        public void Log(string message)
        {
#if BEPINEX
            if (!MoreHeadUtilitiesPlugin._enableDebugLogging.Value)
                return;
            bepInExLog.LogInfo(message);
#else
            Debug.Log(message);
#endif
        }

        public void LogError(string message)
        {
#if BEPINEX
            if (!MoreHeadUtilitiesPlugin._enableDebugLogging.Value)
                return;
            bepInExLog.LogError(message);
#else
            Debug.LogError(message);
#endif
        }

        public void Update()
        {
            if (isFrameOne)
            {
                isFrameOne = false;
                isFrameTwo = true;
            }
            else if (isFrameTwo)
            {
                isFrameTwo = false;

                // Is running when menu first opened
                // Figure out a way to stop that to reduce lag

                Stopwatch stopwatch = Stopwatch.StartNew();

                if (!parentComponent)
                {
                    Log($"{gameObject.name} is finding parent on awakening");

                    TotalParent = transform;
                    while (TotalParent.name != "ANIM BOT" && TotalParent.name != null && TotalParent.name != "WorldDecorationFollower")
                    {
                        TotalParent = TotalParent.parent;
                    }

                    if (TotalParent.name == null)
                    {
                        LogError($"No root found");
                        return;
                    }

                    if (TotalParent.name == "WorldDecorationFollower")
                    {
                        LogError($"{gameObject.name} is set to world parent. MoreHeadUtilities does not support part removal from a world object.");
                        return;
                    }

                    parentComponent = TotalParent.GetComponent<HiddenParts>();
                    if (!parentComponent)
                    {
                        parentComponent = TotalParent.gameObject.AddComponent<HiddenParts>();
                    }
                    else
                    {
                        Log($"Part already exists");
                    }
                }

                parentComponent.AddHiddenPart(partToHide, hideChildren);
            }
        }
        

        void OnDisable()
        {
            if (isFrameOne)
            {
                return;
            }
            else
            {
                isFrameOne = true;
            }

            if (!parentComponent)
            {
                Log($"{gameObject.name} is finding parent for destruction");

                TotalParent = transform;
                while (TotalParent.name != "ANIM BOT" && TotalParent.name != null && TotalParent.name != "WorldDecorationFollower")
                {
                    TotalParent = TotalParent.parent;
                }

                if (TotalParent.name == null)
                {
                    LogError($"No root found");
                    return;
                }

                if (TotalParent.name == "WorldDecorationFollower")
                {
                    LogError($"{gameObject.name} is set to world parent. MoreHeadUtilities does not support part removal from a world object.");
                    return;
                }

                parentComponent = TotalParent.GetComponent<HiddenParts>();
                if (!parentComponent)
                {
                    parentComponent = TotalParent.gameObject.AddComponent<HiddenParts>();
                }
                else
                {
                    Log($"Component already exists");
                }
            }

            parentComponent.RemoveHiddenPart(partToHide, hideChildren);
        }
    }
}
