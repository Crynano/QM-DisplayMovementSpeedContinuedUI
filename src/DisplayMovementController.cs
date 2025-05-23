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
        public Image attackTypeImage;
        public Image healthBar;
        public Image lifeRemainingPreviewBar;
        public Image backgroundBar;
        public RawImage marks;

        [Header("Images")]
        private Sprite _meleeSprite;
        private Sprite _rangedSprite;
        private Sprite _defaultSprite;

        private Monster _lastMonster;

        private RectTransform _root;

        private RectTransform _canvas;

        private Animator _animator;

        private CreatureData _playerData;

        public void Awake()
        {
            LoadComponents();
        }

        public void LoadComponents()
        {
            _canvas = this.gameObject.GetComponentInParent<Canvas>().transform as RectTransform;

            apTextObject = this.transform.GetComponentInChildren<TextMeshProUGUI>(true);

            var images = this.transform.GetComponentsInChildren<Image>(true);

            attackTypeImage = images
                .First(x => x.gameObject.name.Equals("Image", StringComparison.CurrentCultureIgnoreCase));

            healthBar = images
                .First(x => x.gameObject.name.Equals("CurrentHealth", StringComparison.CurrentCultureIgnoreCase));

            lifeRemainingPreviewBar = images
                .First(x => x.gameObject.name.Equals("RemainingHealthPreview", StringComparison.CurrentCultureIgnoreCase));

            backgroundBar = images
                .First(x => x.gameObject.name.Equals("Healthbar", StringComparison.CurrentCultureIgnoreCase));

            marks = this.transform
                .GetComponentsInChildren<RawImage>(true)
                .First(x => x.gameObject.name.StartsWith("Marks", StringComparison.CurrentCultureIgnoreCase));

            _root = this.transform
                .GetComponentsInChildren<RectTransform>(true)
                .First(x => x.gameObject.name.Equals("Root", StringComparison.CurrentCultureIgnoreCase));

            healthBar.color = Plugin.Config.CurrentHealthColor;
            lifeRemainingPreviewBar.color = Plugin.Config.RemainingHealthColor;
            backgroundBar.color = Plugin.Config.BackgroundHealthColor;
            marks.color = Plugin.Config.HealthChunkDividerColor;

            _animator = this.GetComponent<Animator>();

            var whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.zero);
            // Use static class to load images for the sprites
            var sprites = DataLoader.LoadFilesFromBundle<Sprite>(Plugin.BundleName, new List<string> { "melee", "ranged", "default" });
            _meleeSprite = sprites[0] != null ? sprites[0] : whiteSprite;
            _rangedSprite = sprites[1] != null ? sprites[1] : whiteSprite;
            _defaultSprite = sprites[2] != null ? sprites[2] : whiteSprite;

            attackTypeImage.sprite = _defaultSprite;
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
                Vector2 viewPortPos = Camera.main.WorldToViewportPoint(worldPos + Vector3.Scale(Camera.main.transform.up, adjustment));
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
                ChangeSprite(monster);
            }

            healthBar.fillAmount = monster.CreatureData.Health.Percent;
            apTextObject.text = $"{monster.ActionPoints}";

            if (_playerData == null)
                _playerData = DungeonGameMode.Instance.Creatures.Player.CreatureData;

            var weaponRecord = _playerData.Inventory.CurrentWeapon.Record<WeaponRecord>();
            var weaponComponent = _playerData.Inventory.CurrentWeapon.Comp<WeaponComponent>();

            if (weaponRecord != null && weaponComponent != null)
            {
                DmgInfo damageInfo = weaponComponent.Damage;
                var damageResist = monster.CreatureData.GetResist(damageInfo.damage);

                float meleeMult = 1f;
                if (weaponRecord.IsMelee)
                {
                    damageInfo += _playerData.MeleeDamage;
                    damageInfo += _playerData.GetMeleeAddedFlatDamage();
                    meleeMult = _playerData.OverallMeleeDamageMult(_playerData.Inventory.CurrentWeapon, ignoreFiremode: true);
                }

                float firemodeMult = (float)weaponComponent.CurrentFireMode.WeaponCastsCount * weaponComponent.CurrentFireMode.DamageMult;
                float avgDamage = ((float)damageInfo.maxDmg + (float)damageInfo.minDmg) * firemodeMult * meleeMult * 0.5f;

                float maxHealth = (float)monster.CreatureData.Health.MaxValue;
                float currentHealth = (float)monster.CreatureData.Health._value;

                float remainingAvgHealth = Mathf.Clamp(currentHealth - avgDamage, 0f, maxHealth);
                float avgPercent = remainingAvgHealth / maxHealth;
#if DEBUG
                Debug.Log($"UI Damage Report Aproximation: {avgDamage} {damageInfo.damage} damage with monster having {damageResist}% resistance, totaling {remainingAvgHealth}/{maxHealth} health.\nDisplayed bar is at {avgPercent}%.");
#endif

                lifeRemainingPreviewBar.fillAmount = avgPercent;
            }

            EnableUI();
        }

        public void AttachToNewMonster(Monster newMonster)
        {
            if (_lastMonster != null)
                _lastMonster.CreatureData.Health.Killed -= OnAttachedDead;
            newMonster.CreatureData.Health.Killed += OnAttachedDead;
            _lastMonster = newMonster;

            marks.uvRect = new Rect(0, 0, ((float)_lastMonster.CreatureData.Health.MaxValue / (float)Plugin.Config.HealthChunkValue), 1);
        }

        private void ChangeSprite(Monster monster)
        {
            Inventory inventory = monster.CreatureData.Inventory;

            bool hasRanged = false;

            if (inventory != null)
            {
                //Assuming that if one ranged weapon is found, it's ranged.
                //Ignoring turrets since they will never be melee.

                hasRanged = inventory.WeaponSlots
                    .Any(x => x.Items
                        .Any(y => y?.Record<WeaponRecord>()?.IsMelee == false)
                    );
            }
            attackTypeImage.sprite = hasRanged ? _rangedSprite : _meleeSprite;
        }

        private void OnAttachedDead()
        {
            DisableUI();
        }

        public void DisableUI()
        {
            this._root?.gameObject.SetActive(false);
            _animator?.SetBool("Blink", false);
        }

        private void EnableUI()
        {
            this.attackTypeImage?.gameObject.SetActive(Plugin.Config.EnabledAttackType);
            this.healthBar?.transform.parent.gameObject.SetActive(Plugin.Config.EnabledHealthBar);
            this.lifeRemainingPreviewBar.gameObject.SetActive(Plugin.Config.EnableRemainingHealthPreviewBar);
            this.apTextObject?.gameObject.SetActive(Plugin.Config.EnabledActionPoints);
            this.marks.enabled = Plugin.Config.HealthChunkEnabled;
            if (Plugin.Config.HealthBarBlink && Plugin.Config.EnableRemainingHealthPreviewBar)
                _animator?.SetBool("Blink", true);
            this._root.gameObject.SetActive(true);
        }
    }
}
