// ---------------------------------
// HiddenParts.cs
// Is a component, should not be applied manually
// Stores the parts that should currently be hidden
// ---------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using UnityEngine;
using UnityEngine.PlayerLoop;

#if BEPINEX
using BepInEx.Logging;
#endif

namespace MoreHeadUtilities
{
    public class HiddenParts : MonoBehaviour
    {

#if BEPINEX
        public static ManualLogSource Log;

        public static void Init(ManualLogSource source)
        {
            Log = source;
            Log.LogInfo("LoggerUtil initialized!");
        }
#endif


        // Parts that can be hidden
        // Head->Hips  
        public enum Part
        {
            Health = 0,
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg,
            EyeLeft,
            EyeRight,
            Head,
            Neck,
            Body,
            Hips,
            LeftPupil,
            RightPupil,
        };

        // Mesh names to be hidden when cosmetic is enabled  
        private string[][] partNames = new string[][]
        {
               new string[] { "mesh_health", "mesh_health frame", "mesh_health shadow" },   // Health                       
               new string[] { "mesh_arm_l" },                                               // Arm Left                       
               new string[] { "mesh_arm_r" },                                               // Arm Right                       
               new string[] { "mesh_leg_l" },                                               // Leg Left                       
               new string[] { "mesh_leg_r" },                                               // Leg Right                       
               new string[] { "mesh_eye_l" },                                               // Eye Left                       
               new string[] { "mesh_eye_r" },                                               // Eye Right                       
               new string[] { "mesh_head_top" },                                            // Head                       
               new string[] { "mesh_head_bot_sphere", "mesh_head_bot_flat"},                // Neck                       
               new string[] { "mesh_body_top_sphere", "mesh_body_top_flat" },               // Body                       
               new string[] { "mesh_body_bot" },                                            // Hips                       
               new string[] { "mesh_pupil_l" },                                             // Left Pupil
               new string[] { "mesh_pupil_r" },                                             // Right Pupil
        };

        private Part[][] childParts = new Part[][]
        {
               new Part[] {  },                                                             // Health         
               new Part[] {  },                                                             // Arm Left       
               new Part[] {  },                                                             // Arm Right      
               new Part[] {  },                                                             // Leg Left       
               new Part[] {  },                                                             // Leg Right      
               new Part[] { Part.LeftPupil },                                               // Eye Left       
               new Part[] { Part.RightPupil },                                              // Eye Right      
               new Part[] { Part.EyeLeft, Part.EyeRight },                                  // Head           
               new Part[] { Part.Head, Part.Health },                                       // Neck           
               new Part[] { Part.Neck, Part.LeftArm, Part.RightArm },                       // Body           
               new Part[] { Part.Body, Part.LeftLeg, Part.RightLeg },                       // Hips           
               new Part[] {  },                                                             // Left Pupil
               new Part[] {  },                                                             // Right Pupil
        };

        private List<Part> hiddenParts = new List<Part>();

        public void AddHiddenPart(Part part, bool hideChildren, bool update = true)
        {
#if BEPINEX
            Log?.LogInfo($"Adding part {part}");  
#else
            Debug.Log($"Adding part {part}");
#endif

            hiddenParts.Add(part);

            if (hideChildren)
            {
                foreach (var childPart in childParts[(int)part])
                {
                    AddHiddenPart(childPart, hideChildren, false);
                }
            }

            if (update)
            {
                UpdateHiddenParts();
            }
        }

        public void RemoveHiddenPart(Part part, bool hideChildren, bool update = true)
        {
            // Remove the part from the list  
            if (hiddenParts.Contains(part))
            {
                hiddenParts.Remove(part);
            }

            // Remove the children from the list  
            if (hideChildren)
            {
                foreach (var childPart in childParts[(int)part])
                {
                    RemoveHiddenPart(childPart, hideChildren, false);
                }
            }

            if (update)
            {
                UpdateHiddenParts();
            }
        }

        public void UpdateHiddenParts()
        {
#if BEPINEX
            Log?.LogInfo($"Updating parts, to be hidden: {hiddenParts.ToArray()}");  
#else
            Debug.Log($"Updating parts, to be hidden: {hiddenParts.ToArray()}");
#endif

            // Show all parts first  
            foreach (Part part in Enum.GetValues(typeof(Part)))
            {
                ShowPart(part);
            }

            // Hide the parts that are in the list  
            foreach (var part in hiddenParts)
            {
                HidePart(part);
            }
        }

        // Helper method to search the entire hierarchy  
        private Transform FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindInHierarchy(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void ShowPart(Part partToShow)
        {
            // For each mesh in the part  
            foreach (string partName in partNames[(int)partToShow])
            {
                // Find the transform of the part  
                Transform part = FindInHierarchy(transform, partName);

                if (part == null)
                {
#if BEPINEX
                    Log?.LogInfo($"Part '{partName}' not found");  
#else
                    Debug.Log($"Part '{partName}' not found");
#endif
                    continue;
                }

                MeshRenderer partRenderer = part.gameObject.GetComponent<MeshRenderer>();

                // If the part is found, enable the renderer  
                if (partRenderer)
                {
                    partRenderer.enabled = true;
                }
            }
        }

        private void HidePart(Part partToHide)
        {
            // For each mesh in the part  
            foreach (string partName in partNames[(int)partToHide])
            {
                // Find the transform of the part  
                Transform part = FindInHierarchy(transform, partName);

                if (part == null)
                {
#if BEPINEX
                    Log?.LogInfo($"Part '{partName}' not found");  
#else
                    Debug.Log($"Part '{partName}' not found");
#endif
                    continue;
                }

                MeshRenderer partRenderer = part.gameObject.GetComponent<MeshRenderer>();

                // If the part is found, disable the renderer  
                if (partRenderer)
                {
                    partRenderer.enabled = false;
                }
            }
        }
    }
}