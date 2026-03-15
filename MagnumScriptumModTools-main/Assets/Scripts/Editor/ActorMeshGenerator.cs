using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MGSC
{
    public class ActorMeshGenerator
    {
        private readonly Dictionary<HandsGrip, Dictionary<AnimationType, AnimationClip>> _animationClips = new();
        private PackedActorMesh _packedMesh;
        private CreatureAnimator3dPreset _preset;
        private GameObject _tempInstance;
        private ActorDescriptor _descriptor;
        
        public PackedActorMesh GenerateActorMesh(CreatureAnimator3dPreset preset, ActorDescriptor descriptor)
        {
            _packedMesh = new PackedActorMesh();
            _preset = preset;
            _descriptor = descriptor;
            
            var startTime = DateTime.Now;
            
            InitializeTempInstance();
            InitClips();
            InitializeBones();
            PopulateFrames();
            CleanupTempInstance();
            
            var delta = DateTime.Now - startTime;
            
            Debug.Log($"Actor generated, process took {delta.TotalSeconds:0.00} seconds.");
            
            return _packedMesh;
        }
        
        private void InitClips()
        {
            _animationClips.Clear();

            foreach (var collection in _descriptor.AnimationSets)
            {
                if (!_animationClips.ContainsKey(collection.Grip))
                {
                    _animationClips[collection.Grip] = new Dictionary<AnimationType, AnimationClip>();
                }
                
                foreach (var clipMapping in collection.Clips)
                {
                    if (_animationClips[collection.Grip].ContainsKey(clipMapping.AnimationType))
                    {
                        Debug.LogWarning($"Duplicate animation type {clipMapping.AnimationType} found in grip {collection.Grip}. Skipping.");
                        continue;
                    }
                    
                    _animationClips[collection.Grip][clipMapping.AnimationType] = clipMapping.Clip;
                }
            }
        }
        
        private void InitializeBones()
        {
            var childBones = _descriptor.Creature3DModel.GetComponentsInChildren<Bone>();

            _packedMesh.BonesIds = new List<string>(childBones.Length);

            foreach (var bone in childBones)
            {
                if (string.IsNullOrEmpty(bone.BoneId))
                {
                    Debug.LogError($"Bone {bone.name} is missing a valid BoneId.");
                    continue;
                }

                _packedMesh.BonesIds.Add(bone.BoneId);
            }
        }

        
        private void PopulateFrames()
        {
            foreach (var gripAnimations in _animationClips)
            {
                var grip = gripAnimations.Key;
                
                if (!_packedMesh.GripAnimationMeshes.ContainsKey(grip))
                {
                    _packedMesh.GripAnimationMeshes[grip] = new Dictionary<AnimationType, MeshFrameData[]>();
                }

                foreach (var animationPair in gripAnimations.Value)
                {
                    var animationType = animationPair.Key;
                    var clip = animationPair.Value;

                    if (clip == null)
                    {
                        Debug.LogError($"Animation clip for {animationType} in grip {grip} is null.");
                        continue;
                    }

                    var keyFramesData = _preset.keyFramesList.Find(k => k.AnimationType == animationType);
                    
                    if (keyFramesData.Keyframes == null || keyFramesData.Keyframes.Length == 0)
                    {
                        Debug.LogError($"No keyframes data found for animation type {animationType} in grip {grip}.");
                        continue;
                    }

                    var frameDataArray = new MeshFrameData[keyFramesData.Keyframes.Length];

                    for (var i = 0; i < keyFramesData.Keyframes.Length; i++)
                    {
                        frameDataArray[i] = GenerateMeshForFrame(clip, keyFramesData.Keyframes[i]);
                    }

                    _packedMesh.GripAnimationMeshes[grip][animationType] = frameDataArray;
                }
            }
        }

        private MeshFrameData GenerateMeshForFrame(AnimationClip clip, int frameIndex)
        {
            var frameTime = frameIndex / clip.frameRate;

            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(_tempInstance, clip, frameTime);

                var meshFilters = _tempInstance.GetComponentsInChildren<MeshFilter>();
                var combineInstances = new CombineInstance[meshFilters.Length];
                var bonePositions = new Dictionary<string, BoneTransformData>();

                foreach (var bone in _tempInstance.GetComponentsInChildren<Bone>())
                {
                    if (!string.IsNullOrEmpty(bone.BoneId))
                    {
                        var transformData = new BoneTransformData
                        {
                            Position = bone.transform.position,
                            Rotation = bone.transform.rotation
                        };

                        bonePositions[bone.BoneId] = transformData;
                    }
                }

                for (var i = 0; i < meshFilters.Length; i++)
                {
                    var meshFilter = meshFilters[i];
                    combineInstances[i].mesh = meshFilter.sharedMesh;
                    combineInstances[i].transform = meshFilter.transform.localToWorldMatrix;
                }

                Mesh combinedMesh = new();
                combinedMesh.CombineMeshes(combineInstances, true, true);
                
                return new MeshFrameData
                {
                    FrameMesh = combinedMesh,
                    BonePositions = bonePositions
                };
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }
        
        private void InitializeTempInstance()
        {
            _tempInstance = Object.Instantiate(_descriptor.Creature3DModel);
        }

        private void CleanupTempInstance()
        {
            Object.DestroyImmediate(_tempInstance);
            _tempInstance = null;
        }
    }
}