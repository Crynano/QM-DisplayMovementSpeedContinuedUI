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
        private static Vector3 adjustment = new Vector3(0f, 0.15f, 0f);

        [Header("Unity Components")]
        TextMeshProUGUI apTextObject;
        TextMeshProUGUI numericHealthText;
        Image attackTypeImage;
        Image damageTypeImage;
        Image healthBar;

        [Header("Sprites")]
        private Sprite _meleeSprite;
        private Sprite _rangedSprite;

        private Dictionary<string, Sprite> _damageSprites;

        private Creature _creature;
        public Creature Creature => _creature;

        private RectTransform _canvas;

        private CreatureData _playerData;

        private Material hpMaterial;

        private CanvasGroup _canvasGroup;
        private ModConfig.UiMode cachedUiMode;
        public bool Focused { get; set; }
        public bool IsDead { get; private set; }

        public void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            _playerData = DungeonGameMode.Instance.Creatures.Player.CreatureData;
        }

        public void LateUpdate()
        {
            if (Creature == null || Creature.CreatureData.Health.Dead)
            {
                IsDead = true;
            }

            if (IsDead || !_creature.IsSeenByPlayer)
            {
                _canvasGroup.alpha = 0f;
                return;
            }

            if (cachedUiMode == ModConfig.UiMode.OnlyWhenFocused)
            {
                _canvasGroup.alpha = (Focused ? 1f : 0f);
            }
            else
            {
                _canvasGroup.alpha = (Focused ? 1f : 0.5f);
            }

            if (Camera.main != null)
            {
                
                Vector2 viewPortPos =
                    Camera.main.WorldToViewportPoint(Creature.transform.position + Vector3.Scale(Camera.main.transform.up, adjustment));
                Vector2 worldObjectScreenPosition = new Vector2(
                    ((viewPortPos.x * _canvas.sizeDelta.x) - (_canvas.sizeDelta.x * 0.5f)),
                    ((viewPortPos.y * _canvas.sizeDelta.y) - (_canvas.sizeDelta.y * 0.5f)));
                
                ((RectTransform)transform).anchoredPosition = worldObjectScreenPosition;
                ((RectTransform)transform).localScale = Plugin.ScaleSize;
            }
            else
            {
                Debug.LogError($"Camera.main is null, UI not tracking enemy correctly.");
            }

            ChangeSprite(Creature);

            hpMaterial.SetFloat(Plugin.CurrentHealthPercent, Creature.CreatureData.Health.Percent);
            apTextObject.text = $"{Creature.ActionPointsLeft}";
            numericHealthText.text = $"{Creature.CreatureData.Health.Value}/{Creature.CreatureData.Health.MaxValue}";

            CalculateDamagePreview(Creature);

            healthBar.SetMaterialDirty();

            Focused = false;
        }

        public void LoadUI()
        {
            LoadComponents();
            LoadSettings();
        }

        public void SetSprites(Sprite meleeSprite, Sprite rangedSprite, Dictionary<string, Sprite> damageSprites)
        {
            _meleeSprite = meleeSprite;
            _rangedSprite = rangedSprite;
            this._damageSprites = damageSprites;
        }

        private void LoadComponents()
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
        }

        private void LoadSettings()
        {
            // Set all material colors here.
            // Grab the material from the healthbar image
            hpMaterial = healthBar.material;
            hpMaterial.SetColor(Plugin.CurrentHealthbarColor, Plugin.Config.CurrentHealthColor);
            hpMaterial.SetColor(Plugin.DamagedHealthbarColor, Plugin.Config.RemainingHealthColor);
            hpMaterial.SetColor(Plugin.ChunkColor, Plugin.Config.HealthChunkDividerColor);
            hpMaterial.SetInt(Plugin.EnableCurrentHealthBlink, Plugin.Config.HealthBarBlink ? 1 : 0);
            hpMaterial.SetFloat(Plugin.CurrentHealthBlinkingSpeed, Plugin.Config.HealthBarBlinkSpeed);

            healthBar.SetMaterialDirty();


            this.apTextObject?.gameObject.SetActive(Plugin.Config.EnabledActionPoints);
            this.attackTypeImage?.gameObject.SetActive(Plugin.Config.EnabledAttackType);
            this.damageTypeImage?.gameObject.SetActive(Plugin.Config.EnabledDamageType);
            this.numericHealthText?.gameObject.SetActive(Plugin.Config.EnabledNumericHealth);
            this.healthBar?.transform.parent.gameObject.SetActive(Plugin.Config.EnabledHealthBar);
        }

        private void CalculateDamagePreview(Creature monster)
        {
            if (!Focused) return;
            if (_playerData == null) Logger.LogError($"SetEnemy(): Could not find player data.");

            var weaponRecord = _playerData?.Inventory.CurrentWeapon.Record<WeaponRecord>();
            var weaponComponent = _playerData?.Inventory.CurrentWeapon.Comp<WeaponComponent>();

            if (weaponRecord != null && weaponComponent != null)
            {
                DmgInfo damageInfo = weaponComponent.Damage;

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

                hpMaterial.SetFloat(Plugin.DamagedHealthPercent, avgPercent);
            }
        }

        public void SetEnemy(Monster monster)
        {
            AttachToNewMonster(monster);
            ChangeSprite(Creature);
            EnableUI();
        }

        public void AttachToNewMonster(Creature newMonster)
        {
            if (_creature != null)
                _creature.CreatureData.Health.Killed -= OnAttachedDead;

            newMonster.CreatureData.Health.Killed += OnAttachedDead;

            _creature = newMonster;

            var hpval = (float)_creature.CreatureData.Health.MaxValue / (float)Plugin.Config.HealthChunkValue;

            healthBar.material.SetFloat(Plugin.ChunkAmount, hpval);
            healthBar.SetMaterialDirty();
        }

        private void ChangeSprite(Creature monster)
        {
            Inventory inventory = monster.CreatureData.Inventory;

            if (inventory == null) return;

            attackTypeImage.sprite = inventory.CurrentWeapon.Record<WeaponRecord>().IsMelee ? _meleeSprite : _rangedSprite;

            var currentDamageType =
                inventory.CurrentWeapon.Comp<WeaponComponent>().Damage.damage ??
                inventory.CurrentWeapon.Comp<WeaponComponent>().CurrentAmmoType.DmgType ??
                string.Empty;

            _damageSprites.TryGetValue(currentDamageType, out var sprite);

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
            Plugin.ReleaseMonsterUI(this.Creature);
        }

        public void DisableUI()
        {
            _canvasGroup.alpha = 0f;
            this.enabled = false;
        }

        private void EnableUI()
        {
            _canvasGroup.alpha = 1f;
            this.enabled = true;
        }

        public void OnEnable()
        {
            cachedUiMode = Plugin.Config.UIMode;
        }
    }
}