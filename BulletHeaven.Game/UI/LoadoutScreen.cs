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
    private bool _isInspectedEquipped;

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

        string title = "MUTAGEN LOADOUT & ANATOMY";
        int titleW = Raylib.MeasureText(title, 24);
        Raylib.DrawText(title, (1280 - titleW) / 2, 22, 24, new Color(80, 235, 215, 255));

        string subtitle = waveSystem.WaveCompleted
            ? $"WAVE {waveSystem.CurrentWaveNumber} CLEARED — ENHANCE YOUR BIOLOGICAL ARSENAL"
            : $"WAVE {waveSystem.CurrentWaveNumber} PREPARATION — CONFIGURE ANATOMICAL SLOTS";
        int subW = Raylib.MeasureText(subtitle, 13);
        Raylib.DrawText(subtitle, (1280 - subW) / 2, 52, 13, new Color(135, 160, 195, 255));

        Raylib.DrawText("INVENTORY (6)", 50, 92, 13, new Color(175, 195, 220, 255));

        float leftStartX = 50f;
        float leftStartY = 116f;
        float invSize = 100f;
        float invGap = 14f;

        WeaponItem? hoveredInventoryItem = null;

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
                    _isInspectedEquipped = false;
                }

                Raylib.DrawRectangleLinesEx(rect, isHovered ? 2.5f : 1.5f, item.RarityColor);
                DrawCenteredText(item.Definition.Name, rect, 12, Color.White);

                if (isHovered && mouseClicked)
                {
                    int matchingSlot = equip.FindFirstMatchingEmptySlot(item);
                    if (matchingSlot != -1)
                    {
                        equip.Equip(matchingSlot, item);
                        stash.Remove(item);
                        hoveredInventoryItem = null;
                        _isInspectedEquipped = true;
                    }
                }
            }
            else
            {
                Raylib.DrawRectangleLinesEx(rect, 1f, new Color(36, 44, 60, 180));
            }
        }

        Rectangle vitalsRect = new(50f, 470f, 214f, 195f);
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
        if (statsStore != null && statsStore.Has(playerEntity))
        {
            var s = statsStore.Get(playerEntity);
            critVal = s.CritChance * 100f;
            rangeMod = (s.RangeMultiplier - 1f) * 100f;
        }

        Raylib.DrawText("HEALTH:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 38, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText(hpText, (int)vitalsRect.X + 120, (int)vitalsRect.Y + 38, 11, Color.White);

        Raylib.DrawText("MOVE SPEED:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 62, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"{player.Speed:0} px/s", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 62, 11, Color.White);

        Raylib.DrawText("CRIT CHANCE:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 86, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"{critVal:0}%", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 86, 11, new Color(255, 205, 80, 255));

        Raylib.DrawText("RANGE MOD:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 110, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"+{rangeMod:0}%", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 110, 11, new Color(120, 210, 255, 255));

        Raylib.DrawText("EQUIPPED:", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 134, 11, new Color(150, 165, 185, 255));
        Raylib.DrawText($"{equippedCount} / 5", (int)vitalsRect.X + 120, (int)vitalsRect.Y + 134, 11, new Color(80, 240, 180, 255));

        Raylib.DrawLine((int)vitalsRect.X + 12, (int)vitalsRect.Y + 158, (int)vitalsRect.X + (int)vitalsRect.Width - 12, (int)vitalsRect.Y + 158, new Color(36, 48, 68, 180));
        Raylib.DrawText("CLICK ITEM TO EQUIP / UNEQUIP", (int)vitalsRect.X + 14, (int)vitalsRect.Y + 172, 10, new Color(110, 130, 160, 255));

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
                    _isInspectedEquipped = true;
                }

                Raylib.DrawRectangleLinesEx(rect, isHovered ? 2.5f : 1.5f, slot.Weapon.RarityColor);
                DrawCenteredText(slot.Weapon.Definition.Name, rect, 12, Color.White);

                if (isHovered && mouseClicked)
                {
                    if (stash.Items.Count < 6)
                    {
                        var unequipped = equip.Unequip(i);
                        if (unequipped != null)
                        {
                            stash.Add(unequipped);
                            _isInspectedEquipped = false;
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

        Rectangle inspectorRect = new(710f, 92f, 520f, 480f);
        Raylib.DrawRectangleRec(inspectorRect, new Color(13, 16, 24, 255));

        Color inspectorBorder = _inspectedItem != null ? _inspectedItem.RarityColor : new Color(36, 48, 68, 200);
        Raylib.DrawRectangleLinesEx(inspectorRect, 1.5f, inspectorBorder);

        if (_inspectedItem != null)
        {
            var item = _inspectedItem;

            Raylib.DrawText(item.Definition.Name, (int)inspectorRect.X + 22, (int)inspectorRect.Y + 20, 20, Color.White);

            string tierText = $"TIER {item.Tier} ({item.Rarity.ToString().ToUpper()})";
            Raylib.DrawText(tierText, (int)inspectorRect.X + 22, (int)inspectorRect.Y + 48, 12, item.RarityColor);

            string slotBadge = $"SLOT: [{item.Definition.Slot.ToString().ToUpper()}]";
            Raylib.DrawText(slotBadge, (int)inspectorRect.X + 220, (int)inspectorRect.Y + 48, 12, new Color(80, 230, 210, 255));

            int tagX = (int)inspectorRect.X + 22;
            int tagY = (int)inspectorRect.Y + 74;

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

            Raylib.DrawLine((int)inspectorRect.X + 22, (int)inspectorRect.Y + 106, (int)inspectorRect.X + (int)inspectorRect.Width - 22, (int)inspectorRect.Y + 106, new Color(36, 48, 68, 180));

            Raylib.DrawText("MUTATION SPECIFICATIONS", (int)inspectorRect.X + 22, (int)inspectorRect.Y + 120, 11, new Color(125, 150, 180, 255));

            int statY = (int)inspectorRect.Y + 144;
            int statCol1 = (int)inspectorRect.X + 22;
            int statCol2 = (int)inspectorRect.X + 270;

            if (item.Definition.Type == WeaponType.Passive)
            {
                Raylib.DrawText("ORGAN TYPE:", statCol1, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText("PASSIVE MUTATION", statCol1 + 130, statY, 12, new Color(80, 240, 180, 255));

                Raylib.DrawText("CATEGORY:", statCol2, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText("BIO-AUGMENT", statCol2 + 120, statY, 12, Color.White);

                statY += 26;
                Raylib.DrawText("TRIGGER:", statCol1, statY, 12, new Color(150, 165, 185, 255));
                string trigger = (item.Definition.Keywords & WeaponKeywords.Stationary) != 0 ? "STAND STILL (>0.5s)" :
                                 (item.Definition.Keywords & WeaponKeywords.Burst) != 0 ? "EVERY 4s (DASH)" :
                                 (item.Definition.Keywords & WeaponKeywords.Berserk) != 0 ? "LOW HP SCALING" : "PERMANENT AURA";
                Raylib.DrawText(trigger, statCol1 + 130, statY, 12, Color.White);

                Raylib.DrawText("POTENCY:", statCol2, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText($"TIER {item.Tier} ENHANCED", statCol2 + 120, statY, 12, item.RarityColor);

                statY += 26;
                Raylib.DrawText("SLOT TARGET:", statCol1, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText(item.Definition.Slot.ToString().ToUpper(), statCol1 + 130, statY, 12, Color.White);

                Raylib.DrawText("ATTACK:", statCol2, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText("NONE (PASSIVE)", statCol2 + 120, statY, 12, new Color(140, 160, 185, 255));
            }
            else
            {
                Raylib.DrawText("ATTACK TYPE:", statCol1, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText(item.Definition.Type.ToString().ToUpper(), statCol1 + 130, statY, 12, Color.White);

                Raylib.DrawText("DAMAGE:", statCol2, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.Damage:0.0}", statCol2 + 120, statY, 12, Color.White);

                statY += 26;
                Raylib.DrawText("FIRE RATE:", statCol1, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{(1f / MathF.Max(0.01f, item.AttackInterval)):0.00} /s", statCol1 + 130, statY, 12, Color.White);

                Raylib.DrawText("COOLDOWN:", statCol2, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.AttackInterval:0.00}s", statCol2 + 120, statY, 12, Color.White);

                statY += 26;
                Raylib.DrawText("RANGE:", statCol1, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.Range:0} px", statCol1 + 130, statY, 12, Color.White);

                Raylib.DrawText("KNOCKBACK:", statCol2, statY, 12, new Color(150, 165, 185, 255));
                Raylib.DrawText($"{item.Definition.Knockback:0}", statCol2 + 120, statY, 12, Color.White);
            }

            Raylib.DrawLine((int)inspectorRect.X + 22, (int)inspectorRect.Y + 236, (int)inspectorRect.X + (int)inspectorRect.Width - 22, (int)inspectorRect.Y + 236, new Color(36, 48, 68, 180));

            Raylib.DrawText("BIOLOGICAL MECHANISM", (int)inspectorRect.X + 22, (int)inspectorRect.Y + 250, 11, new Color(125, 150, 180, 255));

            string desc = string.IsNullOrEmpty(item.Definition.Description)
                ? "No biological description available."
                : item.Definition.Description;

            DrawWrappedText(desc, (int)inspectorRect.X + 22, (int)inspectorRect.Y + 276, 476, 13, 22, new Color(210, 225, 240, 255));

            Raylib.DrawLine((int)inspectorRect.X + 22, (int)inspectorRect.Y + 418, (int)inspectorRect.X + (int)inspectorRect.Width - 22, (int)inspectorRect.Y + 418, new Color(36, 48, 68, 180));

            if (_isInspectedEquipped)
            {
                Raylib.DrawText("[CLICK ITEM TO UNEQUIP TO INVENTORY]", (int)inspectorRect.X + 22, (int)inspectorRect.Y + 440, 12, new Color(255, 130, 110, 255));
            }
            else
            {
                string equipPrompt = $"[CLICK ITEM TO EQUIP TO {item.Definition.Slot.ToString().ToUpper()}]";
                Raylib.DrawText(equipPrompt, (int)inspectorRect.X + 22, (int)inspectorRect.Y + 440, 12, new Color(75, 235, 160, 255));
            }
        }
        else
        {
            string emptyPrompt = "HOVER OVER AN ITEM OR BODY SLOT TO INSPECT MUTAGEN DATA";
            int epw = Raylib.MeasureText(emptyPrompt, 12);
            Raylib.DrawText(emptyPrompt, (int)inspectorRect.X + ((int)inspectorRect.Width - epw) / 2, (int)inspectorRect.Y + 225, 12, new Color(100, 120, 150, 255));
        }

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
