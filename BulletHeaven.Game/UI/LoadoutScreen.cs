using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Shop;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game.UI;

public sealed class LoadoutScreen
{
    private readonly ShopService _shopService = new();
    private int _lastWaveLoaded = -1;

    private bool _isEvolutionModalOpen = false;
    private int _evolutionSlotA = -1;
    private int _evolutionSlotB = -1;
    private EvolutionBranch? _branchA;
    private EvolutionBranch? _branchB;
    private WeaponItem? _sourceWeapon;
    private WeaponDefinition? _pendingEvolutionChoice;

    private WeaponDefinition? _hoveredDefinition;

    public void Draw(World world, WaveSystem waveSystem, Vector2 mousePos, Action onStartWave, Action onOpenSettings)
    {
        var equipments = world.GetStore<EquipmentComponent>();
        var inventories = world.GetStore<BioInventoryComponent>();
        var players = world.GetStore<PlayerComponent>();
        var statsStore = world.GetStore<PlayerStatsComponent>();
        var healthStore = world.GetStore<HealthComponent>();

        if (equipments == null || players == null) return;

        Entity playerEntity = default;
        bool hasPlayer = false;

        foreach (Entity p in world.Query().With<PlayerComponent>().With<EquipmentComponent>())
        {
            playerEntity = p;
            hasPlayer = true;
            break;
        }

        if (!hasPlayer) return;

        ref EquipmentComponent equip = ref equipments.Get(playerEntity);
        ref PlayerComponent player = ref players.Get(playerEntity);

        if (inventories != null && !inventories.Has(playerEntity))
        {
            world.AddComponent(playerEntity, new BioInventoryComponent());
        }

        ref BioInventoryComponent inventory = ref inventories!.Get(playerEntity);

        if (_lastWaveLoaded != waveSystem.CurrentWaveNumber)
        {
            _lastWaveLoaded = waveSystem.CurrentWaveNumber;
            _shopService.OnWaveCompleted(waveSystem.CurrentWaveNumber);
        }

        bool mouseClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);
        _hoveredDefinition = null;

        Raylib.DrawRectangle(0, 0, 1280, 720, new Color(8, 11, 16, 250));

        DrawTopBar(ref player, waveSystem, mousePos, mouseClicked, onOpenSettings);

        DrawOperatorStatsPanel(ref player, playerEntity, healthStore, statsStore);

        DrawNutrientShop(ref player, ref equip, ref inventory, waveSystem, mousePos, mouseClicked);

        DrawArsenalAndMutations(ref player, ref equip, ref inventory, mousePos, mouseClicked);

        DrawNextWaveButton(waveSystem, mousePos, mouseClicked, onStartWave);

        if (_isEvolutionModalOpen && _branchA != null && _branchB != null && _sourceWeapon != null)
        {
            DrawSplitEvolutionModal(ref equip, mousePos, mouseClicked);
        }
    }

    private static void DrawTopBar(ref PlayerComponent player, WaveSystem waveSystem, Vector2 mousePos, bool mouseClicked, Action onOpenSettings)
    {
        string title = "SOMNIUM RUPTURE - BIO-OPERATOR RECONSTITUTION";
        Raylib.DrawText(title, 30, 18, 13, new Color(90, 230, 205, 255));

        string waveText = $"PREPARATION FOR WAVE {waveSystem.CurrentWaveNumber}";
        Raylib.DrawText(waveText, 30, 36, 11, new Color(130, 155, 185, 255));

        Rectangle bioBox = new(870f, 16f, 240f, 32f);
        Raylib.DrawRectangleRec(bioBox, new Color(16, 32, 28, 255));
        Raylib.DrawRectangleLinesEx(bioBox, 1.2f, new Color(50, 210, 160, 255));

        string bioStr = $"BIOMASSA: {player.Biomass} BIO";
        int bw = Raylib.MeasureText(bioStr, 13);
        Raylib.DrawText(bioStr, (int)bioBox.X + ((int)bioBox.Width - bw) / 2, (int)bioBox.Y + 9, 13, new Color(80, 255, 210, 255));

        Rectangle settingsBtn = new(1130f, 16f, 120f, 32f);
        bool setHover = Raylib.CheckCollisionPointRec(mousePos, settingsBtn);
        Raylib.DrawRectangleRec(settingsBtn, setHover ? new Color(28, 42, 60, 255) : new Color(18, 26, 38, 255));
        Raylib.DrawRectangleLinesEx(settingsBtn, 1f, setHover ? new Color(80, 230, 210, 255) : new Color(50, 70, 95, 200));

        string setText = "SETTINGS";
        int stw = Raylib.MeasureText(setText, 11);
        Raylib.DrawText(setText, (int)settingsBtn.X + ((int)settingsBtn.Width - stw) / 2, (int)settingsBtn.Y + 10, 11, setHover ? Color.White : new Color(160, 185, 210, 255));

        if (setHover && mouseClicked)
        {
            onOpenSettings();
        }
    }

    private static void DrawOperatorStatsPanel(ref PlayerComponent player, Entity playerEntity, ComponentStore<HealthComponent>? healthStore, ComponentStore<PlayerStatsComponent>? statsStore)
    {
        int px = 30;
        int py = 64;
        int pw = 260;
        int ph = 636;

        Raylib.DrawRectangle(px, py, pw, ph, new Color(13, 16, 24, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(px, py, pw, ph), 1.5f, new Color(34, 48, 70, 255));

        Raylib.DrawText("OPERATOR BIODATA", px + 16, py + 14, 12, new Color(80, 230, 205, 255));

        float hpCur = 100f;
        float hpMax = 100f;
        if (healthStore != null && healthStore.Has(playerEntity))
        {
            ref readonly HealthComponent hp = ref healthStore.Get(playerEntity);
            hpCur = hp.Current;
            hpMax = hp.Max;
        }

        int barY = py + 36;
        int barW = pw - 32;
        int barH = 16;
        float hpRatio = Math.Clamp(hpCur / Math.Max(1f, hpMax), 0f, 1f);

        Raylib.DrawRectangle(px + 16, barY, barW, barH, new Color(24, 30, 42, 255));
        Raylib.DrawRectangle(px + 16, barY, (int)(barW * hpRatio), barH, new Color(45, 195, 140, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(px + 16, barY, barW, barH), 1f, new Color(60, 90, 120, 255));

        string hpText = $"HP: {(int)hpCur} / {(int)hpMax}";
        int htw = Raylib.MeasureText(hpText, 10);
        Raylib.DrawText(hpText, px + 16 + (barW - htw) / 2, barY + 3, 10, Color.White);

        Raylib.DrawLine(px + 16, py + 62, px + pw - 16, py + 62, new Color(30, 42, 60, 255));

        int sy = py + 76;
        int rowH = 28;

        float crit = 0.05f;
        float critMult = 1.5f;
        float range = 1.0f;
        float dmg = 1.0f;
        float atkSpd = 1.0f;
        float regen = 0f;
        float dodge = 0f;
        float speed = 1.0f;

        if (statsStore != null && statsStore.Has(playerEntity))
        {
            ref readonly PlayerStatsComponent s = ref statsStore.Get(playerEntity);
            crit = s.CritChance;
            critMult = s.CritMultiplier;
            range = s.RangeMultiplier;
            dmg = s.DamageMultiplier;
            atkSpd = s.AttackSpeedMultiplier;
            regen = s.HealthRegen;
            dodge = s.DodgeChance;
            speed = s.MoveSpeedMultiplier;
        }

        DrawStatRow("HP REGEN", $"+{regen:0.0} /s", regen > 0f, px + 16, sy, barW); sy += rowH;
        DrawStatRow("% DAMAGE", $"{dmg * 100f - 100f:+0;-0;+0}%", dmg > 1.0f, px + 16, sy, barW); sy += rowH;
        DrawStatRow("ATTACK SPEED", $"{atkSpd * 100f - 100f:+0;-0;+0}%", atkSpd > 1.0f, px + 16, sy, barW); sy += rowH;
        DrawStatRow("CRIT CHANCE", $"{crit:P0}", crit > 0.05f, px + 16, sy, barW); sy += rowH;
        DrawStatRow("CRIT MULT", $"{critMult:0.0}x", critMult > 1.5f, px + 16, sy, barW); sy += rowH;
        DrawStatRow("RANGE", $"{range:0.00}x", range > 1.0f, px + 16, sy, barW); sy += rowH;
        DrawStatRow("DODGE CHANCE", $"{dodge:P0}", dodge > 0f, px + 16, sy, barW); sy += rowH;
        DrawStatRow("MOVE SPEED", $"{speed * 100f - 100f:+0;-0;+0}%", speed > 1.0f, px + 16, sy, barW); sy += rowH;

        Raylib.DrawLine(px + 16, sy + 6, px + pw - 16, sy + 6, new Color(30, 42, 60, 255));
        sy += 16;

        string sub = "Anatomy: 6 Bio-Weapon Slots\nInfinite Mutation Capacity";
        Raylib.DrawText(sub, px + 16, sy, 10, new Color(120, 140, 165, 255));
    }

    private static void DrawStatRow(string label, string value, bool isBoosted, int x, int y, int w)
    {
        Raylib.DrawText(label, x, y + 4, 10, new Color(140, 160, 185, 255));
        int vw = Raylib.MeasureText(value, 11);
        Color valCol = isBoosted ? new Color(80, 240, 190, 255) : new Color(200, 215, 235, 255);
        Raylib.DrawText(value, x + w - vw, y + 3, 11, valCol);
    }

    private void DrawNutrientShop(ref PlayerComponent player, ref EquipmentComponent equip, ref BioInventoryComponent inventory, WaveSystem waveSystem, Vector2 mousePos, bool mouseClicked)
    {
        int sx = 310;
        int sy = 64;
        int sw = 540;
        int sh = 550;

        Raylib.DrawRectangle(sx, sy, sw, sh, new Color(12, 15, 22, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(sx, sy, sw, sh), 1.5f, new Color(34, 48, 70, 255));

        Raylib.DrawText("NUTRIENT INCUBATOR (SHOP)", sx + 20, sy + 14, 12, new Color(80, 230, 205, 255));
        Raylib.DrawText("Purchase active armaments or cellular tissue upgrades", sx + 20, sy + 30, 10, new Color(125, 145, 170, 255));

        int cardW = 162;
        int cardH = 410;
        int cardY = sy + 52;
        int gap = 14;

        for (int i = 0; i < ShopService.CardCount; i++)
        {
            int cardX = sx + 18 + i * (cardW + gap);
            DrawShopCard(i, cardX, cardY, cardW, cardH, ref player, ref equip, ref inventory, mousePos, mouseClicked);
        }

        int rerollCost = _shopService.GetCurrentRerollCost(waveSystem.CurrentWaveNumber);
        Rectangle rerollBtn = new(sx + (sw - 280) / 2, sy + sh - 68, 280, 42);
        bool canReroll = player.Biomass >= rerollCost;
        bool rerollHover = Raylib.CheckCollisionPointRec(mousePos, rerollBtn);

        Color rBg = canReroll ? (rerollHover ? new Color(35, 55, 80, 255) : new Color(22, 34, 52, 255))
                              : new Color(18, 22, 30, 255);
        Color rBorder = canReroll ? (rerollHover ? new Color(100, 220, 255, 255) : new Color(60, 140, 200, 255))
                                  : new Color(40, 50, 65, 255);

        Raylib.DrawRectangleRec(rerollBtn, rBg);
        Raylib.DrawRectangleLinesEx(rerollBtn, 1.5f, rBorder);

        string rerollTxt = $"REROLL SHOP ({rerollCost} BIO)";
        int rtw = Raylib.MeasureText(rerollTxt, 11);
        Raylib.DrawText(rerollTxt, (int)rerollBtn.X + ((int)rerollBtn.Width - rtw) / 2, (int)rerollBtn.Y + 15, 11, canReroll ? Color.White : new Color(100, 115, 135, 255));

        if (canReroll && rerollHover && mouseClicked)
        {
            int b = player.Biomass;
            _shopService.Reroll(ref b, waveSystem.CurrentWaveNumber);
            player.Biomass = b;
        }
    }

    private void DrawShopCard(
        int index,
        int cx, int cy, int cw, int ch,
        ref PlayerComponent player,
        ref EquipmentComponent equip,
        ref BioInventoryComponent inventory,
        Vector2 mousePos,
        bool mouseClicked)
    {
        var card = _shopService.Cards[index];
        Rectangle cardRect = new(cx, cy, cw, ch);
        bool isHovered = Raylib.CheckCollisionPointRec(mousePos, cardRect);

        if (card.IsPurchased)
        {
            Raylib.DrawRectangleRec(cardRect, new Color(16, 20, 28, 160));
            Raylib.DrawRectangleLinesEx(cardRect, 1.2f, new Color(40, 50, 65, 180));
            string soldTxt = "ABSORBED";
            int stw = Raylib.MeasureText(soldTxt, 12);
            Raylib.DrawText(soldTxt, cx + (cw - stw) / 2, cy + ch / 2 - 10, 12, new Color(90, 110, 130, 255));
            return;
        }

        var def = card.Definition;
        if (def == null) return;

        if (isHovered)
        {
            _hoveredDefinition = def;
        }

        Color border = card.IsLocked ? new Color(245, 180, 50, 255)
                                     : (isHovered ? new Color(80, 220, 200, 255) : new Color(38, 52, 74, 255));

        Raylib.DrawRectangleRec(cardRect, new Color(16, 20, 28, 255));
        Raylib.DrawRectangleLinesEx(cardRect, card.IsLocked ? 1.8f : 1.2f, border);

        int ty = cy + 10;
        string name = def.Name;
        if (name.Length > 18) name = name.Substring(0, 16) + "..";
        Raylib.DrawText(name, cx + 10, ty, 11, Color.White);
        ty += 18;

        bool isWeapon = def.Type != WeaponType.Passive;
        string typeLabel = isWeapon ? "WEAPON [T-I]" : "PASSIVE [T-I]";
        Color typeCol = isWeapon ? new Color(70, 215, 185, 255) : new Color(190, 130, 245, 255);
        Raylib.DrawText(typeLabel, cx + 10, ty, 9, typeCol);
        ty += 16;

        DrawCardTagBadges(def.Keywords, cx + 10, ty, cw - 20);
        ty += 24;

        Raylib.DrawLine(cx + 8, ty, cx + cw - 8, ty, new Color(28, 38, 54, 255));
        ty += 8;

        if (isWeapon)
        {
            DrawBulletStat("DMG", $"{def.BaseDamage:0.0}", cx + 10, ref ty);
            DrawBulletStat("COOLDOWN", $"{def.AttackInterval:0.00}s", cx + 10, ref ty);
            DrawBulletStat("RANGE", $"{def.Range:0} px", cx + 10, ref ty);
            if (def.ProjectileCount > 1) DrawBulletStat("PROJECTILES", $"{def.ProjectileCount}x", cx + 10, ref ty);
            if (def.PiercingHits > 1) DrawBulletStat("PIERCING", $"{def.PiercingHits} Hits", cx + 10, ref ty);
            if (def.Knockback > 0f) DrawBulletStat("KNOCKBACK", $"{def.Knockback:0}", cx + 10, ref ty);
            if (def.Lifesteal > 0f) DrawBulletStat("LIFESTEAL", $"{def.Lifesteal:P0}", cx + 10, ref ty);
        }
        else
        {
            if (def.CritChanceBonus > 0f) DrawBulletStat("CRIT CHANCE", $"+{def.CritChanceBonus:P0}", cx + 10, ref ty);
            if (def.DamageMultiplierBonus > 0f) DrawBulletStat("ALL DAMAGE", $"+{def.DamageMultiplierBonus:P0}", cx + 10, ref ty);
            if (def.HealthRegenBonus > 0f) DrawBulletStat("HP REGEN", $"+{def.HealthRegenBonus:0.0}/s", cx + 10, ref ty);
            if (def.RangeMultiplierBonus > 0f) DrawBulletStat("RANGE", $"+{def.RangeMultiplierBonus:P0}", cx + 10, ref ty);
            if (def.DodgeChanceBonus > 0f) DrawBulletStat("DODGE", $"+{def.DodgeChanceBonus:P0}", cx + 10, ref ty);
            if (def.MoveSpeedMultiplierBonus > 0f) DrawBulletStat("SPEED", $"+{def.MoveSpeedMultiplierBonus:P0}", cx + 10, ref ty);
            if ((def.Keywords & WeaponKeywords.Berserk) != 0) DrawBulletStat("TRAIT", "[Berserk]", cx + 10, ref ty);
            if ((def.Keywords & WeaponKeywords.Stationary) != 0) DrawBulletStat("TRAIT", "[Rooted]", cx + 10, ref ty);
            if ((def.Keywords & WeaponKeywords.Burst) != 0) DrawBulletStat("TRAIT", "[Burst Dash]", cx + 10, ref ty);
        }

        int btnY = cy + ch - 72;
        string priceTxt = $"{card.Price} BIOMASSA";
        int ptw = Raylib.MeasureText(priceTxt, 11);
        Raylib.DrawText(priceTxt, cx + (cw - ptw) / 2, btnY - 14, 11, new Color(80, 240, 210, 255));

        Rectangle buyBtn = new(cx + 8, btnY + 4, cw - 16, 28);
        bool canAfford = player.Biomass >= card.Price;
        bool isFullWeapons = isWeapon && equip.CountEquippedWeapons() >= EquipmentComponent.MaxWeaponSlots;
        bool canBuy = canAfford && !isFullWeapons;
        bool buyHover = Raylib.CheckCollisionPointRec(mousePos, buyBtn);

        Color buyBg = canBuy ? (buyHover ? new Color(30, 160, 125, 255) : new Color(20, 120, 95, 255))
                             : new Color(30, 36, 48, 255);
        Raylib.DrawRectangleRec(buyBtn, buyBg);
        Raylib.DrawRectangleLinesEx(buyBtn, 1f, canBuy ? new Color(70, 230, 180, 255) : new Color(50, 60, 75, 255));

        string buyLabel = isFullWeapons ? "WEAPONS FULL" : (canAfford ? "BUY ITEM" : "INSUFFICIENT BIO");
        int btw = Raylib.MeasureText(buyLabel, 9);
        Raylib.DrawText(buyLabel, (int)buyBtn.X + ((int)buyBtn.Width - btw) / 2, (int)buyBtn.Y + 9, 9, canBuy ? Color.White : new Color(130, 145, 165, 255));

        if (canBuy && buyHover && mouseClicked)
        {
            int b = player.Biomass;
            _shopService.BuyItem(index, ref b, ref equip, ref inventory);
            player.Biomass = b;
        }

        Rectangle lockBtn = new(cx + 8, btnY + 36, cw - 16, 24);
        bool lockHover = Raylib.CheckCollisionPointRec(mousePos, lockBtn);
        Color lockBg = card.IsLocked ? new Color(80, 60, 20, 255)
                                     : (lockHover ? new Color(30, 38, 52, 255) : new Color(18, 24, 34, 255));
        Color lockBorder = card.IsLocked ? new Color(245, 185, 50, 255) : new Color(45, 58, 78, 255);

        Raylib.DrawRectangleRec(lockBtn, lockBg);
        Raylib.DrawRectangleLinesEx(lockBtn, 1f, lockBorder);

        string lockLabel = card.IsLocked ? "[LOCKED]" : "LOCK";
        int ltw = Raylib.MeasureText(lockLabel, 9);
        Raylib.DrawText(lockLabel, (int)lockBtn.X + ((int)lockBtn.Width - ltw) / 2, (int)lockBtn.Y + 7, 9, card.IsLocked ? new Color(255, 210, 80, 255) : new Color(140, 160, 180, 255));

        if (lockHover && mouseClicked)
        {
            _shopService.ToggleLock(index);
        }
    }

    private static void DrawBulletStat(string label, string val, int x, ref int y)
    {
        Raylib.DrawCircle(x + 2, y + 5, 2f, new Color(70, 210, 180, 255));
        Raylib.DrawText(label, x + 8, y, 9, new Color(140, 160, 185, 255));
        int vw = Raylib.MeasureText(val, 9);
        Raylib.DrawText(val, x + 142 - vw, y, 9, Color.White);
        y += 14;
    }

    private static void DrawCardTagBadges(WeaponKeywords keywords, int x, int y, int maxW)
    {
        int curX = x;
        var list = new[]
        {
            (WeaponKeywords.Bio, "BIO", new Color(60, 220, 160, 255)),
            (WeaponKeywords.Blade, "BLADE", new Color(220, 140, 255, 255)),
            (WeaponKeywords.Rapid, "RAPID", new Color(100, 220, 255, 255)),
            (WeaponKeywords.Heavy, "HEAVY", new Color(245, 175, 75, 255)),
            (WeaponKeywords.Pierce, "PIERCE", new Color(255, 100, 120, 255)),
            (WeaponKeywords.Chain, "CHAIN", new Color(80, 255, 230, 255)),
            (WeaponKeywords.Berserk, "BERSERK", new Color(255, 60, 80, 255)),
            (WeaponKeywords.Stationary, "ROOTED", new Color(240, 220, 180, 255)),
            (WeaponKeywords.Burst, "BURST", new Color(120, 255, 220, 255)),
            (WeaponKeywords.Crit, "CRIT", new Color(160, 220, 255, 255))
        };

        foreach (var t in list)
        {
            if ((keywords & t.Item1) != 0)
            {
                int pw = Raylib.MeasureText(t.Item2, 8) + 8;
                if (curX + pw > x + maxW) break;

                Rectangle r = new(curX, y, pw, 14);
                Raylib.DrawRectangleRec(r, new Color(20, 26, 36, 255));
                Raylib.DrawRectangleLinesEx(r, 1f, t.Item3);
                Raylib.DrawText(t.Item2, curX + 4, y + 3, 8, t.Item3);
                curX += pw + 4;
            }
        }
    }

    private void DrawArsenalAndMutations(ref PlayerComponent player, ref EquipmentComponent equip, ref BioInventoryComponent inventory, Vector2 mousePos, bool mouseClicked)
    {
        int ax = 870;
        int ay = 64;
        int aw = 380;
        int ah = 550;

        Raylib.DrawRectangle(ax, ay, aw, ah, new Color(12, 15, 22, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(ax, ay, aw, ah), 1.5f, new Color(34, 48, 70, 255));

        int eqCount = equip.CountEquippedWeapons();
        Raylib.DrawText($"WEAPON ARSENAL ({eqCount}/6)", ax + 16, ay + 14, 12, new Color(80, 230, 205, 255));

        bool canMergeAny = equip.CanMergeAny(out int mergeSlotA, out int mergeSlotB);
        if (canMergeAny)
        {
            Rectangle mergeAllBtn = new(ax + aw - 140, ay + 10, 124, 22);
            bool mHover = Raylib.CheckCollisionPointRec(mousePos, mergeAllBtn);
            Raylib.DrawRectangleRec(mergeAllBtn, mHover ? new Color(40, 160, 120, 255) : new Color(24, 110, 80, 255));
            Raylib.DrawRectangleLinesEx(mergeAllBtn, 1.2f, new Color(80, 255, 200, 255));

            string mText = "SPLIT EVOLVE!";
            int mtw = Raylib.MeasureText(mText, 10);
            Raylib.DrawText(mText, (int)mergeAllBtn.X + ((int)mergeAllBtn.Width - mtw) / 2, (int)mergeAllBtn.Y + 6, 10, Color.White);

            if (mHover && mouseClicked)
            {
                TriggerSplitEvolutionModal(ref equip, mergeSlotA, mergeSlotB);
            }
        }

        int slotW = 110;
        int slotH = 76;
        int slotStartX = ax + 16;
        int slotStartY = ay + 38;
        int gap = 8;

        for (int i = 0; i < EquipmentComponent.MaxWeaponSlots; i++)
        {
            int col = i % 3;
            int row = i / 3;
            int sx = slotStartX + col * (slotW + gap);
            int sy = slotStartY + row * (slotH + gap);

            DrawWeaponSlotBox(i, sx, sy, slotW, slotH, ref player, ref equip, canMergeAny && (i == mergeSlotA || i == mergeSlotB), mousePos, mouseClicked);
        }

        int trayY = ay + 215;
        Raylib.DrawLine(ax + 14, trayY, ax + aw - 14, trayY, new Color(30, 42, 60, 255));
        trayY += 12;

        int totalPassives = inventory.Count;
        Raylib.DrawText($"MUTATIONS & ORGANS ({totalPassives})", ax + 16, trayY, 11, new Color(80, 230, 205, 255));
        trayY += 22;

        var stacked = inventory.GetStackedItems();
        int chipSize = 44;
        int chipGap = 8;
        int maxCols = 6;

        for (int i = 0; i < stacked.Count; i++)
        {
            int c = i % maxCols;
            int r = i / maxCols;
            int chipX = ax + 16 + c * (chipSize + chipGap);
            int chipY = trayY + r * (chipSize + chipGap);

            Rectangle chipRect = new(chipX, chipY, chipSize, chipSize);
            bool chipHover = Raylib.CheckCollisionPointRec(mousePos, chipRect);

            var item = stacked[i].Sample;
            int count = stacked[i].Count;

            Color chipBg = chipHover ? new Color(30, 42, 60, 255) : new Color(18, 22, 32, 255);
            Raylib.DrawRectangleRec(chipRect, chipBg);
            Raylib.DrawRectangleLinesEx(chipRect, 1.2f, chipHover ? new Color(80, 230, 200, 255) : new Color(42, 56, 78, 255));

            string initials = item.Definition.Name.Length >= 2 ? item.Definition.Name.Substring(0, 2).ToUpper() : "MU";
            int itw = Raylib.MeasureText(initials, 11);
            Raylib.DrawText(initials, chipX + (chipSize - itw) / 2, chipY + 12, 11, new Color(190, 210, 235, 255));

            if (count > 1)
            {
                string countStr = $"x{count}";
                int ctw = Raylib.MeasureText(countStr, 9);
                Raylib.DrawText(countStr, chipX + chipSize - ctw - 3, chipY + chipSize - 12, 9, new Color(70, 240, 190, 255));
            }

            if (chipHover)
            {
                _hoveredDefinition = item.Definition;
            }
        }

        if (_hoveredDefinition != null)
        {
            DrawHoveredTooltip(ax + 16, ay + ah - 110, aw - 32, 96, _hoveredDefinition);
        }
    }

    private void DrawWeaponSlotBox(
        int slotIndex,
        int x, int y, int w, int h,
        ref PlayerComponent player,
        ref EquipmentComponent equip,
        bool isMatchingPair,
        Vector2 mousePos,
        bool mouseClicked)
    {
        Rectangle rect = new(x, y, w, h);
        bool isHovered = Raylib.CheckCollisionPointRec(mousePos, rect);
        var weapon = equip.Slots[slotIndex].Weapon;

        if (weapon == null)
        {
            Raylib.DrawRectangleRec(rect, new Color(16, 20, 28, 180));
            Raylib.DrawRectangleLinesEx(rect, 1f, new Color(34, 44, 60, 180));
            string emptyTxt = $"EMPTY [{slotIndex + 1}]";
            int etw = Raylib.MeasureText(emptyTxt, 9);
            Raylib.DrawText(emptyTxt, x + (w - etw) / 2, y + h / 2 - 5, 9, new Color(80, 95, 115, 255));
            return;
        }

        if (isHovered)
        {
            _hoveredDefinition = weapon.Definition;
        }

        Color border = isMatchingPair ? new Color(60, 255, 190, 255)
                                      : (isHovered ? new Color(90, 220, 210, 255) : new Color(42, 58, 80, 255));

        Raylib.DrawRectangleRec(rect, new Color(18, 24, 34, 255));
        Raylib.DrawRectangleLinesEx(rect, isMatchingPair ? 1.6f : 1.2f, border);

        string name = weapon.Definition.Name;
        if (name.Length > 12) name = name.Substring(0, 10) + "..";
        Raylib.DrawText(name, x + 6, y + 6, 10, Color.White);

        string tier = ToRoman(weapon.Tier);
        int ttw = Raylib.MeasureText(tier, 9);
        Raylib.DrawText(tier, x + w - ttw - 6, y + 6, 9, new Color(70, 230, 190, 255));

        string statsSnippet = $"{weapon.Definition.BaseDamage:0} DMG | {weapon.Definition.AttackInterval:0.0}s";
        Raylib.DrawText(statsSnippet, x + 6, y + 24, 9, new Color(140, 160, 185, 255));

        Rectangle recycleBtn = new(x + 4, y + h - 22, w - 8, 18);
        bool recHover = Raylib.CheckCollisionPointRec(mousePos, recycleBtn);
        int sellValue = Math.Max(8, (int)(22 * weapon.Tier * 0.70f));

        Raylib.DrawRectangleRec(recycleBtn, recHover ? new Color(60, 28, 34, 255) : new Color(32, 20, 26, 255));
        Raylib.DrawRectangleLinesEx(recycleBtn, 1f, recHover ? new Color(240, 90, 100, 255) : new Color(80, 40, 50, 255));

        string sellTxt = $"SELL +{sellValue}";
        int stw = Raylib.MeasureText(sellTxt, 8);
        Raylib.DrawText(sellTxt, (int)recycleBtn.X + ((int)recycleBtn.Width - stw) / 2, (int)recycleBtn.Y + 5, 8, recHover ? Color.White : new Color(200, 140, 150, 255));

        if (recHover && mouseClicked)
        {
            equip.Unequip(slotIndex);
            player.Biomass += sellValue;
        }
    }

    private static void DrawHoveredTooltip(int x, int y, int w, int h, WeaponDefinition def)
    {
        Rectangle box = new(x, y, w, h);
        Raylib.DrawRectangleRec(box, new Color(14, 18, 26, 250));
        Raylib.DrawRectangleLinesEx(box, 1.2f, new Color(70, 210, 180, 255));

        string name = def.Name;
        Raylib.DrawText(name, x + 10, y + 8, 11, Color.White);

        string tags = $"Keywords: {def.Keywords}";
        Raylib.DrawText(tags, x + 10, y + 24, 9, new Color(80, 220, 200, 255));

        string desc = !string.IsNullOrWhiteSpace(def.Description) ? def.Description : "Cellular biopunk implant modifying host physiology.";
        if (desc.Length > 90) desc = desc.Substring(0, 87) + "...";
        Raylib.DrawText(desc, x + 10, y + 42, 9, new Color(160, 180, 205, 255));

        string statsSum = def.Type == WeaponType.Passive
            ? $"Regen: +{def.HealthRegenBonus:0.0} | Crit: +{def.CritChanceBonus:P0} | Dodge: +{def.DodgeChanceBonus:P0}"
            : $"Base DMG: {def.BaseDamage:0.0} | CD: {def.AttackInterval:0.00}s | Pierce: {def.PiercingHits}";
        Raylib.DrawText(statsSum, x + 10, y + 72, 9, new Color(90, 240, 210, 255));
    }

    private void DrawNextWaveButton(WaveSystem waveSystem, Vector2 mousePos, bool mouseClicked, Action onStartWave)
    {
        Rectangle nextBtn = new(310f, 630f, 540f, 56f);
        bool btnHover = Raylib.CheckCollisionPointRec(mousePos, nextBtn);

        Color bg = btnHover ? new Color(28, 170, 130, 255) : new Color(18, 125, 95, 255);
        Raylib.DrawRectangleRec(nextBtn, bg);
        Raylib.DrawRectangleLinesEx(nextBtn, 2f, new Color(80, 255, 210, 255));

        string text = $"START WAVE {waveSystem.CurrentWaveNumber}  [SPACE]";
        int tw = Raylib.MeasureText(text, 15);
        Raylib.DrawText(text, (int)nextBtn.X + ((int)nextBtn.Width - tw) / 2, (int)nextBtn.Y + 20, 15, Color.White);

        if ((btnHover && mouseClicked) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            onStartWave();
        }
    }

    private void TriggerSplitEvolutionModal(ref EquipmentComponent equip, int slotA, int slotB)
    {
        var item = equip.Slots[slotA].Weapon;
        if (item == null) return;

        _evolutionSlotA = slotA;
        _evolutionSlotB = slotB;
        _sourceWeapon = item;

        var branches = EvolutionDatabase.GetEvolutionBranches(item);
        _branchA = branches.BranchA;
        _branchB = branches.BranchB;
        _isEvolutionModalOpen = true;
    }

    private void DrawSplitEvolutionModal(ref EquipmentComponent equip, Vector2 mousePos, bool mouseClicked)
    {
        Raylib.DrawRectangle(0, 0, 1280, 720, new Color(6, 8, 12, 235));

        int mw = 740;
        int mh = 430;
        int mx = (1280 - mw) / 2;
        int my = (720 - mh) / 2;

        Rectangle modalBox = new(mx, my, mw, mh);
        Raylib.DrawRectangleRec(modalBox, new Color(14, 18, 26, 255));
        Raylib.DrawRectangleLinesEx(modalBox, 2f, new Color(70, 230, 195, 255));

        string mTitle = "CELLULAR DIVERGENCE: SPLIT EVOLUTION";
        int mtw = Raylib.MeasureText(mTitle, 16);
        Raylib.DrawText(mTitle, mx + (mw - mtw) / 2, my + 24, 16, new Color(80, 255, 215, 255));

        string mSub = $"Merging 2x [{_sourceWeapon!.Definition.Name} {ToRoman(_sourceWeapon.Tier)}] -> Choose Cellular Evolution Branch:";
        int msw = Raylib.MeasureText(mSub, 11);
        Raylib.DrawText(mSub, mx + (mw - msw) / 2, my + 50, 11, new Color(140, 165, 195, 255));

        int cardW = 320;
        int cardH = 260;
        int cardY = my + 85;

        _pendingEvolutionChoice = null;

        int cardAX = mx + 35;
        DrawEvolutionBranchCard(cardAX, cardY, cardW, cardH, _branchA!, mousePos, mouseClicked, () =>
        {
            _pendingEvolutionChoice = _branchA!.Definition;
        });

        int cardBX = mx + mw - 35 - cardW;
        DrawEvolutionBranchCard(cardBX, cardY, cardW, cardH, _branchB!, mousePos, mouseClicked, () =>
        {
            _pendingEvolutionChoice = _branchB!.Definition;
        });

        if (_pendingEvolutionChoice != null)
        {
            CompleteEvolution(ref equip, _pendingEvolutionChoice);
            _pendingEvolutionChoice = null;
        }

        Rectangle cancelBtn = new(mx + (mw - 160) / 2, my + mh - 50, 160, 30);
        bool cHover = Raylib.CheckCollisionPointRec(mousePos, cancelBtn);
        Raylib.DrawRectangleRec(cancelBtn, cHover ? new Color(45, 55, 75, 255) : new Color(24, 30, 44, 255));
        Raylib.DrawRectangleLinesEx(cancelBtn, 1f, cHover ? Color.White : new Color(60, 80, 105, 255));

        string cTxt = "CANCEL MERGE";
        int ctw = Raylib.MeasureText(cTxt, 11);
        Raylib.DrawText(cTxt, (int)cancelBtn.X + ((int)cancelBtn.Width - ctw) / 2, (int)cancelBtn.Y + 9, 11, cHover ? Color.White : new Color(160, 180, 205, 255));

        if ((cHover && mouseClicked) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _isEvolutionModalOpen = false;
        }
    }

    private static void DrawEvolutionBranchCard(int x, int y, int w, int h, EvolutionBranch branch, Vector2 mousePos, bool mouseClicked, Action onChoose)
    {
        Rectangle box = new(x, y, w, h);
        bool hovered = Raylib.CheckCollisionPointRec(mousePos, box);

        Color border = hovered ? new Color(80, 240, 200, 255) : new Color(40, 58, 80, 255);
        Raylib.DrawRectangleRec(box, new Color(18, 24, 34, 255));
        Raylib.DrawRectangleLinesEx(box, hovered ? 1.8f : 1.2f, border);

        Raylib.DrawText(branch.BranchTitle, x + 16, y + 16, 12, new Color(80, 240, 210, 255));

        string name = branch.Definition.Name;
        Raylib.DrawText(name, x + 16, y + 36, 11, Color.White);

        string tags = $"Synergy: {branch.Definition.Keywords}";
        Raylib.DrawText(tags, x + 16, y + 54, 9, new Color(130, 155, 185, 255));

        Raylib.DrawLine(x + 14, y + 74, x + w - 14, y + 74, new Color(30, 42, 60, 255));

        int ty = y + 86;
        string[] lines = branch.SummaryText.Split('|', StringSplitOptions.TrimEntries);
        foreach (var l in lines)
        {
            Raylib.DrawCircle(x + 18, ty + 5, 2.5f, new Color(60, 220, 170, 255));
            Raylib.DrawText(l, x + 26, ty, 10, new Color(210, 230, 250, 255));
            ty += 18;
        }

        string flavor = branch.FlavorText;
        if (flavor.Length > 70) flavor = flavor.Substring(0, 67) + "..";
        Raylib.DrawText(flavor, x + 16, y + 165, 9, new Color(130, 150, 175, 255));

        Rectangle chooseBtn = new(x + 16, y + h - 46, w - 32, 32);
        bool btnHover = Raylib.CheckCollisionPointRec(mousePos, chooseBtn);
        Raylib.DrawRectangleRec(chooseBtn, btnHover ? new Color(30, 160, 125, 255) : new Color(20, 120, 95, 255));
        Raylib.DrawRectangleLinesEx(chooseBtn, 1.2f, btnHover ? Color.White : new Color(80, 240, 200, 255));

        string btnTxt = "SELECT MUTATION";
        int btw = Raylib.MeasureText(btnTxt, 11);
        Raylib.DrawText(btnTxt, (int)chooseBtn.X + ((int)chooseBtn.Width - btw) / 2, (int)chooseBtn.Y + 10, 11, Color.White);

        if ((btnHover && mouseClicked) || (hovered && mouseClicked))
        {
            onChoose();
        }
    }

    private void CompleteEvolution(ref EquipmentComponent equip, WeaponDefinition chosenDef)
    {
        if (_evolutionSlotA >= 0 && _evolutionSlotA < equip.Slots.Length &&
            _evolutionSlotB >= 0 && _evolutionSlotB < equip.Slots.Length &&
            _sourceWeapon != null)
        {
            equip.Slots[_evolutionSlotA].Weapon = new WeaponItem(chosenDef, _sourceWeapon.Tier + 1);
            equip.Slots[_evolutionSlotB].Weapon = null;
        }

        _isEvolutionModalOpen = false;
        _branchA = null;
        _branchB = null;
        _sourceWeapon = null;
        _evolutionSlotA = -1;
        _evolutionSlotB = -1;
    }

    private static string ToRoman(int number)
    {
        return number switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => number.ToString()
        };
    }
}
