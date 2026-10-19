using System;

namespace Game.Meta.Progression
{
    public enum ContentUnlockType
    {
        Weapon,
        Hero,
        World,
        Perk
    }

    public class ContentUnlockDefinition
    {
        public string LevelId { get; }
        public string ContentId { get; }
        public ContentUnlockType UnlockType { get; }

        public ContentUnlockDefinition(string levelId, string contentId, ContentUnlockType unlockType)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("LevelId is required.", nameof(levelId));
            if (string.IsNullOrWhiteSpace(contentId)) throw new ArgumentException("ContentId is required.", nameof(contentId));

            LevelId = levelId;
            ContentId = contentId;
            UnlockType = unlockType;
        }
    }
}
