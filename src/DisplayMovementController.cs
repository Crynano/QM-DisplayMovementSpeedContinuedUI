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

        // public void Awake()
        // {
        //     LoadComponents();
        // }

        // Call LoadComponents after its created. Awake just creates a lag spike when user first enables it.
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
            hpMaterial.SetColor(Plugin.CurrentHealthbarColor, Plugin.Config.CurrentHealthColor);
            hpMaterial.SetColor(Plugin.DamagedHealthbarColor, Plugin.Config.RemainingHealthColor);
            hpMaterial.SetColor(Plugin.ChunkColor, Plugin.Config.HealthChunkDividerColor);
            hpMaterial.SetInt(Plugin.EnableCurrentHealthBlink, Plugin.Config.HealthBarBlink ? 1 : 0);
            hpMaterial.SetFloat(Plugin.CurrentHealthBlinkingSpeed, Plugin.Config.HealthBarBlinkSpeed);

            healthBar.SetMaterialDirty();

            // Keep a white sprite just in case.
            var whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.zero);

            // Use static class to load images for the sprites
            var spriteResourcesList = new List<string> { "melee", "ranged", "default" };
            spriteResourcesList.AddRange(_damageTypes);
            
            // We reduce the call from 2 to 1. More efficient way. We recover all sprites at once.
            var sprites = DataLoader.LoadFilesFromMemory<Sprite>(Plugin.BundleName, spriteResourcesList).ToList();
            _meleeSprite = sprites[0] != null ? sprites[0] : whiteSprite;
            _rangedSprite = sprites[1] != null ? sprites[1] : whiteSprite;
            _defaultSprite = sprites[2] != null ? sprites[2] : whiteSprite;
            
            DamageSprites = new Dictionary<string, Sprite>();

            //Populate dictionary
            for (int i = 3; i < sprites.Count; i++)
            {
                DamageSprites.Add(_damageTypes[i-3], sprites[i]);
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

            hpMaterial.SetFloat(Plugin.CurrentHealthPercent, monster.CreatureData.Health.Percent);
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
                
                hpMaterial.SetFloat(Plugin.DamagedHealthPercent, avgPercent);
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

            healthBar.material.SetFloat(Plugin.ChunkAmount,
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
                inventory.CurrentWeapon.Comp<WeaponComponent>().Damage.damage ??
                inventory.CurrentWeapon.Comp<WeaponComponent>().CurrentAmmoType.DmgType ??
                string.Empty;
            DamageSprites.TryGetValue(currentDamageType, out var sprite);
            if (sprite != null)
            {
                Logger.LogDebug($"ChangeSprite(): Setting sprite for {currentDamageType}");
                damageTypeImage.sprite = sprite;
            }
            else
            {
                Logger.LogError($"ChangeSprite(): Sprite named {currentDamageType} not found in sprites dictionary. Weapon was {inventory.CurrentWeapon.Id}");
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

            hpMaterial.SetFloat(Plugin.EnableDamagePreview, Plugin.Config.EnableRemainingHealthPreviewBar ? 1 : 0);
            hpMaterial.SetFloat(Plugin.EnableChunk, Plugin.Config.HealthChunkEnabled ? 1 : 0);
            hpMaterial.SetFloat(Plugin.CurrentHealthBlink, Plugin.Config.HealthBarBlink && Plugin.Config.EnableRemainingHealthPreviewBar ? 1 : 0);
            healthBar?.SetMaterialDirty();

            this.gameObject.SetActive(true);
        }
    }
}