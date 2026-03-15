using System.Collections.Generic;
using UnityEngine;

namespace MGSC
{
    public struct MeshFrameData
    {
        public Mesh FrameMesh;
        public Dictionary<string, BoneTransformData> BonePositions;
    }

    public class PackedActorMesh
    {
        public List<string> BonesIds = new();
        public Dictionary<HandsGrip, Dictionary<AnimationType, MeshFrameData[]>> GripAnimationMeshes = new();
    }
}