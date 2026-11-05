using System;
using BulletHeaven.Game.Weapons;

namespace BulletHeaven.Game.Components;

public sealed class WeaponSlot
{
    public WeaponItem? Weapon;
    public float CooldownTimer;
    public float VisualTimer;
    public float VisualDuration = 0.18f;
    public float AttackAngle;
    public float AttackRange;
}

public struct EquipmentComponent
{
    public const int MaxWeaponSlots = 6;
    public WeaponSlot[] Slots;

    public EquipmentComponent() : this(MaxWeaponSlots)
    {
    }

    public EquipmentComponent(int maxSlots)
    {
        Slots = new WeaponSlot[maxSlots];
        for (int i = 0; i < maxSlots; i++)
        {
            Slots[i] = new WeaponSlot();
        }
    }

    public bool CanEquipInSlot(int slotIndex, WeaponItem item)
    {
        if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length) return false;
        return item.Definition.Type != WeaponType.Passive;
    }

    public bool Equip(int slotIndex, WeaponItem item)
    {
        if (!CanEquipInSlot(slotIndex, item)) return false;
        Slots[slotIndex].Weapon = item;
        Slots[slotIndex].CooldownTimer = 0f;
        return true;
    }

    public bool EquipFirstAvailable(WeaponItem item)
    {
        int emptyIdx = FirstEmptySlotIndex();
        if (emptyIdx == -1) return false;
        return Equip(emptyIdx, item);
    }

    public WeaponItem? Unequip(int slotIndex)
    {
        if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length) return null;
        var prev = Slots[slotIndex].Weapon;
        Slots[slotIndex].Weapon = null;
        return prev;
    }

    public int FirstEmptySlotIndex()
    {
        if (Slots == null) return -1;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].Weapon == null) return i;
        }
        return -1;
    }

    public int CountEquippedWeapons()
    {
        if (Slots == null) return 0;
        int count = 0;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].Weapon != null) count++;
        }
        return count;
    }

    public bool CanMergeAny(out int slotA, out int slotB)
    {
        slotA = -1;
        slotB = -1;
        if (Slots == null) return false;

        for (int i = 0; i < Slots.Length; i++)
        {
            var wA = Slots[i].Weapon;
            if (wA == null || wA.Tier >= 4) continue;

            for (int j = i + 1; j < Slots.Length; j++)
            {
                var wB = Slots[j].Weapon;
                if (wB == null) continue;

                if (wA.Definition.Id == wB.Definition.Id && wA.Tier == wB.Tier)
                {
                    slotA = i;
                    slotB = j;
                    return true;
                }
            }
        }
        return false;
    }

    public bool AreMatching(int slotA, int slotB)
    {
        if (Slots == null || slotA < 0 || slotB < 0 || slotA >= Slots.Length || slotB >= Slots.Length || slotA == slotB) return false;
        var wA = Slots[slotA].Weapon;
        var wB = Slots[slotB].Weapon;
        if (wA == null || wB == null) return false;
        return wA.Definition.Id == wB.Definition.Id && wA.Tier == wB.Tier && wA.Tier < 4;
    }
}
