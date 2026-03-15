using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGSC
{
    public enum HandsGrip
    {
        None = 0,
        BareHands = 1,
        MeleeOneHanded = 2,
        MeleeTwoHanded = 4,
        Rifle = 8,
        Pistol = 16,
        Heavy = 32,
        BareHandAndShield = 64,
        MeleeAndShield = 128,
        PistolAndShield = 256,
    }

    [Serializable]
    public struct AnimationCollection
    {
        public HandsGrip Grip;
        public List<AnimationClipMapping> Clips;
    }
    
    [Serializable]
    public struct AnimationClipMapping
    {
        public AnimationType AnimationType;
        public AnimationClip Clip;
    }
    
    [Serializable]
    public struct KeyFramesData
    {
        public AnimationType AnimationType;
        public int Framerate;
        public bool IsLooped;
        public int[] Keyframes;
    }

    public enum AnimationType
    {
        None,
        Attack,
        Death,
        Fire,
        Idle,
        Reloading,
        TakeHit,
        Walk
    }
}