using System;
using System.Collections.Generic;
using System.Linq;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Weapons;

namespace BulletHeaven.Game.Shop;

public sealed class ShopCard
{
    public WeaponDefinition Definition { get; set; } = null!;
    public int Price { get; set; }
    public bool IsLocked { get; set; }
    public bool IsPurchased { get; set; }
}

public sealed class ShopService
{
    public const int CardCount = 3;
    public ShopCard[] Cards { get; } = new ShopCard[CardCount];
    public int RerollCount { get; private set; } = 0;

    private readonly Random _rng = new();

    public ShopService()
    {
        for (int i = 0; i < CardCount; i++)
        {
            Cards[i] = new ShopCard();
        }
    }

    public int GetCurrentRerollCost(int waveNumber)
    {
        return Math.Max(3, 4 + waveNumber + RerollCount * 2);
    }

    public void GenerateCards(int waveNumber)
    {
        var allDefinitions = WeaponDatabase.GetAll().ToList();
        if (allDefinitions.Count == 0) return;

        for (int i = 0; i < CardCount; i++)
        {
            if (Cards[i].IsLocked && !Cards[i].IsPurchased)
            {
                continue;
            }

            var chosen = allDefinitions[_rng.Next(allDefinitions.Count)];
            int basePrice = chosen.Type == WeaponType.Passive ? 22 : 28;
            int waveTax = Math.Max(0, (waveNumber - 1) * 3);
            int variance = _rng.Next(-3, 6);

            Cards[i].Definition = chosen;
            Cards[i].Price = Math.Max(12, basePrice + waveTax + variance);
            Cards[i].IsPurchased = false;
            Cards[i].IsLocked = false;
        }
    }

    public bool Reroll(ref int playerBiomass, int waveNumber)
    {
        int cost = GetCurrentRerollCost(waveNumber);
        if (playerBiomass < cost) return false;

        var allDefinitions = WeaponDatabase.GetAll().ToList();
        if (allDefinitions.Count == 0) return false;

        playerBiomass -= cost;
        RerollCount++;

        for (int i = 0; i < CardCount; i++)
        {
            if (Cards[i].IsLocked && !Cards[i].IsPurchased)
            {
                continue;
            }

            var chosen = allDefinitions[_rng.Next(allDefinitions.Count)];
            int basePrice = chosen.Type == WeaponType.Passive ? 22 : 28;
            int waveTax = Math.Max(0, (waveNumber - 1) * 3);
            int variance = _rng.Next(-3, 6);

            Cards[i].Definition = chosen;
            Cards[i].Price = Math.Max(12, basePrice + waveTax + variance);
            Cards[i].IsPurchased = false;
        }

        return true;
    }

    public bool BuyItem(int index, ref int playerBiomass, ref EquipmentComponent equip, ref BioInventoryComponent inventory)
    {
        if (index < 0 || index >= CardCount) return false;
        var card = Cards[index];
        if (card.IsPurchased || card.Definition == null) return false;
        if (playerBiomass < card.Price) return false;

        if (card.Definition.Type != WeaponType.Passive)
        {
            if (equip.CountEquippedWeapons() >= EquipmentComponent.MaxWeaponSlots)
            {
                return false;
            }

            bool equipped = equip.EquipFirstAvailable(new WeaponItem(card.Definition, 1));
            if (!equipped) return false;
        }
        else
        {
            inventory.Add(new WeaponItem(card.Definition, 1));
        }

        playerBiomass -= card.Price;
        card.IsPurchased = true;
        card.IsLocked = false;
        return true;
    }

    public void ToggleLock(int index)
    {
        if (index >= 0 && index < CardCount && !Cards[index].IsPurchased)
        {
            Cards[index].IsLocked = !Cards[index].IsLocked;
        }
    }

    public void OnWaveCompleted(int waveNumber)
    {
        RerollCount = 0;
        GenerateCards(waveNumber);
    }
}

