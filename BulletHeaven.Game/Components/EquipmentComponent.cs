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
    public WeaponSlot[] Slots;

    public EquipmentComponent() : this(6)
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

    public bool Equip(int slotIndex, WeaponItem item)
    {
        if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length) return false;
        Slots[slotIndex].Weapon = item;
        Slots[slotIndex].CooldownTimer = 0f;
        return true;
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
}

