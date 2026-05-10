#region Assembly MenuLib, Version=2.5.1.0, Culture=neutral, PublicKeyToken=null
// C:\Users\xande\AppData\Roaming\Thunderstore Mod Manager\DataFolder\REPO\profiles\Moddedleeldeded\BepInEx\plugins\nickklmao-MenuLib\MenuLib.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion


using System;
using MenuLib.Structs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using System;
using BepInEx.Logging;
using MenuLib;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using MenuLib.MonoBehaviors;
using MenuLib.Structs;
using BepInEx.Configuration;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using BepInEx.Logging;
using MenuLib;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using MenuLib.MonoBehaviors;
using MenuLib.Structs;
using BepInEx.Configuration;
using System.Xml.Linq;

using HarmonyLib;
using UnityEngine;
using MenuLib;

using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Logger = BepInEx.Logging.Logger;
using MoreHead.MoreHead.Patchers;
using BepInEx;

using MenuLib.MonoBehaviors;
using MenuLib.Structs;

namespace MoreHead
{
    static class Logger
    {
        public static ManualLogSource? log = null;
        public static void Init(ManualLogSource logger)
        {
            log = logger;
        }
        public static void Log(string message)
        {
            log?.LogInfo(message);
        }
        public static void LogError(string message)
        {
            log?.LogError(message);
        }
    }

    static class MoreHeadGroupStorage
    {
        // Dictionary of tags and the group that each element in that tag belongs to
        public static Dictionary<string?, List<string?>> tagGroupElements = new();

        public static Dictionary<string, bool> activeGroups = new();


    }

    static class MoreHeadUIStorage
    {
        [CanBeNull] public static REPOPopupPage page = null;

        [CanBeNull] public static string group = null;

        public static bool resetPosition = true;

        public static Dictionary<string?, REPOButton> groupButtons = new();
        public static Dictionary<string?, List<string>> groupButtonTags = new();
    }

    class HeadDecorationManagerStorage
    {
        // List of all groups
        public static List<string?> Decorations = new List<string?>();
    }

    namespace MoreHead.Patchers
    {
        public static class HeadDecorationManagerHelpers
        {

            public static string bundleName = null;

            public static void Patch1_LoadDecorationBundle(string bundleBaseName)
            {
                // now you _have_ the value — store it wherever you like:
                Logger.Log($"Saving bundle name");
                bundleName = bundleBaseName;
            }

            public static void Patch2_LoadDecorationBundle()
            {
                Logger.Log($"Patch_LoadDecorationBundle called");

                var decorationManagerType = typeof(HeadDecorationManager);
                const BindingFlags F = BindingFlags.Static | BindingFlags.NonPublic;

                // Check if the bundleBaseName contains "~" and split it
                string? group = null;

                if (bundleName.Contains("~"))
                {
                    string[] parts = bundleName.Split('~');
                    if (parts.Length >= 2)
                    {
                        // sadge ^1 syntax doesn't work :(
                        group = parts[parts.Length - 1].ToLower();
                        Logger.Log($"Setting group: {group}");
                    }
                }

                Logger.Log($"Adding decoration");
                HeadDecorationManagerStorage.Decorations.Add(group);
            }


            public static void Patch3_LoadDecorationBundle()
            {
                // Remove last input if asset bundle was never fully loaded
                HeadDecorationManagerStorage.Decorations.RemoveAt(HeadDecorationManagerStorage.Decorations.Count - 1);
                Logger.Log($"Removing last from decoration group list");
            }
        }

        public static class MoreHeadUIHelpers
        {
            public static void Patch1_CreateAllDecorationButtons()
            {
                Logger.Log($"Starting patch of CreateAllDecorationButtons");
                // grab the private static fields via reflection
                Logger.Log("Accessing type MoreHeadUI");
                var uiType = typeof(MoreHeadUI);

                Logger.Log("Setting binding flags to Static and NonPublic");
                const BindingFlags F = BindingFlags.Static | BindingFlags.NonPublic;

                Logger.Log("Retrieving ALL_TAGS field from MoreHeadUI");
                var tags = (string[])uiType.GetField("ALL_TAGS", F).GetValue(null)!;

                foreach (string tag in tags)
                {
                    MoreHeadGroupStorage.tagGroupElements[tag] = new List<string>();
                }
            }

            public static List<DecorationInfo> Patch2_CreateAllDecorationButtons()
            {
                try
                {
                    Logger.Log($"Starting Patch2_CreateAllDecorationButtons");
                    // grab the private static fields via reflection  
                    var uiType = typeof(MoreHeadUI);
                    const BindingFlags F = BindingFlags.Static | BindingFlags.NonPublic;

                    var tags = (string[])uiType.GetField("ALL_TAGS", F).GetValue(null)!;


                    var allDecorations = HeadDecorationManager.Decorations.ToList();

                    Logger.Log($"Gotten IsBuiltInDecoration");

                    var isBuiltInMI = AccessTools.Method(
                        typeof(MoreHeadUI),
                        "IsBuiltInDecoration",
                        new Type[] { typeof(DecorationInfo) }
                    )!;

                    // Ensure the method signature matches the delegate type
                    if (isBuiltInMI.GetParameters().Length == 1 && isBuiltInMI.ReturnType == typeof(bool))
                    {
                        var isBuiltIn = (Func<DecorationInfo, bool>)Delegate.CreateDelegate(
                            typeof(Func<DecorationInfo, bool>),
                            isBuiltInMI
                        );
                        Logger.Log("Delegate successfully created for IsBuiltInDecoration.");
                    }
                    else
                    {
                        throw new InvalidOperationException("IsBuiltInDecoration method signature does not match Func<DecorationInfo, bool>.");
                    }

                    Logger.Log($"Bound Delegate");

                    var sortedDecorations = allDecorations
                        .OrderBy(decoration =>
                            HeadDecorationManagerStorage.Decorations[allDecorations.IndexOf(decoration)] ?? string.Empty
                        )
                        .ThenByDescending(decoration => decoration.IsVisible)
                        .ThenBy(decoration => (bool)isBuiltInMI.Invoke(null, new object[] { decoration }) ? 0 : 1)
                        .ThenBy(decoration => decoration.DisplayName)
                        .ToList();

                    Logger.Log($"decorations sorted");

                    return sortedDecorations;
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error creating all decoration buttons: {e}");
                    throw;
                }
            }

            private static bool _inHelper = false;

            public static void Patch1_CreateDecorationButton(DecorationInfo decoration, string group)
            {
                if (_inHelper)
                    return;
                _inHelper = true;
                try
                {
                    //MoreHeadGroupStorage.tagGroupElements[decoration.ParentTag].Add(group);
                }
                catch (Exception e)
                {
                    Logger.Log("------------------------------------------");
                    Logger.LogError($"Issue with creating decoration button: {e}");
                    Logger.Log("------------------------------------------");
                }
                finally
                {
                    _inHelper = false;
                }
            }
        }
    }



    // Patch button creation
    // This will add the new tags to the tagScrollViewElements and tagGroupElements lists
    [HarmonyPatch(typeof(MoreHeadUI))]
    [HarmonyPatch("CreateAllDecorationButtons", new[] { typeof(REPOPopupPage) })]
    static class Patch_CreateAllDecorationButtons
    {
        // 1) The Clear() on tagScrollViewElements
        static readonly MethodInfo ClearScrolls =
            AccessTools.Method(
                typeof(Dictionary<string, List<REPOScrollViewElement>>),
                nameof(Dictionary<string, List<REPOScrollViewElement>>.Clear)
            )!;

        // 2) The Enumerable.ToList<DecorationInfo>() call
        static readonly MethodInfo ToListDecorations =
            AccessTools
              .Method(typeof(Enumerable), nameof(Enumerable.ToList))!
              .MakeGenericMethod(typeof(DecorationInfo));

        // your two helpers:
        static readonly MethodInfo Helper1 =
            AccessTools.Method(
                typeof(MoreHeadUIHelpers),
                nameof(MoreHeadUIHelpers.Patch1_CreateAllDecorationButtons)
            )!;
        static readonly MethodInfo Helper2 =
            AccessTools.Method(
                typeof(MoreHeadUIHelpers),
                nameof(MoreHeadUIHelpers.Patch2_CreateAllDecorationButtons)
            )!;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instrs, ILGenerator il)
        {
            var codes = instrs.ToList();
            bool injected1 = false;
            bool injected2 = false;

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                // -------- Inject #1 after tagScrollViewElements.Clear() --------
                if (!injected1
                    && ci.opcode == OpCodes.Callvirt        // Make sure it's a callvirt, not a call (Clear() is an instance method, not static)
                    && ci.operand is MethodInfo method1     // Make sure it's a callvirt to Clear()
                    && method1.Equals(ClearScrolls))        // Make sure it's the Clear() on tagScrollViewElements, not some other Clear() call
                {
                    Logger.Log($"Patching Patch1_CreateAllDecorationButtons");
                    // insert your first helper immediately after Clear()
                    codes.Insert(++i, new CodeInstruction(OpCodes.Call, Helper1));
                    injected1 = true;

                    Logger.Log($"Patched Patch1_CreateAllDecorationButtons");

                    continue;
                }

                // -------- Inject #2 after the *second* .ToList() call --------
                if (!injected2
                    && ci.opcode == OpCodes.Call            // Make sure it's a call, not callvirt (ToList() is a static method)
                    && ci.operand is MethodInfo method2     // Make sure it's a call to ToList()
                    && method2.Equals(ToListDecorations))   // Make sure it's the ToList() call on the decorations list, not some other ToList() call (there are several in this method)
                {
                    Logger.Log("Patching Patch2_CreateAllDecorationButtons");

                    // Store the return value, call helper, then restore it
                    // Stack before: [ ..., List<DecorationInfo> ]
                    var localVar = il.DeclareLocal(typeof(List<DecorationInfo>));

                    codes.Insert(++i, new CodeInstruction(OpCodes.Pop));
                    codes.Insert(++i, new CodeInstruction(OpCodes.Call, Helper2));


                    Logger.Log("Patched Patch2_CreateAllDecorationButtons");

                    injected2 = true;
                    i += 3; // skip inserted instructions
                }

                if (injected1 && injected2)
                {
                    break; // stop iterating once we've injected both helpers
                }
            }

            return codes;
        }
    }



    [HarmonyPatch(typeof(MoreHeadUI))]
    [HarmonyPatch("CreateAllDecorationButtons", new[] { typeof(REPOPopupPage) })]
    static class Prefix_CreateAllDecorationButtons
    {
        [HarmonyPrefix]
        static bool Prefix(REPOPopupPage page)
        {
            MoreHeadUIStorage.page = page;

            MoreHeadUIStorage.groupButtons.Clear();
            MoreHeadUIStorage.groupButtonTags.Clear();

            return true; // Continue execution of the original method.
        }
    }

    [HarmonyPatch(typeof(MoreHeadUI))]
    [HarmonyPatch("CreateDecorationButton", new[] { typeof(REPOPopupPage), typeof(DecorationInfo) })]
    static class Patch_CreateDecorationButton
    {
        [HarmonyPrefix]
        static bool Prefix(REPOPopupPage page, DecorationInfo decoration)
        {
            try
            {
                string? decoGroup =
                    HeadDecorationManagerStorage.Decorations[HeadDecorationManager.Decorations.IndexOf(decoration)];

                // Create group button if it doesn't exist
                if (decoGroup != null && decoGroup != "")
                {
                    if (MoreHeadGroupStorage.tagGroupElements.TryGetValue("ALL", out var groupElements))
                    {
                        if (!MoreHeadGroupStorage.activeGroups.ContainsKey(decoGroup))
                        {
                            Logger.Log($"Initialising group: {decoGroup}");
                            MoreHeadGroupStorage.activeGroups[decoGroup] = false;
                        }

                        if (!groupElements.Contains(decoGroup))
                        {
                            CreateGroupButton(page, decoGroup);
                        }
                    }
                }


                // Get the type of MoreHeadUI
                var moreHeadUIType = typeof(MoreHeadUI);

                const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                // Use reflection to access the decorationsPage field

                var currentTagFilterField = moreHeadUIType.GetField("LIMB_TAGS", bindingFlags);
                string[] LIMB_TAGS = (string[])currentTagFilterField.GetValue(null);


                // Add a group tag for the scroll element
                MoreHeadGroupStorage.tagGroupElements["ALL"].Add(decoGroup);

                if (decoGroup != null)
                {
                    if (!MoreHeadUIStorage.groupButtonTags.ContainsKey(decoGroup))
                    {
                        Logger.Log($"Initialising group button tag list for {decoGroup}");
                        MoreHeadUIStorage.groupButtonTags[decoGroup] = new List<string>();
                    }

                    MoreHeadUIStorage.groupButtonTags[decoGroup].Add("ALL");
                }

                // 处理四肢装饰物的特殊情况
                if (LIMB_TAGS.Contains(decoration.ParentTag.ToUpper()))
                {
                    // 同时添加到LIMBS标签分类
                    MoreHeadGroupStorage.tagGroupElements["LIMBS"].Add(decoGroup);

                    if (decoGroup != null)
                    {
                        MoreHeadUIStorage.groupButtonTags[decoGroup].Add("LIMBS");
                    }
                }
                // 同时添加到父标签分类
                else
                {
                    if (MoreHeadGroupStorage.tagGroupElements.TryGetValue(decoration.ParentTag.ToUpper(), out var elements))
                    {
                        elements.Add(decoGroup);

                        if (decoGroup != null)
                        {
                            MoreHeadUIStorage.groupButtonTags[decoGroup].Add(decoration.ParentTag.ToUpper());
                        }
                    }

                }

                return true; // Return true to continue execution of the original method
            }
            catch (Exception e)
            {
                Logger.LogError($"Error creating decoration button {decoration.Name}: {e}");
                throw;
            }
        }

        private static string GetGroupButtonText(string group)
        {
            return MoreHeadGroupStorage.activeGroups[group] ? $"<size=20><color=#777777>[-]{group}</color></size>" : $"<size=20><color=#CCCCCC>[+]{group}</color></size>";
        }

        // Create a title for the group
        private static void CreateGroupButton(REPOPopupPage page, string groupName)
        {
         //   Logger.Log($"Creating group button: {groupName}");
            try
            {
                // I'm not going to figure this out right now
                //string buttonText = $"<size=20>{(activeGroups[groupName] ? "<color=#777777>[+]</color>" : "<color=#CCCCCC>[-]</color>")} {groupName}</size>";
                string buttonText = GetGroupButtonText(groupName);

                // 创建按钮
                REPOButton? repoButton = null;

                page.AddElementToScrollView(scrollView => {
                    repoButton = MenuAPI.CreateREPOButton(
                        buttonText,
                        () => OnDecorationGroupButtonClick(groupName),
                        scrollView
                    );

                    return repoButton.rectTransform;
                });

                if (repoButton != null)
                {
                    MoreHeadUIStorage.groupButtons[groupName] = repoButton;
                    MoreHeadUIStorage.groupButtonTags[groupName] = new List<string>();
                }
                else
                {
                    throw new Exception($"Failed to create group button.");
                }

                MoreHeadUIStorage.group = groupName;
                //Logger.Log($"Created group button: {groupName}");
            }
            catch (Exception e)
            {
                Logger.Log($"Error creating group button: {e.Message}");
            }
        }

        static readonly MethodInfo ShowTagDecorationsMI = AccessTools.Method(
            typeof(MoreHeadUI),
            "ShowTagDecorations",
            new[] { typeof(string) }
        );

        private static void OnDecorationGroupButtonClick(string? groupName)
        {
            Logger.Log($"OnDecorationGroupButtonClick called for group: {groupName}");


            var MoreHeadUIType = typeof(MoreHeadUI);
            var currentTagFilterField = MoreHeadUIType.GetField("currentTagFilter", BindingFlags.Static | BindingFlags.NonPublic);
            string currentTagFilter = (string)currentTagFilterField.GetValue(null);

            REPOButton? groupButton = null;
            MoreHeadUIStorage.groupButtons.TryGetValue(groupName, out groupButton);

            // Get the position of the button before toggling the group
            RectTransform? anchor = groupButton?.rectTransform;
            float beforeY = anchor != null ? anchor.position.y : 0F;

            MoreHeadGroupStorage.activeGroups[groupName] = !MoreHeadGroupStorage.activeGroups[groupName];

            MoreHeadUIStorage.resetPosition = false; // Don't reset scroll position this time

            // Re-show the current tag to update visibility of decorations based on the new group state
            ShowTagDecorationsMI.Invoke(
                null,
                new object[] { currentTagFilter }
            );

            // Update the button text
            if (groupButton != null)
            {
                string buttonText = GetGroupButtonText(groupName);
                groupButton.labelTMP.text = buttonText;
            }

            // Adjust scroll position to keep the same elements in view after toggling the group
            if (MoreHeadUIStorage.page != null && anchor != null)
            {
                UnityEngine.MonoBehaviour.FindObjectOfType<MonoBehaviour>()?.StartCoroutine(
                    RestoreScrollAfterGroupToggle(MoreHeadUIStorage.page, anchor, beforeY)
                );
            }

            MoreHeadUIStorage.resetPosition = true; // We want to reset position for the next time, just not this time
        }


        // Keep scroll position stable when toggling a group
        private static System.Collections.IEnumerator RestoreScrollAfterGroupToggle(REPOPopupPage page, RectTransform anchor, float beforeY)
        {
            yield return null; // Wait one frame for the layout to update. Kinda hacky.

            // Update layout to get the new position of the anchor
            Canvas.ForceUpdateCanvases();
            page.scrollView.UpdateElements();

            // Calculate the change in position of the anchor
            float afterY = anchor.position.y;
            float deltaY = beforeY - afterY;

            // Adjust the scroll position by the change in anchor position to keep it stable
            var scrollRect = page.scrollView.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null && scrollRect.content != null)
            {
                Vector2 pos = scrollRect.content.anchoredPosition;
                pos.y += deltaY;
                scrollRect.content.anchoredPosition = pos;
            }

            // Update layout again to ensure everything is in the correct position
            Canvas.ForceUpdateCanvases();
            page.scrollView.UpdateElements();
        }
    }


    [HarmonyPatch(typeof(HeadDecorationManager))]
    [HarmonyPatch("LoadDecorationBundle", new[] { typeof(string) })]
    static class Patch_LoadDecorationBundle
    {

        static readonly MethodInfo EnsureUniqueDisplayName = AccessTools.Method(
                typeof(HeadDecorationManager),
                "EnsureUniqueDisplayName",
                new[] { typeof(string) }
            );

        static readonly MethodInfo EnsureUniqueName = AccessTools.Method(
            typeof(HeadDecorationManager),
            "EnsureUniqueName",
            new[] { typeof(string) }
        );

        static readonly MethodInfo AddDecorationHelper1 =
            AccessTools.Method(
                typeof(HeadDecorationManagerHelpers),
                nameof(HeadDecorationManagerHelpers.Patch1_LoadDecorationBundle),
                new[] { typeof(string) }
            )!;

        static readonly MethodInfo AddDecorationHelper2 =
            AccessTools.Method(
                typeof(HeadDecorationManagerHelpers),
                nameof(HeadDecorationManagerHelpers.Patch2_LoadDecorationBundle)
            )!;

        static readonly MethodInfo AddDecorationHelper3 =
            AccessTools.Method(
                typeof(HeadDecorationManagerHelpers),
                nameof(HeadDecorationManagerHelpers.Patch3_LoadDecorationBundle)
            )!;


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instrs)
        {
            var codes = instrs.ToList();

            bool foundEnsureUniqueName = false;
            bool foundEnsureUniqueDisplayName = false;
            bool foundBlacklistCheck = false;

            for (int i = 0; i < codes.Count(); ++i)
            {
                var ci = codes[i];
                // find the call to EnsureUniqueDisplayName(string)

                if (ci.opcode == OpCodes.Call && ci.operand == EnsureUniqueDisplayName)
                {
                    // insert before it a Dup + call to our helper
                    //   stack before: [ ... , displayName ]
                    //   Dup      -> [ ... , displayName, displayName ]
                    //   Call     -> [ ... , displayName ]  (helper consumes one copy)

                    Logger.Log($"Patching AddDecorationHelper2");
                    codes.Insert(i, new CodeInstruction(OpCodes.Call, AddDecorationHelper2));
                    // Return

                    Logger.Log($"Patched AddDecorationHelper2");

                    foundEnsureUniqueDisplayName = true;
                    i += 2; // skip the next two instructions
                }

                if (ci.opcode == OpCodes.Call && ci.operand == EnsureUniqueName)
                {

                    Logger.Log($"Patching AddDecorationHelper1");
                    codes.Insert(i, new CodeInstruction(OpCodes.Dup));
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AddDecorationHelper1));
                    Logger.Log($"Patched AddDecorationHelper1");

                    foundEnsureUniqueName = true;
                    i += 2; // skip the next two instructions
                }

                if (!foundBlacklistCheck)
                {
                    if (codes[i].opcode == OpCodes.Call
                        && codes[i].operand == AccessTools.Method(
                            typeof(DecorationBlacklistManager),
                            nameof(DecorationBlacklistManager.IsBlacklisted),
                            new[] { typeof(string) }))
                    {
                        // i+2 is the branch that jumps *over* the true-branch when return == false
                        // so the *true*-branch starts at i+3
                        int insertPos = i + 4;

                        // **Before we can use displayName, we have to reload it onto the stack:**

                        Logger.Log($"Patching AddDecorationHelper3");

                        codes.Insert(insertPos++, new CodeInstruction(OpCodes.Call, AddDecorationHelper3));

                        Logger.Log($"Patched AddDecorationHelper3");

                        foundBlacklistCheck = true;

                        i += 5; // skip the next five instructions
                    }
                }

                if (foundEnsureUniqueDisplayName && foundEnsureUniqueName && foundBlacklistCheck)
                {
                    break;
                }
            }

            return codes;
        }

    }
    [HarmonyPatch(typeof(MoreHeadUI))]
    [HarmonyPatch("CreateDecorationButton", new[] { typeof(REPOPopupPage), typeof(DecorationInfo) })]
    static class Transpiler_CreateDecorationButton
    {
        static readonly MethodInfo PatchMethod = AccessTools.Method(
            typeof(MoreHeadUIHelpers),
            nameof(MoreHeadUIHelpers.Patch1_CreateDecorationButton),
            new[] { typeof(DecorationInfo), typeof(string) }
        );

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instrs)
        {
            var codes = instrs.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                // Inject after tagScrollViewElements[tag].Add(scrollViewElement)
                if (ci.opcode == OpCodes.Callvirt && ci.operand is MethodInfo methodInfo && methodInfo.Name == "Add" && methodInfo.DeclaringType == typeof(List<REPOScrollViewElement>))
                {
                    Logger.Log("Patch adding group");
                    // Insert custom logic after the Add call
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_1)); // Load the first argument (decoration)  

                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldstr, MoreHeadUIStorage.group ?? "")); // Insert a string
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Call, PatchMethod)); // Call the custom method  
                    i += 3; // Skip the inserted instructions  
                }
            }

            return codes;
        }
    }



    [HarmonyPatch(typeof(MoreHeadUI))]
    [HarmonyPatch("UpdateDecorationVisibility")]
    static class Patch_UpdateDecorationVisibility
    {
        [HarmonyPrefix]
        static bool Prefix()
        {
            try
            {
                // Get the type of MoreHeadUI
                var moreHeadUIType = typeof(MoreHeadUI);

                const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

                // Use reflection to access the decorationsPage field
                Logger.Log($"Accessing decorationsPage");
                var decorationsPageField = moreHeadUIType.GetField("decorationsPage", bindingFlags);
                REPOPopupPage? decorationsPage = (REPOPopupPage?)decorationsPageField.GetValue(null);

                // Use reflection to access the tagScrollViewElements field
                Logger.Log($"Accessing tagScrollViewElements");
                var tagScrollViewElementsField = moreHeadUIType.GetField("tagScrollViewElements", bindingFlags);
                Dictionary<string, List<REPOScrollViewElement>> tagScrollViewElements = (Dictionary<string, List<REPOScrollViewElement>>)tagScrollViewElementsField.GetValue(null);

                // Use reflection to access the decorationsPage field
                Logger.Log($"Accessing currentTagFilter");
                var currentTagFilterField = moreHeadUIType.GetField("currentTagFilter", bindingFlags);
                string currentTagFilter = (string)currentTagFilterField.GetValue(null);


                // Use reflection to access the decorationsPage currentSearchQuery field
                var currentSearchQuieryField = moreHeadUIType.GetField("currentSearchQuery", bindingFlags);
                string currentSearchQuery = "";
                try
                {
                    currentSearchQuery = (string)currentSearchQuieryField.GetValue(null);
                }
                catch
                {

                }


                // Use reflection to access the decorationDataCache field
                var decorationDataCacheField = moreHeadUIType.GetField("decorationDataCache", bindingFlags);
                Dictionary<string, List<DecorationInfo>> decorationDataCache = (Dictionary<string, List<DecorationInfo>>)decorationDataCacheField.GetValue(null);

                // Use reflection to access the decorationButtons field
                var decorationButtonsField = moreHeadUIType.GetField("decorationButtons", bindingFlags);
                Dictionary<string, REPOButton> decorationButtons = (Dictionary<string, REPOButton>)decorationButtonsField.GetValue(null);

                // Run replacement method
                ShowDecorationsForTag(decorationsPage, tagScrollViewElements, currentTagFilter, currentSearchQuery, decorationDataCache, decorationButtons);

                return false; // Skip execution of the original method.
            }
            catch (Exception e)
            {
                Logger.LogError($"Error updating visibility");
            }

            return true; // Continue execution of the original method in case of error.
        }

        // Full replacement of the original method
        // Mostly copy-pasted from main MoreHead repo
        // https://github.com/Masaicker/repo-MoreHead/blob/main/MoreHead/MoreHeadUI.cs
        static void ShowDecorationsForTag(REPOPopupPage decorationsPage,
            Dictionary<string, List<REPOScrollViewElement>> tagScrollViewElements,
            string currentTagFilter,
            string currentSearchQuery,
            Dictionary<string, List<DecorationInfo>> decorationDataCache,
            Dictionary<string, REPOButton> decorationButtons
            )
        {
            try
            {
                if (decorationsPage == null || string.IsNullOrEmpty(currentTagFilter))
                    return;

                if (!tagScrollViewElements.TryGetValue(currentTagFilter, out var elements))
                    return;

                if (!decorationDataCache.TryGetValue(currentTagFilter, out var decorations))
                    return;

                // Hide all decorations, then selectively enable them based on search and tag filters
                foreach (var kvp in tagScrollViewElements)
                {
                    foreach (var element in kvp.Value)
                    {
                        if (element != null)
                        {
                            element.visibility = false;
                        }
                    }
                }

                bool isSearchEmpty = string.IsNullOrEmpty(currentSearchQuery);

                Logger.Log($"Showing decorations for tag: {currentTagFilter} with search query: '{currentSearchQuery}' (isSearchEmpty: {isSearchEmpty})");

                // If no tag filter is applied, show all decorations
                if (isSearchEmpty)
                {
                    Logger.Log($"No search query, showing all decorations for tag: {currentTagFilter}");
                    foreach (var element in elements)
                    {
                        if (element != null)
                        {
                            element.visibility = true;

                            // Find the decoration name by looking through decorationButtons
                            string? decorationName = decorationButtons
                                .FirstOrDefault(kvp => kvp.Value?.repoScrollViewElement == element)
                                .Key;

                            // If we found the decoration name, find its group and set visibility based on group
                            if (!string.IsNullOrEmpty(decorationName)) // Decoration has a name
                            {
                                int decoIndex = HeadDecorationManager.Decorations.FindIndex(d => d.Name == decorationName); // Find the index of the decoration in the main list to get its group
                                if (decoIndex >= 0) // If we found the decoration in the main list
                                {
                                    string? decoGroup = HeadDecorationManagerStorage.Decorations[decoIndex]; // Get the group of the decoration
                                    if (decoGroup != null && MoreHeadGroupStorage.activeGroups.ContainsKey(decoGroup)) // If the group is valid
                                    {
                                        Logger.Log($"Setting visibility of decoration '{decorationName}' in group '{decoGroup}' to {MoreHeadGroupStorage.activeGroups[decoGroup]}");
                                        element.visibility = MoreHeadGroupStorage.activeGroups[decoGroup];
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Logger.Log($"Search query present, filtering decorations for tag: {currentTagFilter} with search query: '{currentSearchQuery}'");

                    // Non-empty search: only show decorations matching the current tag that also match the search query
                    string searchNormalized = currentSearchQuery.Replace(" ", "").ToLowerInvariant();

                    foreach (var decoration in decorations)
                    {
                        if (decorationButtons.TryGetValue(decoration.Name ?? string.Empty, out REPOButton button) &&
                            button != null &&
                            elements.Contains(button.repoScrollViewElement))  // 兼容分组功能
                        {
                            var displayNameNormalized = decoration.DisplayName?.Replace(" ", "").ToLowerInvariant() ?? "";
                            button.repoScrollViewElement.visibility = displayNameNormalized.Contains(searchNormalized);
                        }
                    }
                }


                // Grouped Elements

                // Only show group buttons if there is no search query
                if (isSearchEmpty)
                {
                    Logger.Log($"Showing groups for tag filter: {currentTagFilter}");

                    for (int i = 0; i < MoreHeadUIStorage.groupButtons.Count(); ++i)
                    {
                        string groupName = MoreHeadUIStorage.groupButtons.ElementAt(i).Key;
                        MoreHeadUIStorage.groupButtonTags.TryGetValue(groupName, out var groupTags);

                        bool shouldShow = (currentTagFilter == "ALL" || groupTags.Contains(currentTagFilter));

                        if (shouldShow)
                        {
                            Logger.Log($"Showing group for tag : {groupName}");
                            MoreHeadUIStorage.groupButtons[groupName].repoScrollViewElement.visibility = true;
                        }
                        else
                        {
                            MoreHeadUIStorage.groupButtons[groupName].repoScrollViewElement.visibility = false;
                        }

                    }
                }
                // Otherwise, only show decorations matching the search query
                else
                {
                    // Hide all group buttons
                    for (int i = 0; i < MoreHeadUIStorage.groupButtons.Count(); ++i)
                    {
                        string groupName = MoreHeadUIStorage.groupButtons.ElementAt(i).Key;
                        MoreHeadUIStorage.groupButtons[groupName].repoScrollViewElement.visibility = false;
                    }
                }


                // Finally, set scroll position
                // If group opened/closed, don't reset position, adjust to keep current view
                if (MoreHeadUIStorage.resetPosition)
                {
                    decorationsPage.scrollView.SetScrollPosition(0);
                    decorationsPage.scrollView.UpdateElements();
                }
                else
                {
                    // Just update elements to reflect visibility changes
                    // Can't seem to find a better way to keep position otherwise
                    decorationsPage.scrollView.UpdateElements();
                }

                
            }
            catch (Exception e)
            {
                Logger.LogError($"Error in ShowDecorationsForTag: {e.Message}");
            }
        }
    }

}

/*

            try
            {
                Logger.Log($"Running the proper ShowTagDecorations function");

                // 隐藏当前标签的装饰物按钮
                List<REPOScrollViewElement> elements;
                List<string?> groups;
                tagScrollViewElements.TryGetValue(currentTagFilter, out elements);
                MoreHeadGroupStorage.tagGroupElements.TryGetValue(currentTagFilter, out groups);

                Logger.Log($"There are {elements.Count()} scroll elements and {groups.Count()} grouped elements");

                if (currentSearchQuery == null)
                {
                    currentSearchQuery = "";
                }

                for (int i = 0; i < elements.Count(); ++i)
                {
                    if (elements[i] != null)
                    {
                        elements[i].visibility = elements[i].tag == currentTagFilter && elements[i].name.Contains(currentSearchQuery);
                    }
                }

                Logger.Log($"Elements hidden for tag filter: {currentTagFilter}");

                // Only use groups if there is not a search query
                if (currentSearchQuery.IsNullOrWhiteSpace())
                {
                    Logger.Log($"Showing groups for tag filter: {currentTagFilter}");
                    for (int i = 0; i < groups.Count(); ++i)
                    {
                        if (groups[i] != null)
                        {
                            // Check if the group contains the current tag filter
                            bool isActive = MoreHeadGroupStorage.activeGroups[groups[i]];
                            Logger.Log($"Group {groups[i]} is active: {isActive}");

                            MoreHeadUIStorage.groupButtonTags.TryGetValue(groups[i], out var groupTags);
                            

                            if (isActive && (currentTagFilter == "ALL" || groupTags.Contains(currentTagFilter)))
                            {
                                Logger.Log($"Showing group for tag : {groups[i]}");
                                MoreHeadUIStorage.groupButtons[groups[i]].repoScrollViewElement.visibility = true;
                            }
                            else
                            {
                                MoreHeadUIStorage.groupButtons[groups[i]].repoScrollViewElement.visibility = false;

                            }
                        }
                    }
                }
                else
                {
                    Logger.Log($"Hiding all groups due to search query");
                    for (int i = 0; i < groups.Count(); ++i)
                    {
                        if (groups[i] != null)
                        {
                            MoreHeadUIStorage.groupButtons[groups[i]].repoScrollViewElement.visibility = true;
                        }
                    }
                }


                    Logger.Log($"Elements and groups hidden for tag filter: {currentTagFilter}");

                // Show decorations for active groups
                List<string?> elementGroups;
                MoreHeadGroupStorage.tagGroupElements.TryGetValue(currentTagFilter, out elementGroups);


                for (int i = 0; i < elementGroups.Count(); ++i)
                {
                    if (elementGroups[i] == null)
                        continue;

                    bool isActive = MoreHeadGroupStorage.activeGroups[elementGroups[i]];

                    if (isActive && elementGroups[i].Contains(currentSearchQuery))
                    {
                        Logger.Log($"Showing element for group : {elementGroups[i]}");
                        elements[i].visibility = true;
                    }
                }



                Logger.Log($"All elements updated for tag filter: {currentTagFilter}");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error in ShowDecorationsForTag: {e}");
            }
        }
*/