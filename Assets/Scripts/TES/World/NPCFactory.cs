﻿using System.Collections.Generic;
using TES3Unity.ESM.Records;
using UnityEngine;

namespace TES3Unity.World
{
    public static class NPCFactory
    {
        public static GameObject InstanciateNPC(NIFManager nifManager, NPC_Record npc)
        {
            var beast = npc.Race == "Argonian" || npc.Race == "Khajiit";
            var female = Utils.ContainsBitFlags((uint)npc.Flags, (uint)NPCFlags.Female);
            var animationFile = npc.Model;

            if (string.IsNullOrEmpty(animationFile))
            {
                animationFile = "xbase_anim";

                if (female)
                {
                    animationFile += "_female";
                }

                if (beast)
                {
                    animationFile = "xbase_animkna";
                }

                animationFile += ".nif";
            }

            var npcObj = nifManager.InstantiateNIF($"meshes\\{animationFile}", false);
            var pelvis = npcObj.transform.Find("Bip01/Bip01 Pelvis");
            pelvis.localPosition = new Vector3(0, -0.268f, 0.009f);

            var boneMapping = new Dictionary<string, Transform>();
            var renderers = npcObj.GetComponentsInChildren<MeshRenderer>(true);
            Transform bone = null;

            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Shadow") || renderer.name.Contains("QuadPatch"))
                {
                    continue;
                }

                bone = renderer.transform.parent;
                boneMapping.Add(bone.name, bone);
            }

            // Load body parts
            var race = npc.Race;
            var gender = female ? "f" : "m";
            var ankle = $"b_n_{race}_{gender}_ankle";
            var foot = $"b_n_{race}_{gender}_foot";
            var forearm = $"b_n_{race}_{gender}_forearm";
            var groin = $"b_n_{race}_{gender}_groin";
            var knee = $"b_n_{race}_{gender}_knee";
            var neck = $"b_n_{race}_{gender}_neck";
            var skins = $"b_n_{race}_{gender}_skins";
            var upperArm = $"b_n_{race}_{gender}_upper arm";
            var upperLeg = $"b_n_{race}_{gender}_upper leg";
            var wrist = $"b_n_{race}_{gender}_wrist";

            AddBodyPart(nifManager, npc.HeadModel, boneMapping["Head"]);
            AddBodyPart(nifManager, npc.HairModel, boneMapping["Head"]);
            AddBodyPart(nifManager, ankle, boneMapping["Left Ankle"], true);
            AddBodyPart(nifManager, ankle, boneMapping["Right Ankle"]);

            // No foot model for that race.
            if (!beast)
            {
                AddBodyPart(nifManager, foot, boneMapping["Left Foot"], true);
                AddBodyPart(nifManager, foot, boneMapping["Right Foot"]);
            }

            AddBodyPart(nifManager, forearm, boneMapping["Left Forearm"], true);
            AddBodyPart(nifManager, forearm, boneMapping["Right Forearm"]);
            AddBodyPart(nifManager, groin, boneMapping["Groin"]);
            AddBodyPart(nifManager, knee, boneMapping["Left Knee"], true);
            AddBodyPart(nifManager, knee, boneMapping["Right Knee"]);
            AddBodyPart(nifManager, neck, boneMapping["Neck"]);
            AddBodyPart(nifManager, upperArm, boneMapping["Left Upper Arm"], true);
            AddBodyPart(nifManager, upperArm, boneMapping["Right Upper Arm"]);
            AddBodyPart(nifManager, upperLeg, boneMapping["Left Upper Leg"], true);
            AddBodyPart(nifManager, upperLeg, boneMapping["Right Upper Leg"]);
            AddBodyPart(nifManager, wrist, boneMapping["Left Wrist"], true);
            AddBodyPart(nifManager, wrist, boneMapping["Right Wrist"]);

            // This part is hacky..
            var chest = AddBodyPart(nifManager, skins, boneMapping["Chest"]);
            if (npc.Race == "Dark Elf")
            {
                if (female)
                {
                    chest.localPosition = new Vector3(-0, -0.03f, 0f);
                }
                else
                {
                    chest.localPosition = new Vector3(0.019f, 0.091f, 0.02f);
                }
            }
            else if (npc.Race == "Wood Elf")
            {
                chest.localPosition = new Vector3(0.014f, 0.092f, 0.014f);
            }
            else
            {
                chest.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
            }

            if (female)
            {
                chest.localRotation = Quaternion.Euler(0.0f, 180.0f, 90.0f);
            }

            if (beast)
            {
                chest.localPosition = new Vector3(-0.018f, -1.081f, 0);
                chest.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else
            {
                var leftHand = chest.Find("Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 L Clavicle/Bip01 L UpperArm/Left Hand");
                InvertXScale(leftHand);
                leftHand.SetParent(boneMapping["Left Hand"], false);

                var rightHand = chest.Find("Bip01 Pelvis/Bip01 Spine/Bip01 Spine1/Bip01 Spine2/Bip01 Neck/Bip01 R Clavicle/Bip01 R UpperArm/Right Hand");
                rightHand.SetParent(boneMapping["Right Hand"], false);
            }

            return npcObj;
        }

        public static Transform AddBodyPart(NIFManager nifManager, string path, Transform parent, bool invertXScale = false)
        {
            var part = nifManager.InstantiateNIF($"meshes\\b\\{path}.NIF", false);
            var partTransform = part.transform;

            if (invertXScale)
            {
                InvertXScale(partTransform);
            }

            partTransform.SetParent(parent, false);

            return partTransform;
        }

        public static void InvertXScale(Transform part)
        {
            var position = part.localPosition;
            var rotation = part.localRotation.eulerAngles;
            var scale = part.localScale;
            part.localPosition = new Vector3(-position.x, position.y, position.z);
            part.localRotation = Quaternion.Euler(rotation.x, rotation.y, -rotation.z);
            part.localScale = new Vector3(scale.x *= -1f, scale.y, scale.z);
        }
    }
}
