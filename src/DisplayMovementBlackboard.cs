using MGSC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

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
                .Select(x => new { Record = x, Descriptor = x.ContentDescriptor as DamageTypesDescriptor })
                .Where(x => x.Descriptor != null)
                .ToDictionary(x => x.Record.Id, x => x.Descriptor.ResistTypeIcon);
        }
    }
}
