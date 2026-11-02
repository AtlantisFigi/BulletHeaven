using System;
using System.Linq;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Systems;
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
        "LEGS",
        "FEET"
    };

    public void Draw(World world, WaveSystem waveSystem, Action onStartWave)
    {
        var equipments = world.GetStore<EquipmentComponent>();
        var stashes = world.GetStore<BioStashComponent>();
        var players = world.GetStore<PlayerComponent>();

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

        Vector2 mousePos = Raylib.GetMousePosition();
        bool mouseClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

        Raylib.DrawRectangle(0, 0, 1280, 720, new Color(8, 10, 16, 235));

        float leftStartX = 140f;
        float leftStartY = 170f;
        float sqSize = 110f;
        float leftGap = 20f;

        for (int i = 0; i < 6; i++)
        {
            int col = i % 2;
            int row = i / 2;
            Rectangle rect = new(leftStartX + col * (sqSize + leftGap), leftStartY + row * (sqSize + leftGap), sqSize, sqSize);
            bool isHovered = Raylib.CheckCollisionPointRec(mousePos, rect);

            Raylib.DrawRectangleRec(rect, isHovered ? new Color(24, 32, 46, 255) : new Color(14, 18, 26, 255));

            if (i < stash.Items.Count)
            {
                var item = stash.Items[i];
                Raylib.DrawRectangleLinesEx(rect, isHovered ? 2.5f : 1.5f, item.RarityColor);
                DrawCenteredText(item.Definition.Name, rect, 13, Color.White);

                if (isHovered && mouseClicked)
                {
                    int emptySlot = equip.FirstEmptySlotIndex();
                    if (emptySlot != -1)
                    {
                        equip.Equip(emptySlot, item);
                        stash.Remove(item);
                    }
                }
            }
            else
            {
                Raylib.DrawRectangleLinesEx(rect, 1f, new Color(40, 48, 65, 180));
            }
        }

        float centerX = 800f;
        float bodyCenterLeft = centerX - sqSize * 0.5f;

        Rectangle[] bodyRects =
        {
            new(bodyCenterLeft, 90f, sqSize, sqSize),
            new(bodyCenterLeft - (sqSize + 24f), 220f, sqSize, sqSize),
            new(bodyCenterLeft, 220f, sqSize, sqSize),
            new(bodyCenterLeft + (sqSize + 24f), 220f, sqSize, sqSize),
            new(bodyCenterLeft, 350f, sqSize, sqSize),
            new(bodyCenterLeft, 480f, sqSize, sqSize)
        };

        for (int i = 0; i < 6; i++)
        {
            Rectangle rect = bodyRects[i];
            bool isHovered = Raylib.CheckCollisionPointRec(mousePos, rect);
            var slot = equip.Slots[i];

            Raylib.DrawRectangleRec(rect, isHovered ? new Color(24, 32, 46, 255) : new Color(14, 18, 26, 255));

            if (slot.Weapon != null)
            {
                Raylib.DrawRectangleLinesEx(rect, isHovered ? 2.5f : 1.5f, slot.Weapon.RarityColor);
                DrawCenteredText(slot.Weapon.Definition.Name, rect, 13, Color.White);

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
                Raylib.DrawRectangleLinesEx(rect, 1f, new Color(45, 54, 72, 180));
                DrawCenteredText(SlotLabels[i], rect, 12, new Color(85, 100, 125, 200));
            }
        }

        Rectangle nextWaveBtn = new(1040f, 620f, 180f, 55f);
        bool btnHover = Raylib.CheckCollisionPointRec(mousePos, nextWaveBtn);

        Color btnBg = btnHover ? new Color(26, 175, 140, 255) : new Color(16, 135, 108, 255);
        Raylib.DrawRectangleRec(nextWaveBtn, btnBg);
        Raylib.DrawRectangleLinesEx(nextWaveBtn, 2f, new Color(80, 250, 205, 255));

        string btnText = "NEXT WAVE";
        int bw = Raylib.MeasureText(btnText, 16);
        Raylib.DrawText(btnText, (int)nextWaveBtn.X + ((int)nextWaveBtn.Width - bw) / 2, (int)nextWaveBtn.Y + 18, 16, Color.White);

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
}
