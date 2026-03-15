using System.Collections.Generic;
using UnityEngine;

namespace MGSC
{
    [CreateAssetMenu(fileName = "New CreatureAnimator3dPreset", menuName = "Data/CreatureAnimator3dPreset")]
    public class CreatureAnimator3dPreset : ScriptableObject
    {
        [Header("Move")]
        public float afterMoveDelay = 0.2f;
        public float endMoveThreshold = 0.2f;
        public float moveDuration = 0.3f;
        
        [Space]
        [Header("Melee Attack")]
        public float meleeAttackDuration = 0.25f;
        public float meleeAttackHitTime = 0.1f;
        public float meleeAttackMaxT = 0.7f;
        public float meleeAttackMoveDelay = 0.05f;
        
        [Space]
        [Header("Ranged Attack")]
        public float twoHandedShootDelay = 0;
        
        [Space]
        [Header("Idle")]
        
        [Space]
        [Header("Taking Damage")]
        public float hittedMaxT = 0.45f;
        public float hittedMaxPosTime = 0.04f;
        public float hittedThrowbackDuration = 0.15f;
        
        [Space]
        [Header("Reloading")]
        
        [Space]
        [Header("Death")]
        
        [Space]
        [Header("Throwback")]
        public float throwbackDuration = 0.08f;
        
        [Space]
        [Header("Animation Keyframes by Type")]
        public List<KeyFramesData> keyFramesList;
    }
}