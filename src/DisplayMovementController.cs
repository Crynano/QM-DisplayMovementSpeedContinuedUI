using MGSC;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QM_DisplayMovementSpeedContinuedUIPermanent
{
    public class DisplayMovementController : MonoBehaviour
    {
        [Header("Manual adjusting")]
        public Vector3 Adjustment = new Vector3(0f, 0.15f, 0f);

        [Header("Components")]
        public TextMeshProUGUI APTextObject;
        public Image AttackTypeImage;
        public Image HealthBar;

        [Header("Images")]
        [SerializeField] private Sprite meleeSprite;
        [SerializeField] private Sprite rangedSprite;
        [SerializeField] private Sprite defaultSprite;

        private RectTransform Root;

        private Monster lastMonster;

        private RectTransform Canvas;

        public void LoadComponents(string bundleName)
        {
            Canvas = this.gameObject.GetComponentInParent<Canvas>().transform as RectTransform;

            APTextObject = this.transform.GetComponentInChildren<TextMeshProUGUI>();

            AttackTypeImage = this.transform.GetComponentsInChildren<Image>()
                                    .Where(x => x.gameObject.name.Equals("Image", StringComparison.CurrentCultureIgnoreCase))
                                    .First();

            HealthBar = this.transform.GetComponentsInChildren<Image>(true)
                                    .Where(x => x.gameObject.name.Equals("Fillbar", StringComparison.CurrentCultureIgnoreCase))
                                    .First();

            Root = this.transform.GetComponentsInChildren<RectTransform>()
                                    .Where(x => x.gameObject.name.Equals("Root", StringComparison.CurrentCultureIgnoreCase))
                                    .First();

            var whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.zero);
            // Use static class to load images for the sprites
            var sprites = DataLoader.LoadFilesFromBundle<Sprite>(bundleName, new List<string> { "melee", "ranged", "default" });
            meleeSprite = sprites[0] != null ? sprites[0] : whiteSprite;
            rangedSprite = sprites[1] != null ? sprites[1] : whiteSprite;
            defaultSprite = sprites[2] != null ? sprites[2] : whiteSprite;

            AttackTypeImage.sprite = defaultSprite;
        }

        public void SetEnemy(Monster monster)
        {
            AttachToNewMonster(monster);
            ChangeSprite(monster);

            Vector3 monsterPos = lastMonster.transform.position;

            if (Camera.main != null)
            {
                // Set object world pos to UI!
                // Didn't remember about the concese calculation, so:
                // https://discussions.unity.com/t/how-to-convert-from-world-space-to-canvas-space/117981
                Vector2 viewPortPos = Camera.main.WorldToViewportPoint(monsterPos + Vector3.Scale(Camera.main.transform.up, Adjustment));
                Vector2 WorldObject_ScreenPosition = new Vector2(
                            ((viewPortPos.x * Canvas.sizeDelta.x) - (Canvas.sizeDelta.x * 0.5f)),
                            ((viewPortPos.y * Canvas.sizeDelta.y) - (Canvas.sizeDelta.y * 0.5f)));
                //viewPortPos += Adjustment;
                ((RectTransform)transform).anchoredPosition = WorldObject_ScreenPosition;
            }
            else
            {
                Debug.LogError($"Camera.main is null, UI not tracking enemy correctly.");
            }

            HealthBar.fillAmount = lastMonster.CreatureData.Health.Percent;
            APTextObject.text = $"{lastMonster.ActionPoints}";  //$"{monster.ActionPointsLeft}/{monster.ActionPoints}";

            EnableUI();
        }

        public void UpdateElement(float sizeDelta)
        {
            if (lastMonster == null || lastMonster.CreatureData.Health.Dead || !lastMonster.IsSeenByPlayer)
            {
                DisableUI();
                return;
            }

            if (Camera.main != null)
            {
                // Set object world pos to UI!
                // Didn't remember about the concese calculation, so:
                // https://discussions.unity.com/t/how-to-convert-from-world-space-to-canvas-space/117981
                Vector2 viewPortPos = Camera.main.WorldToViewportPoint(lastMonster.transform.position + Vector3.Scale(Camera.main.transform.up, Adjustment));
                Vector2 WorldObject_ScreenPosition = new Vector2(
                            ((viewPortPos.x * Canvas.sizeDelta.x) - (Canvas.sizeDelta.x * 0.5f)),
                            ((viewPortPos.y * Canvas.sizeDelta.y) - (Canvas.sizeDelta.y * 0.5f)));
                //viewPortPos += Adjustment;
                ((RectTransform)transform).anchoredPosition = WorldObject_ScreenPosition;
                HealthBar.fillAmount = lastMonster.CreatureData.Health.Percent;
                APTextObject.text = $"{lastMonster.ActionPoints}";  //$"{monster.ActionPointsLeft}/{monster.ActionPoints}";

                // Trying to adapt size to zoom.
                //((RectTransform)transform).sizeDelta = new Vector2(sizeDelta, sizeDelta);
                //((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizeDelta);
                //((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeDelta);
                ((RectTransform)transform).localScale = new Vector3(sizeDelta, sizeDelta, sizeDelta);
                EnableUI();
            }
            else
            {
                Debug.LogError($"Camera.main is null, UI not tracking enemy correctly.");
            }
        }

        public void AttachToNewMonster(Monster newMonster)
        {
            if (lastMonster != null)
                lastMonster.CreatureData.Health.Killed -= OnAttachedDead;
            newMonster.CreatureData.Health.Killed += OnAttachedDead;
            lastMonster = newMonster;
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
            AttackTypeImage.sprite = hasRanged ? rangedSprite : meleeSprite;
        }

        //private void OnDestroy()
        //{
        //    lastMonster.CreatureData.Health.Killed -= OnAttachedDead;
        //}

        public void DisableUI()
        {
            this.Root.gameObject.SetActive(false);
        }

        private void EnableUI()
        {
            //if (!Plugin.IsUIEnabled) { DisableUI(); return; }
            this.AttackTypeImage.gameObject.SetActive(Plugin.IsAttackTypeEnabled);
            this.HealthBar.transform.parent.gameObject.SetActive(Plugin.IsHealthBarEnabled);
            this.APTextObject.gameObject.SetActive(Plugin.IsActionPointsEnabled);
            this.Root.gameObject.SetActive(true);
        }

        private void OnAttachedDead()
        {
            DisableUI();
        }
    }
}
