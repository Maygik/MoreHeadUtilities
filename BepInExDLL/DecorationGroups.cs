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
using System.Linq;using System;
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
    }

    class HeadDecorationManagerStorage
    {
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

            public static void Patch2_CreateAllDecorationButtons(REPOPopupPage page)
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

                // 2) build your list
                var builtInDecorations = allDecorations
                    // cast Invoke(...) back to bool so Where gets a bool
                    .Where(decoration => (bool)isBuiltInMI.Invoke(
                        null,
                        new object[] { decoration }
                    ))
                    // —or— simply use the delegate:
                    //.Where(isBuiltIn)

                    // Order by the DecorationInfo itself, not its Name
                    .OrderBy(decoration =>
                        HeadDecorationManagerStorage.Decorations[allDecorations.IndexOf(decoration)]
                    )
                    .ThenBy(decoration => decoration.DisplayName)
                    .ToList();

                Logger.Log($"builtInDecorations sorted");

                var externalDecorations = allDecorations
                    .Where(decoration => !(bool)isBuiltInMI.Invoke(
                        null,
                        new object[] { decoration }
                    ))
                    .OrderBy(decoration =>
                        HeadDecorationManagerStorage.Decorations[allDecorations.IndexOf(decoration)]
                    )
                    .ThenBy(decoration => decoration.DisplayName)
                    .ToList();

                Logger.Log($"externalDecorations sorted");

                var CreateDecorationButtonMI = AccessTools.Method(
                    typeof(MoreHeadUI),
                    "CreateDecorationButton",
                    new Type[] { typeof(REPOPopupPage), typeof(DecorationInfo) }
                )!;

                foreach (var decoration in builtInDecorations)
                {
                    Logger.Log($"Creating built-in decoration");
                    CreateDecorationButtonMI.Invoke(null, new object[] {page, decoration});
                }

                foreach (var decoration in externalDecorations)
                {
                    Logger.Log($"Creating external decoration");
                    CreateDecorationButtonMI.Invoke(null, new object[] { page, decoration });
                }

                Logger.Log($"externalDecorations all created");

                MoreHeadUIStorage.group = null;

                // Return from ORIGINAL function, not this patch
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
                nameof(MoreHeadUIHelpers.Patch2_CreateAllDecorationButtons),
                new[] { typeof(REPOPopupPage)}
            )!;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instrs, ILGenerator il)
        {
            var codes = instrs.ToList();
            bool injected1 = false;
            int toListHits = 0;
            bool injected2 = false;

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                // -------- Inject #1 after tagScrollViewElements.Clear() --------
                if (!injected1
                    && ci.opcode == OpCodes.Callvirt
                    && ci.operand == ClearScrolls)
                {
                    Logger.Log($"Patching Patch1_CreateAllDecorationButtons");
                    // insert your first helper immediately after Clear()
                    codes.Insert(++i, new CodeInstruction(OpCodes.Call, Helper1));
                    injected1 = true;
                    continue;
                }

                // -------- Inject #2 after the *second* .ToList() call --------
                if (!injected2
                    && ci.opcode == OpCodes.Call
                    && ci.operand == ToListDecorations)
                {
                    toListHits++;
                    // skip the first ToList(), apply on the second
                    if (toListHits == 2)
                    {
                        Logger.Log("Patching Patch2_CreateAllDecorationButtons");

                        // 1) Find the index of the *last* 'ret' in the method's IL
                        int retIndex = codes.FindLastIndex(ci => ci.opcode == OpCodes.Ret);
                        if (retIndex < 0)
                            throw new InvalidOperationException("Couldn't find the final ret in CreateAllDecorationButtons");

                        var retInst = codes[retIndex];
                        var leaveLabel = il.DefineLabel();
                        // Attach the label to the real 'ret' instruction.
                        retInst.labels.Add(leaveLabel);

                        // 3) Inject your helper call
                        codes.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));          // push 'page'
                        codes.Insert(++i, new CodeInstruction(OpCodes.Call, Helper2));   // call Helper2(page)

                        // 4) Branch (Leave) to that final ret, properly unwinding any try/finally
                        codes.Insert(++i, new CodeInstruction(OpCodes.Leave_S, leaveLabel));

                        Logger.Log("Patched Patch2_CreateAllDecorationButtons");

                        // 5) Done—break so we keep the rest of the original IL (handlers & final epilogue)
                        break;
                    }
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
            Logger.Log($"CreateDecorationButton Prefix called");

            string? decoGroup =
                HeadDecorationManagerStorage.Decorations[HeadDecorationManager.Decorations.IndexOf(decoration)];

            if (decoGroup != null && decoGroup != "")
            {
                if (MoreHeadGroupStorage.tagGroupElements.TryGetValue("ALL", out var groupElements))
                {
                    if (!MoreHeadGroupStorage.activeGroups.ContainsKey(decoGroup))
                    {
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
            Logger.Log($"Accessing currentTagFilter");
            var currentTagFilterField = moreHeadUIType.GetField("LIMB_TAGS", bindingFlags);
            string[] LIMB_TAGS = (string[])currentTagFilterField.GetValue(null);

            
            // Add a group tag for the scroll element
            MoreHeadGroupStorage.tagGroupElements["ALL"].Add(decoGroup);

            // 处理四肢装饰物的特殊情况
            if (LIMB_TAGS.Contains(decoration.ParentTag.ToUpper()))
            {
                // 同时添加到LIMBS标签分类
                MoreHeadGroupStorage.tagGroupElements["LIMBS"].Add(decoGroup);
            }
            // 同时添加到父标签分类
            else
            {
                if (MoreHeadGroupStorage.tagGroupElements.TryGetValue(decoration.ParentTag.ToUpper(), out var elements))
                {
                    elements.Add(decoGroup);
                }

            }

            return true; // Return true to continue execution of the original method
        }

        

        // Create a title for the group
        private static void CreateGroupButton(REPOPopupPage page, string groupName)
        {
            Logger.Log($"Creating group button: {groupName}");
            try
            {
                // I'm not going to figure this out right now
                //string buttonText = $"<size=20>{(activeGroups[groupName] ? "<color=#777777>[+]</color>" : "<color=#CCCCCC>[-]</color>")} {groupName}</size>";
                string buttonText = $"<size=20>-{groupName}-</size>";

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

                MoreHeadUIStorage.group = groupName;
                Logger.Log($"Created group button: {groupName}");
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

            MoreHeadGroupStorage.activeGroups[groupName] = !MoreHeadGroupStorage.activeGroups[groupName];

            Logger.Log($"Invoking show tag decorations");


            var MoreHeadUIType = typeof(MoreHeadUI);
            var currentTagFilterField = MoreHeadUIType.GetField("currentTagFilter", BindingFlags.Static | BindingFlags.NonPublic);
            string currentTagFilter = (string)currentTagFilterField.GetValue(null);

            MoreHeadUIStorage.resetPosition = false;

            ShowTagDecorationsMI.Invoke(
                null,
                new object[] { currentTagFilter }
            );

            MoreHeadUIStorage.resetPosition = true;
        }
    }


    [HarmonyPatch(typeof(HeadDecorationManager))]
    [HarmonyPatch("LoadDecorationBundle", new[] { typeof(string) })]
    static class Patch_LoadDecorationBundle
    {

        static readonly MethodInfo EnsureUniqueDisplayName = AccessTools.Method(
                typeof(HeadDecorationManager),
                "EnsureUniqueDisplayName",
                new [] {typeof(string)}
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

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instrs)
        {
            var codes = instrs.ToList();

            bool foundEnsureUniqueName = false;
            bool foundEnsureUniqueDisplayName = false;

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
                    i+=2; // skip the next two instructions
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

                if (foundEnsureUniqueDisplayName && foundEnsureUniqueName)
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
    [HarmonyPatch("ShowTagDecorations", new [] {typeof(string)})]
    static class Patch_ShowTagDecorations
    {
        [HarmonyPrefix]
        static bool Prefix(string tag)
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



                Logger.Log($"Running the proper ShowTagDecorations function");

                // 隐藏当前标签的装饰物按钮
                List<REPOScrollViewElement> elements;
                List<string?> groups;
                tagScrollViewElements.TryGetValue(currentTagFilter, out elements);
                MoreHeadGroupStorage.tagGroupElements.TryGetValue(currentTagFilter, out groups);

                Logger.Log($"There are {elements.Count()} scroll elements and {groups.Count()} grouped elements");

                if (!string.IsNullOrEmpty(currentTagFilter))
                {
                    for (int i = 0; i < elements.Count(); ++i)
                    {
                        if (elements[i] != null)
                        {
                            elements[i].visibility = false;
                        }
                    }
                }

                Logger.Log($"Element visibility hidden");

                // 显示新标签的装饰物按钮
                tagScrollViewElements.TryGetValue(tag, out elements);
                MoreHeadGroupStorage.tagGroupElements.TryGetValue(tag, out groups);

                Logger.Log($"There are {elements.Count()} tagged scroll elements and {groups.Count()} tagged grouped elements");

                if (!string.IsNullOrEmpty(tag))
                {
                    for (int i = 0; i < elements.Count(); ++i)
                    {
                        if (groups[i] == null)
                        {
                            if (elements[i] != null)
                            {
                                elements[i].visibility = true;
                            }
                        }
                        else
                        {
                            if (elements[i] != null && MoreHeadGroupStorage.activeGroups[groups[i]])
                            {
                                elements[i].visibility = true;
                            }
                        }
                    }
                }

                // 更新当前标签
                currentTagFilterField.SetValue(null, tag);

                if (MoreHeadUIStorage.resetPosition)
                {
                    decorationsPage.scrollView.SetScrollPosition(0);
                }

                decorationsPage.scrollView.UpdateElements();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error showing decorations for tag: {tag} | Error: {e.Message}");
            }

            return false; // Continue execution of the original method.
        }

    }
}
