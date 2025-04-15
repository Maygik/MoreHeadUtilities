// ---------------------------------
// HeadShrinker.cs
// Is a component, to be applied to parts of the model
// Determines which part should be hidden by this accessory
// Multiple components should be added for multiple parts, if not all children should be hidden
// ---------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Configuration;
using UnityEngine;
using UnityEngine.PlayerLoop;

#if BEPINEX
using BepInEx.Logging;
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

        void OnEnable()
        {
            // Is running when menu first opened
            // Figure out a way to stop that to reduce lag

            if (!parentComponent)
            {
#if BEPINEX
                    Log.LogInfo("[PartShrinker] OnEnable called.");
#else
                Debug.Log("[PartShrinker] OnEnable called.");
#endif

                TotalParent = transform;
                while (TotalParent.name != "ANIM BOT" && TotalParent.name != null && TotalParent.name != "WorldDecorationFollower")
                {
                    TotalParent = TotalParent.parent;
                }

                if (TotalParent.name == null)
                {
#if BEPINEX
                    Log.LogError($"No root found");
#else
                    Debug.LogError($"No root found");
#endif
                    return;
                }

                if (TotalParent.name == "WorldDecorationFollower")
                {
#if BEPINEX
                    Log.LogInfo($"MoreHeadUtilities does not support part removal from a world object");
#else
                    Debug.Log($"MoreHeadUtilities does not support part removal from a world object");
#endif
                    return;
                }

                parentComponent = TotalParent.GetComponent<HiddenParts>();
                if (!parentComponent)
                {
                    parentComponent = TotalParent.gameObject.AddComponent<HiddenParts>();
                }
                else
                {
#if BEPINEX
                    Log.LogInfo($"Component already exists");
#else
                    Debug.Log($"Component already exists");
#endif
                }
            }

            parentComponent.AddHiddenPart(partToHide, hideChildren);
        }

#if BEPINEX
            public static ManualLogSource Log;

            public static void Init(ManualLogSource source)
            {
                Log = source;
                Log.LogInfo("LoggerUtil initialized!");
            }
#endif

        void OnDisable()
        {
            if (!parentComponent)
            {
#if BEPINEX
                Log.LogInfo("OnDisable called.");
#else
                Debug.Log("[PartShrinker] OnDisable called.");
#endif

                TotalParent = transform;
                while (TotalParent.name != "ANIM BOT" && TotalParent.name != null && TotalParent.name != "WorldDecorationFollower")
                {
                    TotalParent = TotalParent.parent;
                }

                if (TotalParent.name == null)
                {
#if BEPINEX
                    Log.LogError($"No root found");
#else
                    Debug.LogError($"No root found");
#endif
                    return;
                }

                if (TotalParent.name == "WorldDecorationFollower")
                {
#if BEPINEX
                    Log.LogInfo($"MoreHeadUtilities does not support part removal from a world object");
#else
                    Debug.Log($"MoreHeadUtilities does not support part removal from a world object");
#endif
                    return;
                }

                parentComponent = TotalParent.GetComponent<HiddenParts>();
                if (!parentComponent)
                {
                    parentComponent = TotalParent.gameObject.AddComponent<HiddenParts>();
                }
                else
                {
#if BEPINEX
                    Log.LogInfo($"Component already exists");
#else
                    Debug.Log($"Component already exists");
#endif
                }
            }

            parentComponent.RemoveHiddenPart(partToHide, hideChildren);
        }
    }
}
