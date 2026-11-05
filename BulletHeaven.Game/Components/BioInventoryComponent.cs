using System.Collections.Generic;
using System.Linq;
using BulletHeaven.Game.Weapons;

namespace BulletHeaven.Game.Components;

public struct BioInventoryComponent
{
    public List<WeaponItem> Items { get; }

    public BioInventoryComponent()
    {
        Items = new List<WeaponItem>();
    }

    public void Add(WeaponItem item)
    {
        Items.Add(item);
    }

    public bool Remove(WeaponItem item)
    {
        return Items.Remove(item);
    }

    public int Count => Items.Count;

    public List<(WeaponItem Sample, int Count)> GetStackedItems()
    {
        var result = new List<(WeaponItem Sample, int Count)>();
        if (Items == null || Items.Count == 0) return result;

        var groups = Items.GroupBy(it => (it.Definition.Id, it.Tier));
        foreach (var group in groups)
        {
            var first = group.First();
            result.Add((first, group.Count()));
        }

        return result;
    }
}

