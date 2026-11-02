using System;
using System.Collections.Generic;
using BulletHeaven.Game.Weapons;

namespace BulletHeaven.Game.Components;

public struct BioStashComponent
{
    public List<WeaponItem> Items { get; }
    public int Capacity { get; set; }

    public BioStashComponent() : this(12)
    {
    }

    public BioStashComponent(int capacity)
    {
        Capacity = capacity;
        Items = new List<WeaponItem>(capacity);
    }

    public bool Add(WeaponItem item)
    {
        if (Items == null || Items.Count >= Capacity) return false;
        Items.Add(item);
        return true;
    }

    public bool Remove(WeaponItem item)
    {
        return Items != null && Items.Remove(item);
    }

    public bool CanMerge(int indexA, int indexB)
    {
        if (Items == null) return false;
        if (indexA == indexB || indexA < 0 || indexB < 0) return false;
        if (indexA >= Items.Count || indexB >= Items.Count) return false;

        var a = Items[indexA];
        var b = Items[indexB];

        return a.Definition.Id == b.Definition.Id && a.Tier == b.Tier && a.Tier < 4;
    }

    public WeaponItem? Merge(int indexA, int indexB)
    {
        if (!CanMerge(indexA, indexB) || Items == null) return null;

        var item = Items[indexA];
        item.Tier++;

        int first = Math.Max(indexA, indexB);
        int second = Math.Min(indexA, indexB);

        Items.RemoveAt(first);
        Items.RemoveAt(second);
        Items.Add(item);

        return item;
    }
}

