using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MGSC
{
    [CustomEditor(typeof(ActorDescriptor))]
    public class ActorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var actorDescriptor = target as ActorDescriptor;
            
            base.OnInspectorGUI();

            if (GUILayout.Button("Assign bones IDs"))
            {
                AssignBonesIds(actorDescriptor.Creature3DModel);
                AssetDatabase.SaveAssets();
            }
            
            if (GUILayout.Button("Assign Animation Collections"))
            {
                AssignAnimationCollections(actorDescriptor);
                AssetDatabase.SaveAssets();
            }
            
            if (GUILayout.Button("Pack mesh"))
            {
                PackMesh(actorDescriptor);
                AssetDatabase.SaveAssets();
            }
            
            if (GUILayout.Button("Auto-Fill Armor Types"))
            {
                AutoFillArmorTypes(actorDescriptor);
                AssetDatabase.SaveAssets();
                Debug.Log($"Armor types for {actorDescriptor.name} have been auto-filled.");
            }
        }

        public static void AssignBonesIds(GameObject prefabRoot)
        {
            var bones = prefabRoot.GetComponentsInChildren<Bone>(true);
            
            var boneCount = 0;

            foreach (var bone in bones)
            {
                bone.BoneId = bone.gameObject.name;
                boneCount++;
            }
            
            Debug.Log($"Assigned Bone IDs to {boneCount} bones in prefab: {prefabRoot.name}");
            
            EditorUtility.SetDirty(prefabRoot);
        }

        public static void AssignAnimationCollections(ActorDescriptor descriptor)
        {
            descriptor.AnimationSets.Clear();

            var soPath = AssetDatabase.GetAssetPath(descriptor);
            var rootFolder = System.IO.Path.GetDirectoryName(soPath);

            var animationsFolder = $"{rootFolder}/Animations";
            
            if (!AssetDatabase.IsValidFolder(animationsFolder))
            {
                Debug.LogError($"Animations folder not found at: {animationsFolder}");
                return;
            }

            var animationGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationsFolder });

            var gripToClipsMap = new Dictionary<HandsGrip, List<AnimationClipMapping>>();

            foreach (var guid in animationGuids)
            {
                var clipPath = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

                if (!TryParseClipName(clip.name, out var grip, out var animationType))
                {
                    Debug.LogWarning($"Unrecognized animation clip name format: {clip.name}");
                    continue;
                }

                if (!gripToClipsMap.ContainsKey(grip))
                {
                    gripToClipsMap[grip] = new List<AnimationClipMapping>();
                }

                gripToClipsMap[grip].Add(new AnimationClipMapping
                {
                    AnimationType = animationType,
                    Clip = clip
                });
            }

            foreach (var gripPair in gripToClipsMap)
            {
                descriptor.AnimationSets.Add(new AnimationCollection
                {
                    Grip = gripPair.Key,
                    Clips = gripPair.Value
                });
            }

            EditorUtility.SetDirty(descriptor);
        }

        private static AnimationType DetermineAnimationType(string clipName)
        {
            if (clipName.EndsWith("AttackAnim", StringComparison.OrdinalIgnoreCase)) return AnimationType.Attack;
            if (clipName.EndsWith("DeathAnim", StringComparison.OrdinalIgnoreCase)) return AnimationType.Death;
            if (clipName.EndsWith("IdleAnim", StringComparison.OrdinalIgnoreCase)) return AnimationType.Idle;
            if (clipName.EndsWith("ReloadingAnim", StringComparison.OrdinalIgnoreCase)) return AnimationType.Reloading;
            if (clipName.EndsWith("TakeHitAnim", StringComparison.OrdinalIgnoreCase)) return AnimationType.TakeHit;
            if (clipName.EndsWith("WalkAnim", StringComparison.OrdinalIgnoreCase)) return AnimationType.Walk;
            if (clipName.EndsWith("FireAnim", StringComparison.OrdinalIgnoreCase)) return AnimationType.Fire;

            return AnimationType.None;
        }

        private static bool TryParseClipName(string clipName, out HandsGrip grip, out AnimationType animationType)
        {
            grip = HandsGrip.None;
            animationType = AnimationType.None;

            var parts = clipName.Split('_');

            if (parts.Length < 2)
            {
                return false;
            }

            if (!Enum.TryParse(parts[0], true, out grip))
            {
                Debug.LogError($"Invalid HandsGrip value in clip name: {clipName}");
                return false;
            }

            var typeString = parts[^1];
            animationType = DetermineAnimationType(typeString);

            return animationType != AnimationType.None;
        }

        public static void PackMesh(ActorDescriptor descriptor)
        {
            var generator = new ActorMeshGenerator();
            var preset = Resources.Load<CreatureAnimator3dPreset>("CreatureAnimator3dPreset");

            if (preset == null)
            {
                Debug.LogError("Failed to load CreatureAnimator3dPreset. Ensure it exists in the Resources folder.");
                return;
            }

            var packedActorMesh = generator.GenerateActorMesh(preset, descriptor);

            if (packedActorMesh == null)
            {
                Debug.LogError("Failed to generate PackedActorMesh.");
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(descriptor);
            var rootFolder = Path.GetDirectoryName(assetPath);
            var lastFolderName = Path.GetFileName(rootFolder);
            var fileName = $"{lastFolderName}_packed.bytes";
            
            var filePath = Path.Combine("Assets", "Content", "Actors", lastFolderName, fileName);

            try
            {
                using (var fileStream = File.Open(filePath, FileMode.Create))
                using (var writer = new BinaryWriter(fileStream))
                {
                    MeshSerialization.SerializePackedActorMesh(writer, packedActorMesh, 1);
                }

                Debug.Log($"PackedActorMesh saved successfully at: {filePath}");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save PackedActorMesh to {filePath}: {ex.Message}");
            }
        }

        private void AutoFillArmorTypes(ActorDescriptor descriptor)
        {
            var soPath = AssetDatabase.GetAssetPath(descriptor);
            var actorFolder = Path.GetDirectoryName(soPath);
            var armorsFolder = Path.Combine(actorFolder, "Armors");

            if (!Directory.Exists(armorsFolder))
            {
                Debug.LogError($"No Armors folder found for actor: {descriptor.name}");
                return;
            }

            var armorFolders = Directory.GetDirectories(armorsFolder);

            descriptor.ArmorArchTypes.Clear();

            foreach (var armorFolder in armorFolders)
            {
                var armorType = Path.GetFileName(armorFolder);
                var prefabPaths = Directory.GetFiles(armorFolder, "*.prefab", SearchOption.TopDirectoryOnly);

                var prefabs = new List<GameObject>();

                foreach (var prefabPath in prefabPaths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    
                    if (prefab != null)
                    {
                        prefabs.Add(prefab);
                    }
                }

                if (prefabs.Count > 0)
                {
                    descriptor.ArmorArchTypes.Add(new ArmorArchType
                    {
                        ArmorType = armorType,
                        Prefabs = prefabs
                    });
                }
                else
                {
                    Debug.LogWarning($"No prefabs found in folder: {armorFolder}");
                }
            }
            
            EditorUtility.SetDirty(descriptor);
        }
    }
}