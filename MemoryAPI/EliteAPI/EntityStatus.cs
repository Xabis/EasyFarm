using System;

namespace EliteMMO.API
{
    public enum EntityStatus
    {
        Idle = 0,
        Engaged = 1,
        Dead = 2,
        DeadEngaged = 3,
        Event = 4,
        Chocobo = 5,
        [Obsolete("No longer used.")]
        _OldFishingIdle1 = 6,
        Kneel1 = 7,
        DoorOpened = 8,
        DoorClosed = 9,
        Maintenance01 = 28,
        Maintenance02 = 0x1F,
        Healing = 33,
        [Obsolete("No longer used.")]
        _OldFishingBite = 38,
        [Obsolete("No longer used.")]
        _OldFishingCatch = 39,
        [Obsolete("No longer used.")]
        _OldFishingRodBreak = 40,
        [Obsolete("No longer used.")]
        _OldFishingLineBreak = 41,
        [Obsolete("No longer used.")]
        _OldFishingCatchMonster = 42,
        [Obsolete("No longer used.")]
        FishingNoCatch = 43,
        Synthing = 44,
        Sitting = 47,
        Kneel2 = 48,
        [Obsolete("No longer used.")]
        _OldFishingIdle2 = 50,
        [Obsolete("No longer used.")]
        _OldFishingRodCenter = 51,
        [Obsolete("No longer used.")]
        _OldFishingRodRight = 52,
        [Obsolete("No longer used.")]
        _OldFishingRodLeft = 53,
        FishingIdle = 56,
        FishingBite = 57,
        FishingCatch = 58,
        FishingRodBreak = 59,
        FishingLineBreak = 60,
        FishingCatchMonster = 61,
        FishingCancel = 62,
        SitChair = 0x3F,
        Unconscious = 0x40,
        UnknownEffect = 72
    }
}
