using UnityEngine;

namespace MGSC
{
    public class ItemBone : MonoBehaviour
    {
        [SerializeField] private string _targetBoneId;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private Vector3 _rotation;
        [SerializeField] private Vector3 _scale;

        public string TargetBoneId
        {
            get => _targetBoneId;
            set => _targetBoneId = value;
        }

        public Vector3 Scale
        {
            get => _scale;
            set => _scale = value;
        }

        public Vector3 Offset => _offset;
        public Vector3 Rotation => _rotation;
        
        public void AttachToBone(Transform targetBone)
        {
            if (targetBone == null)
            {
                Debug.LogWarning("Target bone is null.");
                return;
            }

            var targetTransform = targetBone.transform;
            
            /*
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                Debug.LogError($"Cannot attach {gameObject.name} because it is still part of a prefab asset.");
                return;
            }
            */

            transform.SetParent(targetTransform, false);
            transform.localScale = Scale;
            transform.localRotation = Quaternion.identity;
        }

        private void Awake()
        {
            transform.localScale = Scale;
            transform.localRotation = Quaternion.identity;
        }
    }
}