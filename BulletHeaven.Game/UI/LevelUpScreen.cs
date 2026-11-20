using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game.UI;

public sealed class LevelUpCard
{
    public string WeaponId { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public bool IsNewWeapon { get; init; }
    public bool IsUpgrade { get; init; }
    public bool IsFallback { get; init; }
    public int SlotIndex { get; init; } = -1;
    public int CurrentLevel { get; init; }
    public int NextLevel { get; init; } = 1;
    public WeaponDefinition? Definition { get; init; }
    public string BadgeText { get; init; } = "";
    public string DeltaLine1 { get; init; } = "";
    public string DeltaLine2 { get; init; } = "";
    public string DeltaLine3 { get; init; } = "";
    public Color AccentColor { get; init; } = new(80, 235, 215, 255);
}

public sealed class LevelUpScreen
{
    private readonly List<LevelUpCard> _cards = new(3);

    public IReadOnlyList<LevelUpCard> Cards => _cards;

    public void Clear()
    {
        _cards.Clear();
    }

    public IReadOnlyList<LevelUpCard> GenerateChoices(World world, Entity player)
    {
        _cards.Clear();
        if (!world.IsAlive(player) || !world.HasComponent<EquipmentComponent>(player))
        {
            return _cards;
        }

        ref readonly EquipmentComponent equip = ref world.GetComponent<EquipmentComponent>(player);
        int equippedCount = equip.CountEquippedWeapons();
        var allDefs = WeaponDatabase.GetAll();

        if (equippedCount < EquipmentComponent.MaxWeaponSlots)
        {
            var unequipped = new List<WeaponDefinition>();
            var upgradeable = new List<(int SlotIndex, WeaponItem Weapon)>();

            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (equip.FindSlotByWeaponId(def.Id) == -1)
                {
                    unequipped.Add(def);
                }
            }

            if (equip.Slots != null)
            {
                for (int s = 0; s < equip.Slots.Length; s++)
                {
                    var weapon = equip.Slots[s].Weapon;
                    if (weapon != null && weapon.CanUpgrade)
                    {
                        upgradeable.Add((s, weapon));
                    }
                }
            }

            Shuffle(unequipped);
            Shuffle(upgradeable);

            int nextOpenSlot = equip.FirstEmptySlotIndex();
            var picked = new List<LevelUpCard>(3);

            // Guarantee at least 1 unequipped new weapon card whenever < 3 slots are filled
            if (unequipped.Count > 0)
            {
                picked.Add(CreateNewWeaponCard(unequipped[0], nextOpenSlot));
            }

            int uIdx = 1;
            int upgIdx = 0;

            // Offer 1 upgrade if available and space remains, then fill remaining slots with unequipped weapons
            if (picked.Count < 3 && upgradeable.Count > 0 && unequipped.Count >= 2)
            {
                // Mix 2 new weapons + 1 upgrade (or all new if no upgradeable)
                picked.Add(CreateNewWeaponCard(unequipped[uIdx++], nextOpenSlot));
                var u = upgradeable[upgIdx++];
                picked.Add(CreateUpgradeCard(u.SlotIndex, u.Weapon));
            }

            while (picked.Count < 3 && uIdx < unequipped.Count)
            {
                picked.Add(CreateNewWeaponCard(unequipped[uIdx++], nextOpenSlot));
            }

            while (picked.Count < 3 && upgIdx < upgradeable.Count)
            {
                var u = upgradeable[upgIdx++];
                picked.Add(CreateUpgradeCard(u.SlotIndex, u.Weapon));
            }

            Shuffle(picked);
            _cards.AddRange(picked);
        }
        else
        {
            // 3/3 slots filled: NEVER offer unequipped weapons; offer upgrades (or fallback if Lv.5 maxed)
            var slotCards = new List<LevelUpCard>(3);
            if (equip.Slots != null)
            {
                for (int s = 0; s < equip.Slots.Length; s++)
                {
                    var weapon = equip.Slots[s].Weapon;
                    if (weapon == null) continue;

                    if (weapon.CanUpgrade)
                    {
                        slotCards.Add(CreateUpgradeCard(s, weapon));
                    }
                    else
                    {
                        slotCards.Add(CreateFallbackCard(s, weapon));
                    }
                }
            }

            Shuffle(slotCards);
            for (int i = 0; i < Math.Min(3, slotCards.Count); i++)
            {
                _cards.Add(slotCards[i]);
            }
        }

        return _cards;
    }

    public bool ApplyChoice(World world, Entity player, int index)
    {
        if (index < 0 || index >= _cards.Count) return false;
        if (!world.IsAlive(player) || !world.HasComponent<EquipmentComponent>(player)) return false;

        LevelUpCard card = _cards[index];
        ref EquipmentComponent equip = ref world.GetComponent<EquipmentComponent>(player);

        if (card.IsNewWeapon)
        {
            if (equip.CountEquippedWeapons() >= EquipmentComponent.MaxWeaponSlots) return false;
            WeaponItem? item = WeaponDatabase.CreateItem(card.WeaponId, level: 1);
            if (item == null) return false;
            return equip.EquipFirstAvailable(item);
        }

        if (card.IsUpgrade)
        {
            int slotIdx = card.SlotIndex;
            if (slotIdx < 0 || equip.Slots == null || slotIdx >= equip.Slots.Length ||
                equip.Slots[slotIdx].Weapon?.Definition.Id != card.WeaponId)
            {
                slotIdx = equip.FindSlotByWeaponId(card.WeaponId);
            }

            if (slotIdx < 0 || equip.Slots == null) return false;
            WeaponItem? weapon = equip.Slots[slotIdx].Weapon;
            if (weapon == null || !weapon.CanUpgrade) return false;

            return weapon.ApplyUpgrade();
        }

        // Max-level fallback bio-surge
        if (world.HasComponent<HealthComponent>(player))
        {
            ref HealthComponent hp = ref world.GetComponent<HealthComponent>(player);
            hp.Max += 20f;
            hp.Current = MathF.Min(hp.Max, hp.Current + 25f);
        }
        return true;
    }

    public int DrawStartChoice(Vector2 virtualMouse, bool inputEnabled = true)
    {
        Raylib.DrawRectangle(0, 0, 1280, 720, new Color(6, 8, 14, 212));

        string title = "CHOOSE YOUR STARTING WEAPON";
        int titleW = Raylib.MeasureText(title, 26);
        Raylib.DrawText(title, (1280 - titleW) / 2, 92, 26, new Color(80, 235, 215, 255));

        string subtitle = "Select 1 of 6 bio-weapons to start Wave 1  -  Click a card or press [1]-[6]";
        int subW = Raylib.MeasureText(subtitle, 14);
        Raylib.DrawText(subtitle, (1280 - subW) / 2, 124, 14, new Color(165, 185, 210, 255));

        var defs = WeaponDatabase.GetAll();
        bool mouseClicked = inputEnabled && Raylib.IsMouseButtonPressed(MouseButton.Left);
        int selected = -1;

        const float cardW = 360f;
        const float cardH = 228f;
        const float gapX = 28f;
        const float gapY = 20f;
        const float startX = 72f;
        const float startY = 152f;

        for (int i = 0; i < defs.Count && i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;
            Rectangle card = new(startX + col * (cardW + gapX), startY + row * (cardH + gapY), cardW, cardH);
            bool hovered = inputEnabled && Raylib.CheckCollisionPointRec(virtualMouse, card);

            var def = defs[i];
            Color bg = hovered ? new Color(20, 27, 40, 252) : new Color(13, 17, 26, 245);
            Color border = hovered ? def.Color : new Color(46, 62, 88, 225);

            Raylib.DrawRectangleRounded(card, 0.04f, 6, bg);
            Raylib.DrawRectangleRoundedLinesEx(card, 0.04f, 6, hovered ? 2.2f : 1.4f, border);
            Raylib.DrawRectangle((int)card.X + 2, (int)card.Y, (int)card.Width - 4, 4, def.Color);

            // Key badge [1]-[6]
            Rectangle keyBadge = new(card.X + 16f, card.Y + 14f, 34f, 24f);
            Raylib.DrawRectangleRec(keyBadge, hovered ? def.Color : new Color(26, 34, 48, 255));
            Raylib.DrawRectangleLinesEx(keyBadge, 1f, def.Color);
            string keyLabel = $"[{i + 1}]";
            int kw = Raylib.MeasureText(keyLabel, 13);
            Raylib.DrawText(keyLabel, (int)keyBadge.X + (34 - kw) / 2, (int)keyBadge.Y + 5, 13, hovered ? new Color(10, 14, 20, 255) : Color.White);

            // Role tag right-aligned
            string roleTag = GetRoleTag(def.Type);
            int rtw = Raylib.MeasureText(roleTag, 11);
            Raylib.DrawText(roleTag, (int)(card.X + card.Width - 16f - rtw), (int)card.Y + 20, 11, def.Color);

            // Weapon Name
            DrawFittedText(def.Name, (int)card.X + 16, (int)card.Y + 48, 18, 13, 328, Color.White);

            // Stats line
            string statsLine = FormatStartStats(def);
            DrawFittedText(statsLine, (int)card.X + 16, (int)card.Y + 76, 12, 10, 328, new Color(145, 215, 225, 255));

            Raylib.DrawLine((int)card.X + 16, (int)card.Y + 98, (int)(card.X + card.Width - 16), (int)card.Y + 98, new Color(36, 48, 68, 210));

            // Wrapped description
            DrawWrappedText(def.Description, (int)card.X + 16, (int)card.Y + 110, 328, 13, 18, 4, new Color(190, 205, 225, 255));

            // Bottom CTA
            string cta = $"CLICK OR PRESS [{i + 1}]";
            int ctaW = Raylib.MeasureText(cta, 12);
            Raylib.DrawText(cta, (int)card.X + ((int)card.Width - ctaW) / 2, (int)card.Y + 198, 12, hovered ? def.Color : new Color(130, 160, 190, 230));

            if (selected < 0 && inputEnabled)
            {
                bool keyPressed = Raylib.IsKeyPressed(KeyboardKey.One + i) || Raylib.IsKeyPressed(KeyboardKey.Kp1 + i);
                if ((hovered && mouseClicked) || keyPressed)
                {
                    selected = i;
                }
            }
        }

        string footer = "ESC: Settings";
        int fw = Raylib.MeasureText(footer, 13);
        Raylib.DrawText(footer, (1280 - fw) / 2, 652, 13, new Color(125, 145, 175, 220));

        return selected;
    }

    public int DrawLevelUp(World world, Entity player, Vector2 virtualMouse, bool inputEnabled = true)
    {
        Raylib.DrawRectangle(0, 0, 1280, 720, new Color(6, 8, 14, 212));

        int level = 1;
        int pending = 1;
        if (world.IsAlive(player) && world.HasComponent<PlayerComponent>(player))
        {
            ref readonly PlayerComponent pc = ref world.GetComponent<PlayerComponent>(player);
            level = pc.Level;
            pending = Math.Max(1, pc.PendingLevelUps);
        }

        string title = "LEVEL UP!";
        int titleW = Raylib.MeasureText(title, 30);
        Raylib.DrawText(title, (1280 - titleW) / 2, 92, 30, new Color(255, 220, 95, 255));

        string subtitle = $"Reached Level {level}  -  Choose an upgrade ([1]-[3] or Click)";
        int subW = Raylib.MeasureText(subtitle, 15);
        Raylib.DrawText(subtitle, (1280 - subW) / 2, 130, 15, new Color(185, 205, 230, 255));

        bool mouseClicked = inputEnabled && Raylib.IsMouseButtonPressed(MouseButton.Left);
        int selected = -1;

        const float cardW = 340f;
        const float cardH = 380f;
        const float gapX = 30f;
        const float startX = 100f;
        const float startY = 172f;

        for (int i = 0; i < _cards.Count && i < 3; i++)
        {
            LevelUpCard c = _cards[i];
            Rectangle card = new(startX + i * (cardW + gapX), startY, cardW, cardH);
            bool hovered = inputEnabled && Raylib.CheckCollisionPointRec(virtualMouse, card);

            Color bg = hovered ? new Color(22, 29, 44, 252) : new Color(13, 17, 26, 246);
            Color border = hovered ? c.AccentColor : new Color(52, 68, 96, 230);

            Raylib.DrawRectangleRounded(card, 0.04f, 6, bg);
            Raylib.DrawRectangleRoundedLinesEx(card, 0.04f, 6, hovered ? 2.4f : 1.5f, border);
            Raylib.DrawRectangle((int)card.X + 2, (int)card.Y, (int)card.Width - 4, 5, c.AccentColor);

            // Key badge [1]-[3]
            Rectangle keyBadge = new(card.X + 18f, card.Y + 18f, 36f, 26f);
            Raylib.DrawRectangleRec(keyBadge, hovered ? c.AccentColor : new Color(26, 34, 48, 255));
            Raylib.DrawRectangleLinesEx(keyBadge, 1f, c.AccentColor);
            string keyLabel = $"[{i + 1}]";
            int kw = Raylib.MeasureText(keyLabel, 13);
            Raylib.DrawText(keyLabel, (int)keyBadge.X + (36 - kw) / 2, (int)keyBadge.Y + 6, 13, hovered ? new Color(10, 14, 20, 255) : Color.White);

            // Right pill badge
            int bw = Raylib.MeasureText(c.BadgeText, 11) + 18;
            Rectangle pill = new(card.X + card.Width - 18f - bw, card.Y + 18f, bw, 26f);
            Raylib.DrawRectangleRounded(pill, 0.25f, 4, new Color(c.AccentColor.R, c.AccentColor.G, c.AccentColor.B, (byte)38));
            Raylib.DrawRectangleRoundedLinesEx(pill, 0.25f, 4, 1f, c.AccentColor);
            Raylib.DrawText(c.BadgeText, (int)pill.X + 9, (int)pill.Y + 7, 11, c.AccentColor);

            // Title
            DrawFittedText(c.Title, (int)card.X + 18, (int)card.Y + 62, 19, 14, 304, Color.White);

            Raylib.DrawLine((int)card.X + 18, (int)card.Y + 98, (int)(card.X + card.Width - 18), (int)card.Y + 98, new Color(36, 48, 68, 220));

            // 3-line delta block
            if (!string.IsNullOrEmpty(c.DeltaLine1))
                DrawFittedText(c.DeltaLine1, (int)card.X + 18, (int)card.Y + 112, 14, 11, 304, new Color(165, 235, 220, 255));
            if (!string.IsNullOrEmpty(c.DeltaLine2))
                DrawFittedText(c.DeltaLine2, (int)card.X + 18, (int)card.Y + 135, 14, 11, 304, new Color(165, 220, 245, 255));
            if (!string.IsNullOrEmpty(c.DeltaLine3))
                DrawFittedText(c.DeltaLine3, (int)card.X + 18, (int)card.Y + 158, 14, 11, 304, new Color(255, 220, 120, 255));

            Raylib.DrawLine((int)card.X + 18, (int)card.Y + 190, (int)(card.X + card.Width - 18), (int)card.Y + 190, new Color(36, 48, 68, 220));

            // Wrapped description
            DrawWrappedText(c.Description, (int)card.X + 18, (int)card.Y + 204, 304, 13, 19, 5, new Color(195, 210, 230, 255));

            // Bottom button
            Rectangle selectBtn = new(card.X + 24f, card.Y + 324f, 292f, 38f);
            Color btnBg = hovered ? c.AccentColor : new Color(26, 36, 52, 245);
            Raylib.DrawRectangleRounded(selectBtn, 0.15f, 4, btnBg);
            Raylib.DrawRectangleRoundedLinesEx(selectBtn, 0.15f, 4, 1.4f, c.AccentColor);
            string btnText = $"SELECT  [{i + 1}]";
            int btw = Raylib.MeasureText(btnText, 14);
            Raylib.DrawText(btnText, (int)selectBtn.X + (292 - btw) / 2, (int)selectBtn.Y + 12, 14, hovered ? new Color(10, 14, 20, 255) : Color.White);

            if (selected < 0 && inputEnabled)
            {
                bool keyPressed = Raylib.IsKeyPressed(KeyboardKey.One + i) || Raylib.IsKeyPressed(KeyboardKey.Kp1 + i);
                if ((hovered && mouseClicked) || keyPressed)
                {
                    selected = i;
                }
            }
        }

        if (pending > 1)
        {
            string pendingText = $"Pending Level-Ups Remaining: {pending}";
            int pw = Raylib.MeasureText(pendingText, 14);
            Raylib.DrawText(pendingText, (1280 - pw) / 2, 568, 14, new Color(255, 215, 110, 245));
        }

        return selected;
    }

    private static LevelUpCard CreateNewWeaponCard(WeaponDefinition def, int openSlot)
    {
        int slotDisplay = openSlot >= 0 ? openSlot + 1 : 1;
        return new LevelUpCard
        {
            WeaponId = def.Id,
            Title = def.Name,
            Description = def.Description,
            IsNewWeapon = true,
            IsUpgrade = false,
            IsFallback = false,
            SlotIndex = openSlot,
            CurrentLevel = 0,
            NextLevel = 1,
            Definition = def,
            BadgeText = "NEW WEAPON",
            DeltaLine1 = $"Equip in Slot {slotDisplay} / {EquipmentComponent.MaxWeaponSlots}",
            DeltaLine2 = string.Create(CultureInfo.InvariantCulture, $"Base Damage: {def.BaseDamage:0.#}"),
            DeltaLine3 = string.Create(CultureInfo.InvariantCulture, $"Cooldown: {def.AttackInterval:0.00}s  ({GetRoleTag(def.Type)})"),
            AccentColor = new Color(65, 230, 195, 255)
        };
    }

    private static LevelUpCard CreateUpgradeCard(int slotIndex, WeaponItem weapon)
    {
        var def = weapon.Definition;
        int curLvl = weapon.Level;
        int nextLvl = curLvl + 1;

        var nextPreview = new WeaponItem(def, nextLvl);
        string bonusLine = def.Type switch
        {
            WeaponType.OuterRing => $"Orbs: {weapon.OrbitCount} -> {nextPreview.OrbitCount}  (+12% Speed)",
            WeaponType.MovementTrail => string.Create(CultureInfo.InvariantCulture, $"Pool Radius: {weapon.HazardRadius:0.#} -> {nextPreview.HazardRadius:0.#}px"),
            WeaponType.PiercingLine or WeaponType.ReturningBoomerang when nextPreview.BonusProjectiles > weapon.BonusProjectiles
                => $"Projectiles: {1 + weapon.BonusProjectiles} -> {1 + nextPreview.BonusProjectiles}  (+1 Shot!)",
            _ => string.Create(CultureInfo.InvariantCulture, $"Range: {weapon.Range:0.#} -> {nextPreview.Range:0.#}px (+8%)")
        };

        return new LevelUpCard
        {
            WeaponId = def.Id,
            Title = def.Name,
            Description = def.Description,
            IsNewWeapon = false,
            IsUpgrade = true,
            IsFallback = false,
            SlotIndex = slotIndex,
            CurrentLevel = curLvl,
            NextLevel = nextLvl,
            Definition = def,
            BadgeText = $"LV.{curLvl} -> LV.{nextLvl}",
            DeltaLine1 = string.Create(CultureInfo.InvariantCulture, $"Damage: {weapon.Damage:0.#} -> {nextPreview.Damage:0.#} (+25%)"),
            DeltaLine2 = string.Create(CultureInfo.InvariantCulture, $"Cooldown: {weapon.AttackInterval:0.00}s -> {nextPreview.AttackInterval:0.00}s (-15%)"),
            DeltaLine3 = bonusLine,
            AccentColor = new Color(255, 200, 75, 255)
        };
    }

    private static LevelUpCard CreateFallbackCard(int slotIndex, WeaponItem weapon)
    {
        var def = weapon.Definition;
        return new LevelUpCard
        {
            WeaponId = def.Id,
            Title = $"{def.Name} (Max)",
            Description = "Weapon is already at Max Level 5. Channel bio-surge energy to reinforce your chassis and heal.",
            IsNewWeapon = false,
            IsUpgrade = false,
            IsFallback = true,
            SlotIndex = slotIndex,
            CurrentLevel = weapon.Level,
            NextLevel = WeaponItem.MaxLevel,
            Definition = def,
            BadgeText = "MAX BONUS",
            DeltaLine1 = "Max HP: +20",
            DeltaLine2 = "Restore: +25 HP",
            DeltaLine3 = $"Slot {slotIndex + 1} Mastery Surge",
            AccentColor = new Color(90, 235, 130, 255)
        };
    }

    private static string GetRoleTag(WeaponType type) => type switch
    {
        WeaponType.ForwardCone => "MELEE CONE",
        WeaponType.PiercingLine => "PIERCING",
        WeaponType.OuterRing => "ORBITAL",
        WeaponType.MovementTrail => "TRAIL",
        WeaponType.ReturningBoomerang => "BOOMERANG",
        WeaponType.SidewaysBroadside => "BROADSIDE",
        _ => "WEAPON"
    };

    private static string FormatStartStats(WeaponDefinition def) => def.Type switch
    {
        WeaponType.OuterRing => string.Create(CultureInfo.InvariantCulture, $"DMG {def.BaseDamage:0}   |   {def.OrbitCount} ORBS   |   RING {def.OrbitRadius:0}px"),
        WeaponType.MovementTrail => string.Create(CultureInfo.InvariantCulture, $"DMG {def.BaseDamage:0}/tick   |   INT {def.AttackInterval:0.00}s   |   DUR {def.HazardDuration:0.0}s"),
        _ => string.Create(CultureInfo.InvariantCulture, $"DMG {def.BaseDamage:0}   |   CD {def.AttackInterval:0.00}s   |   RNG {def.Range:0}px")
    };

    private static void DrawFittedText(string text, int x, int y, int preferredSize, int minSize, int maxWidth, Color color)
    {
        int size = preferredSize;
        while (size > minSize && Raylib.MeasureText(text, size) > maxWidth)
        {
            size--;
        }

        string drawText = text;
        if (Raylib.MeasureText(drawText, size) > maxWidth && drawText.Length > 3)
        {
            while (drawText.Length > 3 && Raylib.MeasureText(drawText + "..", size) > maxWidth)
            {
                drawText = drawText[..^1];
            }
            drawText += "..";
        }

        Raylib.DrawText(drawText, x, y, size, color);
    }

    private static void DrawWrappedText(string text, int x, int y, int maxWidth, int fontSize, int lineHeight, int maxLines, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string currentLine = "";
        int lineIdx = 0;

        for (int i = 0; i < words.Length; i++)
        {
            string candidate = currentLine.Length == 0 ? words[i] : $"{currentLine} {words[i]}";
            if (Raylib.MeasureText(candidate, fontSize) <= maxWidth)
            {
                currentLine = candidate;
            }
            else
            {
                if (lineIdx >= maxLines - 1)
                {
                    DrawFittedText($"{currentLine}..", x, y + lineIdx * lineHeight, fontSize, fontSize, maxWidth, color);
                    return;
                }

                Raylib.DrawText(currentLine, x, y + lineIdx * lineHeight, fontSize, color);
                lineIdx++;
                currentLine = words[i];
            }
        }

        if (currentLine.Length > 0 && lineIdx < maxLines)
        {
            Raylib.DrawText(currentLine, x, y + lineIdx * lineHeight, fontSize, color);
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
