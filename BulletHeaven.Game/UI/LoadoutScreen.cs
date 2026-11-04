using System;
using System.Linq;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game.UI;

public sealed class LoadoutScreen
{
    private static readonly string[] SlotLabels =
    {
        "HEAD",
        "LEFT HAND",
        "CHEST",
        "RIGHT HAND",
        "LEGS"
    };

    private WeaponItem? _inspectedItem;

    public void Draw(World world, WaveSystem waveSystem, Vector2 mousePos, Action onStartWave, Action onOpenSettings)
    {
        var equipments = world.GetStore<EquipmentComponent>();
        var stashes = world.GetStore<BioStashComponent>();
        var players = world.GetStore<PlayerComponent>();
        var statsStore = world.GetStore<PlayerStatsComponent>();
        var healthStore = world.GetStore<HealthComponent>();

        if (equipments == null || stashes == null || players == null) return;

        Entity playerEntity = default;
        bool hasPlayer = false;

        foreach (Entity p in world.Query().With<PlayerComponent>().With<EquipmentComponent>().With<BioStashComponent>())
        {
            playerEntity = p;
            hasPlayer = true;
            break;
        }

        if (!hasPlayer) return;

        ref EquipmentComponent equip = ref equipments.Get(playerEntity);
        ref BioStashComponent stash = ref stashes.Get(playerEntity);
        ref PlayerComponent player = ref players.Get(playerEntity);

        bool mouseClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

        Raylib.DrawRectangle(0, 0, 1280, 720, new Color(8, 11, 17, 245));

        DrawHeaderAndSettings(mousePos, mouseClicked, onOpenSettings);

        WeaponItem? hoveredInventoryItem = null;
        DrawInventorySection(ref stash, ref equip, mousePos, mouseClicked, ref hoveredInventoryItem);
        DrawOperatorVitals(ref player, playerEntity, ref equip, healthStore, statsStore);
        DrawPaperdollSection(ref equip, ref stash, mousePos, mouseClicked, hoveredInventoryItem);
        DrawInspectorSection();
        DrawWaveLaunchSection(waveSystem, mousePos, mouseClicked, onStartWave);
    }

    private static void DrawHeaderAndSettings(Vector2 mousePos, bool mouseClicked, Action onOpenSettings)
    {
        Rectangle settingsBtn = new(1130f, 20f, 100f, 30f);
        bool setHover = Raylib.CheckCollisionPointRec(mousePos, settingsBtn);
        Raylib.DrawRectangleRec(settingsBtn, setHover ? new Color(28, 42, 60, 255) : new Color(18, 26, 38, 255));
        Raylib.DrawRectangleLinesEx(settingsBtn, 1f, setHover ? new Color(80, 230, 210, 255) : new Color(50, 70, 95, 200));

        string setText = "SETTINGS";
        int stw = Raylib.MeasureText(setText, 11);
        Raylib.DrawText(setText, (int)settingsBtn.X + ((int)settingsBtn.Width - stw) / 2, (int)settingsBtn.Y + 9, 11, setHover ? Color.White : new Color(160, 185, 210, 255));

        if (setHover && mouseClicked)
        {
            onOpenSettings();
        }
    }

    private void DrawInventorySection(ref BioStashComponent stash, ref EquipmentComponent equip, Vector2 mousePos, bool mouseClicked, ref WeaponItem? hoveredInventoryItem)
    {
        Raylib.DrawText("INVENTORY", 50, 92, 13, new Color(175, 195, 220, 255));

        float leftStartX = 50f;
        float leftStartY = 116f;
        float invSize = 100f;
        float invGap = 14f;

        for (int i = 0; i < 6; i++)
        {
            int col = i % 2;
            int row = i / 2;
            Rectangle rect = new(leftStartX + col * (invSize + invGap), leftStartY + row * (invSize + invGap), invSize, invSize);
            bool isHovered = Raylib.CheckCollisionPointRec(mousePos, rect);

            Raylib.DrawRectangleRec(rect, isHovered ? new Color(24, 32, 46, 255) : new Color(14, 18, 26, 255));

            if (i < stash.Items.Count)
            {
                var item = stash.Items[i];
                if (isHovered)
                {
                    hoveredInventoryItem = item;
                    _inspectedItem = item;
                }

                Raylib.DrawRectangleLinesEx(rect, isHovered ? 2.5f : 1.5f, item.RarityColor);
                DrawCenteredText($"{item.Definition.Name} {ToRoman(item.Tier)}", rect, 11, Color.White);

                if (isHovered && mouseClicked)
                {
                    int matchingSlot = equip.FindFirstMatchingEmptySlot(item);
                    if (matchingSlot != -1)
                    {
                        equip.Equip(matchingSlot, item);
                        stash.Remove(item);
                        hoveredInventoryItem = null;
                    }
                }
            }
            else
            {
                Raylib.DrawRectangleLinesEx(rect, 1f, new Color(36, 44, 60, 180));
            }
        }
    }

    private static void DrawOperatorVitals(ref PlayerComponent player, Entity playerEntity, ref EquipmentComponent equip, ComponentStore<HealthComponent>? healthStore, ComponentStore<PlayerStatsComponent>? statsStore)
    {
        Rectangle vitalsRect = new(50f, 470f, 214f, 175f);
        Raylib.DrawRectangleRec(vitalsRect, new Color(13, 16, 24, 255));
        Raylib.DrawRectangleLinesEx(vitalsRect, 1.5f, new Color(36, 48, 68, 200));

        Raylib.DrawText("OPERATOR VITALS", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 14, 12, new Color(80, 230, 210, 255));

        int equippedCount = 0;
        for (int i = 0; i < equip.Slots.Length; i++)
        {
            if (equip.Slots[i].Weapon != null) equippedCount++;
        }

        string hpText = "100 / 100";
        if (healthStore != null && healthStore.Has(playerEntity))
        {
            var hp = healthStore.Get(playerEntity);
            hpText = $"{MathF.Round(hp.Current)} / {MathF.Round(hp.Max)}";
        }

        float critVal = 5f;
        float rangeMod = 0f;
        float dodgeVal = 0f;
        if (statsStore != null && statsStore.Has(playerEntity))
        {
            var s = statsStore.Get(playerEntity);
            critVal = s.CritChance * 100f;
            rangeMod = (s.RangeMultiplier - 1f) * 100f;
            dodgeVal = s.DodgeChance * 100f;
        }

        Raylib.DrawText("HEALTH:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 36, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText(hpText, (int)vitalsRect.X + 120, (int)vitalsRect.Y + 36, 11, Color.White);

        Raylib.DrawText("MOVE SPEED:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 58, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"{player.Speed:0} px/s", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 58, 11, Color.White);

        Raylib.DrawText("CRIT CHANCE:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 80, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"{critVal:0}%", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 80, 11, new Color(255, 205, 80, 255));

        Raylib.DrawText("DODGE CHANCE:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 102, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"{dodgeVal:0}%", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 102, 11, new Color(90, 225, 235, 255));

        Raylib.DrawText("RANGE MOD:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 124, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"+{rangeMod:0}%", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 124, 11, new Color(120, 210, 255, 255));

        Raylib.DrawText("EQUIPPED:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 146, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"{equippedCount} / 5", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 146, 11, new Color(80, 240, 180, 255));
    }

    private void DrawPaperdollSection(ref EquipmentComponent equip, ref BioStashComponent stash, Vector2 mousePos, bool mouseClicked, WeaponItem? hoveredInventoryItem)
    {
        float bodyCenter = 490f;
        float slotSize = 100f;
        float bodyCenterLeft = bodyCenter - slotSize * 0.5f;

        Raylib.DrawText("ANATOMICAL SLOTS", (int)bodyCenterLeft - 10, 92, 13, new Color(175, 195, 220, 255));

        Color boneLineColor = new(40, 60, 85, 220);
        Raylib.DrawLineEx(new Vector2(bodyCenter, 216f), new Vector2(bodyCenter, 230f), 2f, boneLineColor);
        Raylib.DrawLineEx(new Vector2(bodyCenterLeft, 280f), new Vector2(bodyCenterLeft - 20f, 280f), 2f, boneLineColor);
        Raylib.DrawLineEx(new Vector2(bodyCenterLeft + slotSize, 280f), new Vector2(bodyCenterLeft + slotSize + 20f, 280f), 2f, boneLineColor);
        Raylib.DrawLineEx(new Vector2(bodyCenter, 330f), new Vector2(bodyCenter, 344f), 2f, boneLineColor);

        Rectangle[] bodyRects =
        {
            new(bodyCenterLeft, 116f, slotSize, slotSize),
            new(bodyCenterLeft - (slotSize + 20f), 230f, slotSize, slotSize),
            new(bodyCenterLeft, 230f, slotSize, slotSize),
            new(bodyCenterLeft + (slotSize + 20f), 230f, slotSize, slotSize),
            new(bodyCenterLeft, 344f, slotSize, slotSize)
        };

        for (int i = 0; i < 5; i++)
        {
            Rectangle rect = bodyRects[i];
            bool isHovered = Raylib.CheckCollisionPointRec(mousePos, rect);
            var slot = equip.Slots[i];

            bool isCompatible = hoveredInventoryItem != null &&
                                EquipmentComponent.SlotTypes[i] == hoveredInventoryItem.Definition.Slot &&
                                slot.Weapon == null;

            Color bgColor = isHovered ? new Color(24, 32, 46, 255) : (isCompatible ? new Color(16, 32, 34, 255) : new Color(14, 18, 26, 255));
            Raylib.DrawRectangleRec(rect, bgColor);

            if (slot.Weapon != null)
            {
                if (isHovered)
                {
                    _inspectedItem = slot.Weapon;
                }

                Raylib.DrawRectangleLinesEx(rect, isHovered ? 2.5f : 1.5f, slot.Weapon.RarityColor);
                DrawCenteredText($"{slot.Weapon.Definition.Name} {ToRoman(slot.Weapon.Tier)}", rect, 11, Color.White);

                if (isHovered && mouseClicked)
                {
                    if (stash.Items.Count < 6)
                    {
                        var unequipped = equip.Unequip(i);
                        if (unequipped != null)
                        {
                            stash.Add(unequipped);
                        }
                    }
                }
            }
            else
            {
                Color borderColor = isCompatible ? new Color(75, 235, 160, 255) : new Color(42, 50, 68, 180);
                float thickness = isCompatible ? 2f : 1f;
                Raylib.DrawRectangleLinesEx(rect, thickness, borderColor);

                Color textColor = isCompatible ? new Color(140, 245, 200, 255) : new Color(85, 100, 125, 200);
                DrawCenteredText(SlotLabels[i], rect, 11, textColor);
            }
        }
    }

    private void DrawInspectorSection()
    {
        Rectangle inspectorRect = new(710f, 92f, 520f, 480f);
        Raylib.DrawRectangleRec(inspectorRect, new Color(13, 16, 24, 255));

        Color inspectorBorder = _inspectedItem != null ? _inspectedItem.RarityColor : new Color(36, 48, 68, 200);
        Raylib.DrawRectangleLinesEx(inspectorRect, 1.5f, inspectorBorder);

        if (_inspectedItem != null)
        {
            var item = _inspectedItem;

            Raylib.DrawText(item.Definition.Name, (int)inspectorRect.X + 22, (int)inspectorRect.Y + 20, 20, Color.White);
            int nameW = Raylib.MeasureText(item.Definition.Name, 20);
            Raylib.DrawText(ToRoman(item.Tier), (int)inspectorRect.X + 22 + nameW + 10, (int)inspectorRect.Y + 20, 20, item.RarityColor);

            int tagX = (int)inspectorRect.X + 22;
            int tagY = (int)inspectorRect.Y + 50;

            string slotBadge = $"[{item.Definition.Slot.ToString().ToUpper()}]";
            int sbw = Raylib.MeasureText(slotBadge, 11);
            Rectangle slotPill = new(tagX, tagY, sbw + 10, 20);
            Raylib.DrawRectangleRec(slotPill, new Color(18, 38, 38, 255));
            Raylib.DrawRectangleLinesEx(slotPill, 1f, new Color(60, 200, 175, 255));
            Raylib.DrawText(slotBadge, tagX + 5, tagY + 4, 11, new Color(100, 240, 215, 255));
            tagX += sbw + 16;

            foreach (WeaponKeywords kw in Enum.GetValues<WeaponKeywords>())
            {
                if (kw == WeaponKeywords.None) continue;
                if (item.Definition.Keywords.HasFlag(kw))
                {
                    string tagStr = $"[{kw.ToString().ToUpper()}]";
                    int tw = Raylib.MeasureText(tagStr, 11);
                    Rectangle tagPill = new(tagX, tagY, tw + 10, 20);
                    Raylib.DrawRectangleRec(tagPill, new Color(20, 32, 48, 255));
                    Raylib.DrawRectangleLinesEx(tagPill, 1f, new Color(50, 110, 180, 255));
                    Raylib.DrawText(tagStr, tagX + 5, tagY + 4, 11, new Color(150, 210, 255, 255));
                    tagX += tw + 16;
                }
            }

            Raylib.DrawLine((int)inspectorRect.X + 22, (int)inspectorRect.Y + 80, (int)inspectorRect.X + (int)inspectorRect.Width - 22, (int)inspectorRect.Y + 80, new Color(36, 48, 68, 180));

            int statY = (int)inspectorRect.Y + 98;
            int statCol1 = (int)inspectorRect.X + 22;
            int statCol2 = (int)inspectorRect.X + 265;

            if (item.Definition.Type == WeaponType.Passive)
            {
                if (item.Definition.CritChanceBonus > 0f)
                {
                    Raylib.DrawText($"+{(item.Definition.CritChanceBonus * 100f):0}% Critical Strike Chance", statCol1, statY, 13, new Color(80, 240, 140, 255));
                    statY += 26;
                }

                if (item.Definition.RangeMultiplierBonus > 0f)
                {
                    Raylib.DrawText($"+{(item.Definition.RangeMultiplierBonus * 100f):0}% Attack Range across all mutations", statCol1, statY, 13, new Color(80, 240, 140, 255));
                    statY += 26;
                }

                if (item.Definition.LongRangeThreshold > 0f)
                {
                    Raylib.DrawText($"+{((item.Definition.LongRangeDamageMultiplier - 1f) * 100f):0}% Damage to targets >{item.Definition.LongRangeThreshold:0}px away", statCol1, statY, 13, new Color(80, 240, 140, 255));
                    statY += 26;
                }

                if ((item.Definition.Keywords & WeaponKeywords.Berserk) != 0)
                {
                    Raylib.DrawText("[Berserk]: Attack Speed scales up to +85% as HP drops", statCol1, statY, 13, new Color(255, 180, 80, 255));
                    statY += 26;
                }

                if ((item.Definition.Keywords & WeaponKeywords.Stationary) != 0)
                {
                    Raylib.DrawText("[Stationary]: +40% Damage and +4 HP/s while standing still", statCol1, statY, 13, new Color(80, 240, 140, 255));
                    statY += 26;
                }

                if ((item.Definition.Keywords & WeaponKeywords.Burst) != 0)
                {
                    Raylib.DrawText("[Burst]: +100% Movement Speed dash every 4 seconds", statCol1, statY, 13, new Color(100, 220, 255, 255));
                    statY += 26;
                }
            }
            else
            {
                Raylib.DrawText("Damage:", statCol1, statY, 13, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.Damage:0.0}", statCol1 + 90, statY, 13, Color.White);

                Raylib.DrawText("Cooldown:", statCol2, statY, 13, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.AttackInterval:0.00}s ({(1f / MathF.Max(0.01f, item.AttackInterval)):0.00}/s)", statCol2 + 90, statY, 13, Color.White);

                statY += 26;
                Raylib.DrawText("Range:", statCol1, statY, 13, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.Range:0} px", statCol1 + 90, statY, 13, Color.White);

                Raylib.DrawText("Knockback:", statCol2, statY, 13, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.Definition.Knockback:0}", statCol2 + 90, statY, 13, Color.White);

                if (item.Definition.PiercingHits > 1)
                {
                    statY += 26;
                    Raylib.DrawText($"Pierces up to {item.Definition.PiercingHits} enemies", statCol1, statY, 13, new Color(120, 220, 255, 255));
                }

                if (item.Definition.LifestealOnCrit > 0f)
                {
                    statY += 26;
                    Raylib.DrawText($"+{item.Definition.LifestealOnCrit:0} HP Lifesteal on Critical Strike", statCol1, statY, 13, new Color(255, 120, 140, 255));
                }

                if (item.Definition.ChainTargets > 0)
                {
                    statY += 26;
                    Raylib.DrawText($"Lightning chains to {item.Definition.ChainTargets} targets ({(item.Definition.ChainDamageRatio * 100f):0}% damage)", statCol1, statY, 13, new Color(100, 220, 255, 255));
                }
            }

            int descDividerY = Math.Max(statY + 14, (int)inspectorRect.Y + 205);
            Raylib.DrawLine((int)inspectorRect.X + 22, descDividerY, (int)inspectorRect.X + (int)inspectorRect.Width - 22, descDividerY, new Color(36, 48, 68, 180));

            string desc = string.IsNullOrEmpty(item.Definition.Description)
                ? "No biological description available."
                : item.Definition.Description;

            DrawWrappedText(desc, (int)inspectorRect.X + 22, descDividerY + 16, 476, 13, 22, new Color(210, 225, 240, 255));
        }
        else
        {
            string emptyPrompt = "HOVER OVER AN ITEM OR BODY SLOT TO INSPECT MUTAGEN DATA";
            int epw = Raylib.MeasureText(emptyPrompt, 12);
            Raylib.DrawText(emptyPrompt, (int)inspectorRect.X + ((int)inspectorRect.Width - epw) / 2, (int)inspectorRect.Y + 225, 12, new Color(100, 120, 150, 255));
        }
    }

    private static void DrawWaveLaunchSection(WaveSystem waveSystem, Vector2 mousePos, bool mouseClicked, Action onStartWave)
    {
        int targetWave = waveSystem.WaveCompleted ? waveSystem.CurrentWaveNumber + 1 : waveSystem.CurrentWaveNumber;
        Rectangle nextWaveBtn = new(710f, 595f, 520f, 70f);
        bool btnHover = Raylib.CheckCollisionPointRec(mousePos, nextWaveBtn);

        Color btnBg = btnHover ? new Color(26, 175, 140, 255) : new Color(16, 135, 108, 255);
        Raylib.DrawRectangleRec(nextWaveBtn, btnBg);
        Raylib.DrawRectangleLinesEx(nextWaveBtn, 2f, new Color(80, 250, 205, 255));

        string btnText = $"START WAVE {targetWave}";
        int bw = Raylib.MeasureText(btnText, 18);
        Raylib.DrawText(btnText, (int)nextWaveBtn.X + ((int)nextWaveBtn.Width - bw) / 2, (int)nextWaveBtn.Y + 16, 18, Color.White);

        string spaceText = "(OR PRESS SPACEBAR)";
        int sw = Raylib.MeasureText(spaceText, 11);
        Raylib.DrawText(spaceText, (int)nextWaveBtn.X + ((int)nextWaveBtn.Width - sw) / 2, (int)nextWaveBtn.Y + 42, 11, new Color(190, 255, 235, 255));

        if ((btnHover && mouseClicked) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            onStartWave();
        }
    }

    private static string ToRoman(int tier) => tier switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        _ => "I"
    };

    private static void DrawCenteredText(string text, Rectangle rect, int fontSize, Color color)
    {
        int textW = Raylib.MeasureText(text, fontSize);
        if (textW > rect.Width - 12)
        {
            string[] words = text.Split(' ');
            if (words.Length > 1)
            {
                string line1 = words[0];
                string line2 = string.Join(" ", words.Skip(1));
                int w1 = Raylib.MeasureText(line1, fontSize);
                int w2 = Raylib.MeasureText(line2, fontSize);
                int y1 = (int)(rect.Y + (rect.Height - fontSize * 2 - 4) * 0.5f);
                int y2 = y1 + fontSize + 4;
                Raylib.DrawText(line1, (int)(rect.X + (rect.Width - w1) * 0.5f), y1, fontSize, color);
                Raylib.DrawText(line2, (int)(rect.X + (rect.Width - w2) * 0.5f), y2, fontSize, color);
                return;
            }
        }

        int x = (int)(rect.X + (rect.Width - textW) * 0.5f);
        int y = (int)(rect.Y + (rect.Height - fontSize) * 0.5f);
        Raylib.DrawText(text, x, y, fontSize, color);
    }

    private static void DrawWrappedText(string text, int x, int y, int maxWidth, int fontSize, int lineHeight, Color color)
    {
        string[] words = text.Split(' ');
        string currentLine = "";
        int currentY = y;

        for (int i = 0; i < words.Length; i++)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? words[i] : currentLine + " " + words[i];
            int testWidth = Raylib.MeasureText(testLine, fontSize);

            if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                Raylib.DrawText(currentLine, x, currentY, fontSize, color);
                currentLine = words[i];
                currentY += lineHeight;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            Raylib.DrawText(currentLine, x, currentY, fontSize, color);
        }
    }
}
