using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MGSC
{
    public static class MeshSerialization
    {
        public static void SerializePackedActorMesh(BinaryWriter stream, PackedActorMesh mesh, int version)
        {
            stream.Write(version);
            
            // Step 1: Serialize Bone IDs
            stream.Write(mesh.BonesIds.Count);

            foreach (var boneId in mesh.BonesIds)
            {
                stream.Write(boneId);
            }

            // Step 2: Serialize Grip Animation Collection
            stream.Write(mesh.GripAnimationMeshes.Count);

            foreach (var gripPair in mesh.GripAnimationMeshes)
            {
                stream.Write((int)gripPair.Key);
                stream.Write(gripPair.Value.Count);

                foreach (var animationPair in gripPair.Value)
                {
                    stream.Write((int)animationPair.Key);
                    stream.Write(animationPair.Value.Length);
                    SerializeMeshFrameDataWithDeltas(stream, animationPair.Value);
                }
            }
        }

        public static PackedActorMesh DeserializePackedActorMesh(BinaryReader stream)
        {
            int version = stream.ReadInt32();
            
            var packedActorMesh = new PackedActorMesh();

            // Step 1: Deserialize Bone IDs
            var bonesCount = stream.ReadInt32();

            for (var i = 0; i < bonesCount; i++)
            {
                var boneId = stream.ReadString();
                packedActorMesh.BonesIds.Add(boneId);
            }

            // Step 2: Deserialize Grip Animation Collection
            var gripCount = stream.ReadInt32();

            for (var i = 0; i < gripCount; i++)
            {
                var grip = (HandsGrip)stream.ReadInt32();
                var animationCount = stream.ReadInt32();

                var animationMeshes = new Dictionary<AnimationType, MeshFrameData[]>();

                for (var j = 0; j < animationCount; j++)
                {
                    var animationType = (AnimationType)stream.ReadInt32();
                    var frameCount = stream.ReadInt32();

                    // Step 3: Deserialize Base Mesh
                    var baseMesh = DeserializeMesh(stream);

                    // Step 4: Deserialize Frame Data Using Deltas
                    var frames = DeserializeMeshFrameDataWithDeltas(stream, baseMesh, frameCount);
                    animationMeshes[animationType] = frames;
                }

                packedActorMesh.GripAnimationMeshes[grip] = animationMeshes;
            }

            return packedActorMesh;
        }
        
        private static void SerializeMeshFrameDataWithDeltas(BinaryWriter stream, MeshFrameData[] frameData)
        {
            // Serialize Base Mesh
            SerializeMesh(stream, frameData[0].FrameMesh);

            // Serialize Bone Pos for the Base Mesh
            stream.Write(frameData[0].BonePositions.Count);
            foreach (var bonePair in frameData[0].BonePositions)
            {
                stream.Write(bonePair.Key);
                var boneTransformData = bonePair.Value;

                stream.Write(boneTransformData.Position.x);
                stream.Write(boneTransformData.Position.y);
                stream.Write(boneTransformData.Position.z);

                stream.Write(boneTransformData.Rotation.x);
                stream.Write(boneTransformData.Rotation.y);
                stream.Write(boneTransformData.Rotation.z);
                stream.Write(boneTransformData.Rotation.w);
            }
            
            // Serialize Delta Frames
            for (var i = 1; i < frameData.Length; i++)
            {
                SerializeMeshWithDeltas(stream, frameData[0].FrameMesh, frameData[i].FrameMesh);

                // Serialize Bone Positions
                stream.Write(frameData[i].BonePositions.Count);
                foreach (var bonePair in frameData[i].BonePositions)
                {
                    stream.Write(bonePair.Key);
                    var boneTransformData = bonePair.Value;

                    stream.Write(boneTransformData.Position.x);
                    stream.Write(boneTransformData.Position.y);
                    stream.Write(boneTransformData.Position.z);

                    stream.Write(boneTransformData.Rotation.x);
                    stream.Write(boneTransformData.Rotation.y);
                    stream.Write(boneTransformData.Rotation.z);
                    stream.Write(boneTransformData.Rotation.w);
                }
            }
        }

        private static void SerializeMesh(BinaryWriter stream, Mesh mesh)
        {
            stream.Write(mesh.name);

            var vertices = mesh.vertices;
            stream.Write(vertices.Length);

            foreach (var vertex in vertices)
            {
                stream.Write(vertex.x);
                stream.Write(vertex.y);
                stream.Write(vertex.z);
            }

            var normals = mesh.normals;
            stream.Write(normals.Length);

            foreach (var normal in normals)
            {
                stream.Write(normal.x);
                stream.Write(normal.y);
                stream.Write(normal.z);
            }

            var uvs = mesh.uv;
            stream.Write(uvs.Length);

            foreach (var uv in uvs)
            {
                stream.Write(uv.x);
                stream.Write(uv.y);
            }

            var triangles = mesh.triangles;
            stream.Write(triangles.Length);

            foreach (var triangle in triangles)
            {
                stream.Write(triangle);
            }
        }
        
        private static void SerializeMeshWithDeltas(BinaryWriter stream, Mesh baseMesh, Mesh targetMesh)
        {
            var baseVertices = baseMesh.vertices;
            var targetVertices = targetMesh.vertices;

            if (baseVertices.Length != targetVertices.Length)
            {
                throw new InvalidOperationException("Base mesh and target mesh vertex counts do not match.");
            }

            // Write vertex count (same for all)
            stream.Write(baseVertices.Length);

            // Write deltas
            for (var i = 0; i < baseVertices.Length; i++)
            {
                var delta = targetVertices[i] - baseVertices[i];
                stream.Write(delta.x);
                stream.Write(delta.y);
                stream.Write(delta.z);
            }
        }
        
        private static MeshFrameData[] DeserializeMeshFrameDataWithDeltas(BinaryReader stream, Mesh baseMesh, int frameCount)
        {
            var frameDataArray = new MeshFrameData[frameCount];

            // Deserialize Base Mesh
            frameDataArray[0] = new MeshFrameData
            {
                FrameMesh = baseMesh,
                BonePositions = DeserializeBonePositions(stream)
            };

            // Deserialize Delta Frames
            for (var i = 1; i < frameCount; i++)
            {
                var frameMesh = DeserializeMeshWithDeltas(stream, baseMesh);
                var bonePositions = DeserializeBonePositions(stream);

                frameDataArray[i] = new MeshFrameData
                {
                    FrameMesh = frameMesh,
                    BonePositions = bonePositions
                };
            }

            return frameDataArray;
        }

        private static Mesh DeserializeMesh(BinaryReader stream)
        {
            var name = stream.ReadString();

            var vertexCount = stream.ReadInt32();
            var vertices = new Vector3[vertexCount];

            for (var i = 0; i < vertexCount; i++)
            {
                var x = stream.ReadSingle();
                var y = stream.ReadSingle();
                var z = stream.ReadSingle();
                vertices[i] = new Vector3(x, y, z);
            }

            var normalCount = stream.ReadInt32();
            var normals = new Vector3[normalCount];

            for (var i = 0; i < normalCount; i++)
            {
                var x = stream.ReadSingle();
                var y = stream.ReadSingle();
                var z = stream.ReadSingle();
                normals[i] = new Vector3(x, y, z);
            }

            var uvCount = stream.ReadInt32();
            var uvs = new Vector2[uvCount];

            for (var i = 0; i < uvCount; i++)
            {
                var x = stream.ReadSingle();
                var y = stream.ReadSingle();
                uvs[i] = new Vector2(x, y);
            }

            var triangleCount = stream.ReadInt32();
            var triangles = new int[triangleCount];

            for (var i = 0; i < triangleCount; i++)
            {
                triangles[i] = stream.ReadInt32();
            }

            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };

            return mesh;
        }
        
        private static Mesh DeserializeMeshWithDeltas(BinaryReader stream, Mesh baseMesh)
        {
            var baseVertices = baseMesh.vertices;

            // Vertex count matches
            var vertexCount = stream.ReadInt32();
            
            if (vertexCount != baseVertices.Length)
            {
                throw new InvalidOperationException("Vertex count mismatch between base mesh and delta data.");
            }

            var targetVertices = new Vector3[vertexCount];

            // Read deltas and reconstruct vertices
            for (var i = 0; i < vertexCount; i++)
            {
                var delta = new Vector3(
                    stream.ReadSingle(),
                    stream.ReadSingle(),
                    stream.ReadSingle()
                );

                targetVertices[i] = baseVertices[i] + delta;
            }

            var targetMesh = new Mesh
            {
                vertices = targetVertices,
                // normals are unchanged???
                normals = baseMesh.normals,
                // UVs are unchanged???
                uv = baseMesh.uv,
                // Triangles are static
                triangles = baseMesh.triangles
            };

            return targetMesh;
        }
        
        private static Dictionary<string, BoneTransformData> DeserializeBonePositions(BinaryReader stream)
        {
            var boneCount = stream.ReadInt32();
            var bonePositions = new Dictionary<string, BoneTransformData>();

            for (var i = 0; i < boneCount; i++)
            {
                var boneId = stream.ReadString();
                var position = new Vector3(
                    stream.ReadSingle(),
                    stream.ReadSingle(),
                    stream.ReadSingle()
                );
                var rotation = new Quaternion(
                    stream.ReadSingle(),
                    stream.ReadSingle(),
                    stream.ReadSingle(),
                    stream.ReadSingle()
                );

                bonePositions[boneId] = new BoneTransformData
                {
                    Position = position,
                    Rotation = rotation
                };
            }

            return bonePositions;
        }
    }
}