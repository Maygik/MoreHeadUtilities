// ---------------------------------
// HiddenParts.cs
// Is a component, should not be applied manually
// Stores the parts that should currently be hidden
// ---------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Debug = UnityEngine.Debug;

#if BEPINEX
using BepInEx.Logging;
using MoreHeadUtilities.Plugin;
#endif



namespace MoreHeadUtilities
{
    public class HiddenParts : MonoBehaviour
    {


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
            bepInExLog?.LogInfo(message);
#else
            Debug.Log(message);
#endif
        }

        public void LogError(string message)
        {
#if BEPINEX
            bepInExLog?.LogError(message);
#else
            Debug.LogError(message);
#endif
        }

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

        private MeshRenderer[][] partRenderers = new MeshRenderer[][]
        {
            new MeshRenderer[3],                                                            // Health     
            new MeshRenderer[1],                                                            // Arm Left   
            new MeshRenderer[1],                                                            // Arm Right  
            new MeshRenderer[1],                                                            // Leg Left   
            new MeshRenderer[1],                                                            // Leg Right  
            new MeshRenderer[1],                                                            // Eye Left   
            new MeshRenderer[1],                                                            // Eye Right  
            new MeshRenderer[1],                                                            // Head       
            new MeshRenderer[2],                                                            // Neck       
            new MeshRenderer[2],                                                            // Body       
            new MeshRenderer[1],                                                            // Hips       
            new MeshRenderer[1],                                                            // Left Pupil
            new MeshRenderer[1],                                                            // Right Pupil
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


        private bool updatedThisFrame = false;

        public void Start()
        {
            foreach (Part part in Enum.GetValues(typeof(Part)))
            {
                for (int i = 0; i < partNames[(int)part].Length; ++i)
                {
                    Transform partTransform = FindInHierarchy(transform, partNames[(int)part][i]);

                    if (partTransform == null)
                    {
                        Log($"Part '{partNames[(int)part][i]}' not found");
                        continue;
                    }

                    MeshRenderer partRenderer = partTransform.gameObject.GetComponent<MeshRenderer>();

                    if (partRenderer)
                    {
                        Log($"Found part '{partNames[(int)part][i]}'");
                        partRenderers[(int)part][i] = partRenderer;
                    }
                }
            }
        }

        public void AddHiddenPart(Part part, bool hideChildren, bool update = true)
        {
            Log($"Adding part {part}");

            hiddenParts.Add(part);

            if (hideChildren)
            {
                foreach (var childPart in childParts[(int)part])
                {
                    AddHiddenPart(childPart, hideChildren, false);
                }
            }

            updatedThisFrame = true;
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

            updatedThisFrame = true;
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
        public void LateUpdate()
        {
            // Update the parts if they have changed  
            if (updatedThisFrame)
            {
                UpdateHiddenParts();
                updatedThisFrame = false;
            }
        }


        public void UpdateHiddenParts()
        {
            Log($"Updating parts, to be hidden: {hiddenParts.ToArray()}");

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

        private void ShowPart(Part partToShow)
        {
            // For each mesh in the part
            for (int i = 0; i < partNames[(int)partToShow].Length; ++i)
            {
                MeshRenderer partRenderer = partRenderers[(int)partToShow][i].gameObject.GetComponent<MeshRenderer>();

                // If the part is found, disable the renderer  
                if (partRenderer)
                {
                    partRenderer.enabled = true;
                }
            }
        }

        private void HidePart(Part partToHide)
        {
            // For each mesh in the part
            for (int i = 0; i < partNames[(int)partToHide].Length; ++i)
            {
                MeshRenderer partRenderer = partRenderers[(int)partToHide][i].gameObject.GetComponent<MeshRenderer>();

                // If the part is found, disable the renderer  
                if (partRenderer)
                {
                    partRenderer.enabled = false;
                }
            }
        }
    }
}