using UnityEngine;

namespace MGSC
{
    public struct BoneTransformData
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }
    
    public class Bone : MonoBehaviour
    {
        [SerializeField] private string _boneId;

        public string BoneId
        {
            get => _boneId;
            set => _boneId = value;
        }
    }
}