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
    public float SpecialActiveTimer;
    public float SpecialVisualTimer;
    public float SpecialVisualDuration = 0.35f;

    public void Reset()
    {
        Weapon = null;
        CooldownTimer = 0f;
        VisualTimer = 0f;
        VisualDuration = 0.18f;
        AttackAngle = 0f;
        AttackRange = 0f;
        SpecialActiveTimer = 0f;
        SpecialVisualTimer = 0f;
        SpecialVisualDuration = 0.35f;
    }
}

public struct EquipmentComponent
{
    public const int MaxWeaponSlots = 3;
    public WeaponSlot[] Slots;

    public EquipmentComponent() : this(MaxWeaponSlots)
    {
    }

    public EquipmentComponent(int maxSlots)
    {
        int clamped = Math.Clamp(maxSlots, 1, MaxWeaponSlots);
        Slots = new WeaponSlot[clamped];
        for (int i = 0; i < clamped; i++)
        {
            Slots[i] = new WeaponSlot();
        }
    }

    public bool CanEquipInSlot(int slotIndex, WeaponItem item)
    {
        if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length) return false;
        return item != null;
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

    public int FindSlotByWeaponId(string weaponId)
    {
        if (Slots == null || string.IsNullOrEmpty(weaponId)) return -1;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].Weapon?.Definition.Id == weaponId) return i;
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
}
