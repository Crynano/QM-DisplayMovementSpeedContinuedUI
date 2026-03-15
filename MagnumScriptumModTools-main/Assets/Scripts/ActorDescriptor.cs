using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGSC
{
    [Serializable]
    public struct ArmorPartInfo
    {
        public string ArmorType;
        public string ArmorPart;
        public Texture Texture;
    }
    
    [Serializable]
    public struct ArmorArchType
    {
        public string ArmorType;
        public List<GameObject> Prefabs;
    }
    
    [Serializable]
    public struct HairArchType
    {
        public string HairType;
        public GameObject Prefab;
    }
    
    [CreateAssetMenu(fileName = "New Actor", menuName = "Data/Descriptors/Actor")]
    public class ActorDescriptor : ScriptableObject
    {
        [SerializeField] private GameObject _creature3dModel;
        [SerializeField] private List<Texture> _skin;
        [SerializeField] private List<AnimationCollection> _animationSets;
        [SerializeField] private List<ArmorArchType> _armorTypes;
        [SerializeField] private List<HairArchType> _hairTypes;
        
        [SerializeField] private Texture _hairTexture;

        [SerializeField] private Sprite _inventoryDummySprite;
        [SerializeField] private Sprite _inventoryDummyLinesSprite;
        
        [SerializeField] private LiquidType _blood = LiquidType.Red;
        [SerializeField] private bool _putCorpse = true;
        [SerializeField] private bool _putBlood = true;
        [SerializeField] private bool _putBloodCorners;
        [SerializeField] private bool _interactWithBloodTrail = true;
        
        [SerializeField] private float _activityStatusY = 0.28f;
        [SerializeField] private bool _shakeCameraOnMove;
        [SerializeField] private bool _bigShadow;
        
        [SerializeField] private Sprite[] _deathBloodAnimSprites;
        [SerializeField] private int _deathAnimFrameRate = 10;

        [SerializeField] private Vector2 _boxColliderOffset;
        [SerializeField] private Vector2 _boxColliderSize;
        
        public GameObject Creature3DModel => _creature3dModel;
        public List<AnimationCollection> AnimationSets => _animationSets;
        public List<ArmorArchType> ArmorArchTypes => _armorTypes;
        public List<HairArchType> HairArchTypes => _hairTypes;
        public List<Texture> Skin => _skin;

        public Texture HairTexture => _hairTexture;
        
        public Sprite InventoryDummySprite => _inventoryDummySprite;
        public Sprite InventoryDummyLinesSprite => _inventoryDummyLinesSprite;
        
        public LiquidType Blood => _blood;
        public bool PutCorpse => _putCorpse;
        public bool PutBlood => _putBlood;
        public bool PutBloodCorners => _putBloodCorners;
        public bool InteractWithBloodTrail => _interactWithBloodTrail;
        
        public float ActivityStatusY => _activityStatusY;
        public bool ShakeCameraOnMove => _shakeCameraOnMove;
        public bool BigShadow => _bigShadow;
        
        public Sprite[] DeathBloodAnimSprites => _deathBloodAnimSprites;
        public int DeathAnimFrameRate => _deathAnimFrameRate;
        
        public Vector2 BoxColliderOffset => _boxColliderOffset;
        public Vector2 BoxColliderSize => _boxColliderSize;

        // TODO: TEMPORARY WORKAROUND
        public PackedActorMesh LoadedMesh;
    }
}