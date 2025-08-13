using MGSC;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QM_DisplayMovementSpeedContinuedUI
{
    [UIView(GameLoopGroup.Dungeon, false, false)]
    public class DisplayMovementController : MonoBehaviour
    {
        private static readonly int DamagedHealthbarColor = Shader.PropertyToID("_DamagedHealthbarColor");
        private static readonly int CurrentHealthbarColor = Shader.PropertyToID("_CurrentHealthbarColor");
        private static readonly int EnableCurrentHealthBlink = Shader.PropertyToID("_EnableCurrentHealthBlink");
        private static readonly int CurrentHealthBlinkingSpeed = Shader.PropertyToID("_CurrentHealthBlinkingSpeed");
        private static readonly int ChunkColor = Shader.PropertyToID("_ChunkColor");
        private static readonly int CurrentHealthPercent = Shader.PropertyToID("_CurrentHealthPercent");
        private static readonly int DamagedHealthPercent = Shader.PropertyToID("_DamagedHealthPercent");
        private static readonly int ChunkAmount = Shader.PropertyToID("_ChunkAmount");
        private static readonly int CurrentHealthBlink = Shader.PropertyToID("_EnableCurrentHealthBlink");
        private static readonly int EnableChunk = Shader.PropertyToID("_EnableChunk");
        private static readonly int EnableDamagePreview = Shader.PropertyToID("_EnableDamagePreview");

        public Vector3 adjustment = new Vector3(0f, 0.15f, 0f);

        public TextMeshProUGUI apTextObject;
        public TextMeshProUGUI numericHealthText;
        public Image attackTypeImage;
        public Image damageTypeImage;
        public Image healthBar;

        [Header("Images")] private Sprite _meleeSprite;
        private Sprite _rangedSprite;
        private Sprite _defaultSprite;

        // All damage sprites
        private Dictionary<string, Sprite> DamageSprites;

        private Monster _lastMonster;

        private RectTransform _canvas;

        private CreatureData _playerData;

        private Material hpMaterial;

        private List<string> _damageTypes = new List<string>
            { "blunt", "pierce", "lacer", "fire", "cold", "poison", "shock", "beam" };

        public void Awake()
        {
            LoadComponents();
        }

        public void LoadComponents()
        {
            _canvas = this.gameObject.GetComponentInParent<Canvas>(true).transform as RectTransform;

            apTextObject = this.transform.Find("AP").GetComponent<TextMeshProUGUI>();
            numericHealthText = this.transform.Find("NumericHealth").GetComponent<TextMeshProUGUI>();

            var images = this.transform.GetComponentsInChildren<Image>(true);

            attackTypeImage = images
                .First(x => x.gameObject.name.Equals("Attack", StringComparison.CurrentCultureIgnoreCase));

            damageTypeImage = images
                .First(x => x.gameObject.name.Equals("DamageType", StringComparison.CurrentCultureIgnoreCase));

            healthBar = images
                .First(x => x.gameObject.name.Equals("Healthbar", StringComparison.CurrentCultureIgnoreCase));

            // Set all material colors here.
            // Grab the material from the healthbar image
            hpMaterial = healthBar.material;
            hpMaterial.SetColor(CurrentHealthbarColor, Plugin.Config.CurrentHealthColor);
            hpMaterial.SetColor(DamagedHealthbarColor, Plugin.Config.RemainingHealthColor);
            hpMaterial.SetColor(ChunkColor, Plugin.Config.HealthChunkDividerColor);
            hpMaterial.SetInt(EnableCurrentHealthBlink, Plugin.Config.HealthBarBlink ? 1 : 0);
            hpMaterial.SetFloat(CurrentHealthBlinkingSpeed, Plugin.Config.HealthBarBlinkSpeed);

            healthBar.SetMaterialDirty();

            // Keep a white sprite just in case.
            var whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.zero);

            // Use static class to load images for the sprites
            var sprites = DataLoader
                .LoadFilesFromMemory<Sprite>(Plugin.BundleName, new List<string> { "melee", "ranged", "default" })
                .ToList();
            _meleeSprite = sprites[0] != null ? sprites[0] : whiteSprite;
            _rangedSprite = sprites[1] != null ? sprites[1] : whiteSprite;
            _defaultSprite = sprites[2] != null ? sprites[2] : whiteSprite;

            var damageSprites = DataLoader.LoadFilesFromMemory<Sprite>(Plugin.BundleName, _damageTypes).ToList();
            DamageSprites = new Dictionary<string, Sprite>();

            //Populate dictionary
            for (int i = 0; i < damageSprites.Count; i++)
            {
                DamageSprites.Add(_damageTypes[i], damageSprites[i]);
            }

            attackTypeImage.sprite = _defaultSprite;

            

            this.gameObject.SetActive(false);
        }

        private void Start()
        {
            _playerData = DungeonGameMode.Instance.Creatures.Player.CreatureData;
        }

        public void SetEnemy(Monster monster, Vector3 worldPos, float scaleSize)
        {
            if (monster == null) return;

            if (monster.CreatureData.Health.Dead)
            {
                return;
            }

            if (!monster.IsSeenByPlayer) return;

            if (Camera.main != null)
            {
                // Set object world pos to UI!
                // Didn't remember about the concese calculation, so:
                // https://discussions.unity.com/t/how-to-convert-from-world-space-to-canvas-space/117981
                Vector2 viewPortPos =
                    Camera.main.WorldToViewportPoint(worldPos + Vector3.Scale(Camera.main.transform.up, adjustment));
                Vector2 worldObjectScreenPosition = new Vector2(
                    ((viewPortPos.x * _canvas.sizeDelta.x) - (_canvas.sizeDelta.x * 0.5f)),
                    ((viewPortPos.y * _canvas.sizeDelta.y) - (_canvas.sizeDelta.y * 0.5f)));
                //viewPortPos += Adjustment;
                ((RectTransform)transform).anchoredPosition = worldObjectScreenPosition;

                ((RectTransform)transform).localScale = new Vector3(scaleSize, scaleSize, scaleSize);
            }
            else
            {
                Debug.LogError($"Camera.main is null, UI not tracking enemy correctly.");
            }

            if (_lastMonster == null || monster != _lastMonster)
            {
                AttachToNewMonster(monster);
                // I cant change sprite here because the enemy can change gun, drop it or switch ammunition. So its updated every frame?
                // Eugh
            }
            
            ChangeSprite(monster);

            hpMaterial.SetFloat(CurrentHealthPercent, monster.CreatureData.Health.Percent);
            apTextObject.text = $"{monster.ActionPoints}";
            numericHealthText.text = $"{monster.CreatureData.Health.Value}/{monster.CreatureData.Health.MaxValue}";

            if (_playerData == null) Logger.LogError($"SetEnemy(): Could not find player data.");
            
            var weaponRecord = _playerData?.Inventory.CurrentWeapon.Record<WeaponRecord>();
            var weaponComponent = _playerData?.Inventory.CurrentWeapon.Comp<WeaponComponent>();

            if (weaponRecord != null && weaponComponent != null)
            {
                DmgInfo damageInfo = weaponComponent.Damage;
                var damageResist = monster.CreatureData.GetResist(damageInfo.damage);

                float meleeMult = 1f;
                if (weaponRecord.IsMelee)
                {
                    damageInfo += _playerData.MeleeDamage;
                    damageInfo += _playerData.GetMeleeAddedFlatDamage();
                    meleeMult = _playerData.OverallMeleeDamageMult(_playerData.Inventory.CurrentWeapon,
                        ignoreFiremode: true);
                }

                float firemodeMult = (float)weaponComponent.CurrentFireMode.WeaponCastsCount *
                                     weaponComponent.CurrentFireMode.DamageMult;
                float avgDamage = ((float)damageInfo.maxDmg + (float)damageInfo.minDmg) * firemodeMult * meleeMult *
                                  0.5f;

                float maxHealth = (float)monster.CreatureData.Health.MaxValue;
                float currentHealth = (float)monster.CreatureData.Health._value;

                float remainingAvgHealth = Mathf.Clamp(currentHealth - avgDamage, 0f, maxHealth);
                float avgPercent = remainingAvgHealth / maxHealth;
                
                Logger.LogDebug(
                    $"SetEnemy(): Damage Approximation: {avgDamage} avg. {damageInfo.damage} damage with monster having {damageResist}% resistance, totaling {remainingAvgHealth}/{maxHealth} health.\nDisplayed bar is at {avgPercent}%.");
                
                hpMaterial.SetFloat(DamagedHealthPercent, avgPercent);
            }
            else
            {
                // Not executing dmg preview?
                Logger.LogDebug($"SetEnemy(): Not executing the damage preview.");
            }

            healthBar.SetMaterialDirty();
            EnableUI();
        }

        public void AttachToNewMonster(Monster newMonster)
        {
            if (_lastMonster != null)
                _lastMonster.CreatureData.Health.Killed -= OnAttachedDead;
            newMonster.CreatureData.Health.Killed += OnAttachedDead;
            _lastMonster = newMonster;

            healthBar.material.SetFloat(ChunkAmount,
                (float)_lastMonster.CreatureData.Health.MaxValue / (float)Plugin.Config.HealthChunkValue);
            healthBar.SetMaterialDirty();
        }

        private void ChangeSprite(Monster monster)
        {
            Inventory inventory = monster.CreatureData.Inventory;

            if (inventory == null) return;

            // hasRanged = inventory.WeaponSlots
            //     .Any(x => x.Items
            //         .Any(y => y?.Record<WeaponRecord>()?.IsMelee == false)
            //     );

            attackTypeImage.sprite = inventory.CurrentWeapon.Record<WeaponRecord>().IsMelee ? _meleeSprite : _rangedSprite;

            // Now we check the damage of the weapon.
            var currentDamageType =
                inventory.CurrentWeapon.Comp<WeaponComponent>().CurrentAmmoType.DmgType ?? string.Empty;
            DamageSprites.TryGetValue(currentDamageType, out var sprite);
            if (sprite != null)
            {
                Logger.LogDebug($"ChangeSprite(): Setting sprite for {currentDamageType}");
                damageTypeImage.sprite = sprite;
            }
            else
            {
                Logger.LogDebug($"ChangeSprite(): Sprite named {currentDamageType} not found in sprites dictionary.");
            }
        }

        private void OnAttachedDead()
        {
            DisableUI();
        }

        public void DisableUI()
        {
            this.gameObject.SetActive(false);
        }

        private void EnableUI()
        {
            this.apTextObject?.gameObject.SetActive(Plugin.Config.EnabledActionPoints);
            this.attackTypeImage?.gameObject.SetActive(Plugin.Config.EnabledAttackType);
            this.damageTypeImage?.gameObject.SetActive(Plugin.Config.EnabledDamageType);
            this.numericHealthText?.gameObject.SetActive(Plugin.Config.EnabledNumericHealth);
            this.healthBar?.transform.parent.gameObject.SetActive(Plugin.Config.EnabledHealthBar);

            hpMaterial.SetFloat(EnableDamagePreview, Plugin.Config.EnableRemainingHealthPreviewBar ? 1 : 0);
            hpMaterial.SetFloat(EnableChunk, Plugin.Config.HealthChunkEnabled ? 1 : 0);
            hpMaterial.SetFloat(CurrentHealthBlink, Plugin.Config.HealthBarBlink && Plugin.Config.EnableRemainingHealthPreviewBar ? 1 : 0);
            healthBar?.SetMaterialDirty();

            this.gameObject.SetActive(true);
        }
    }
}