using MGSC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public class DisplayMovementBlackboard
    {
        public Sprite MeleeSprite { get; private set; }
        public Sprite RangedSprite { get; private set; }

        public Dictionary<string, Sprite> DamageSprites { get; private set; }

        public DisplayMovementBlackboard()
        {
            LoadSprites();
            LoadDamageSprites();
        }

        public void LoadSprites()
        {
            var whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.zero);

            var spriteResourcesList = new List<string> { "melee", "ranged", "default" };

            var sprites = DataLoader.LoadFilesFromMemory<Sprite>(Plugin.BundleName, spriteResourcesList).ToList();
            MeleeSprite = sprites[0] != null ? sprites[0] : whiteSprite;
            RangedSprite = sprites[1] != null ? sprites[1] : whiteSprite;
        }

        private void LoadDamageSprites()
        {
            DamageSprites = Data.DamageTypes.Records
                .Select(damageType =>
                {
                    var descriptor = damageType.ContentDescriptor as DamageTypesDescriptor;
                    var icon = descriptor?.ResistTypeIcon;

                    if (icon == null && damageType.ResistTypes.Count > 0)
                    {
                        var firstResistTypeRecord = Data.DamageTypes.GetRecord(damageType.ResistTypes[0]);
                        if (firstResistTypeRecord != null)
                        {
                            icon = ((DamageTypesDescriptor)firstResistTypeRecord.ContentDescriptor).ResistTypeIcon;
                        }
                        else
                        {
                            Debug.LogWarning($"Damage type {damageType.Id} has a resist type {damageType.ResistTypes[0]} that does not exist in the data.");
                        }
                    }
                    else if (icon == null)
                    {
                        Debug.LogWarning($"Damage type {damageType.Id} does not have a resist type icon and has no resist types to fallback to.");
                    }

                    return new { Id = damageType.Id, Icon = icon };
                })
                .ToDictionary(x => x.Id, x => x.Icon);
        }
    }
}
