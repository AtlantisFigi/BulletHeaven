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
    public static readonly EquipSlot[] SlotTypes =
    {
        EquipSlot.Head,
        EquipSlot.Hand,
        EquipSlot.Chest,
        EquipSlot.Hand,
        EquipSlot.Legs
    };

    public WeaponSlot[] Slots;

    public EquipmentComponent() : this(5)
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
        if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Length || slotIndex >= SlotTypes.Length) return false;
        return SlotTypes[slotIndex] == item.Definition.Slot;
    }

    public bool Equip(int slotIndex, WeaponItem item)
    {
        if (!CanEquipInSlot(slotIndex, item)) return false;
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

    public int FindFirstMatchingEmptySlot(WeaponItem item)
    {
        if (Slots == null) return -1;
        for (int i = 0; i < Slots.Length && i < SlotTypes.Length; i++)
        {
            if (Slots[i].Weapon == null && SlotTypes[i] == item.Definition.Slot)
            {
                return i;
            }
        }
        return -1;
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
