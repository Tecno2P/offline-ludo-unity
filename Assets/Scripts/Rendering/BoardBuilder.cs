using System.Collections.Generic;
using LudoGame.Core;
using UnityEngine;

namespace LudoGame.Rendering
{
    public class BoardBuilder : MonoBehaviour
    {
        public float CellSize = 1f;
        private Dictionary<(int row, int col), Transform> _cellObjects = new Dictionary<(int, int), Transform>();

        private static readonly Dictionary<PlayerColor, Color> ColorMap = new Dictionary<PlayerColor, Color>
        {
            { PlayerColor.Red, new Color(0.85f, 0.18f, 0.18f) },
            { PlayerColor.Green, new Color(0.16f, 0.60f, 0.28f) },
            { PlayerColor.Yellow, new Color(0.95f, 0.78f, 0.10f) },
            { PlayerColor.Blue, new Color(0.15f, 0.35f, 0.80f) },
        };

        public static Color GetColor(PlayerColor color) => ColorMap[color];

        public Transform Build()
        {
            var root = new GameObject("Board").transform;
            root.SetParent(transform, false);

            DrawYards(root);
            DrawRingCells(root);
            DrawHomeStretches(root);
            DrawCenter(root);

            return root;
        }

        public Vector3 CellToWorld(int row, int col)
        {
            // Row 0 is the top of the board, so it maps to the highest Y.
            float x = col * CellSize;
            float y = (BoardLayout.GridSize - 1 - row) * CellSize;
            return new Vector3(x, y, 0f);
        }

        private void DrawYards(Transform root)
        {
            foreach (PlayerColor color in System.Enum.GetValues(typeof(PlayerColor)))
            {
                var (originRow, originCol) = BoardLayout.GetYardOrigin(color);
                var yardColor = ColorMap[color];
                var paleYard = Color.Lerp(yardColor, Color.white, 0.65f);

                // The 6x6 quadrant background.
                var quad = new GameObject($"Yard_{color}").AddComponent<SpriteRenderer>();
                quad.transform.SetParent(root, false);
                quad.sprite = ProceduralSprites.RoundedSquare(600, paleYard, yardColor, 0.06f);
                quad.transform.position = CellToWorld(originRow, originCol) + new Vector3(2.5f * CellSize, -2.5f * CellSize, 0f);
                quad.transform.localScale = Vector3.one * (CellSize * 6f);
                quad.sortingOrder = 0;

                // The 4 token-slot circles inside it.
                for (int t = 0; t < 4; t++)
                {
                    var (slotRow, slotCol) = BoardLayout.GetYardSlot(color, t);
                    var slot = new GameObject($"YardSlot_{color}_{t}").AddComponent<SpriteRenderer>();
                    slot.transform.SetParent(root, false);
                    slot.sprite = ProceduralSprites.Circle(200, Color.white, yardColor, 0.1f);
                    slot.transform.position = CellToWorld(slotRow, slotCol);
                    slot.transform.localScale = Vector3.one * (CellSize * 0.9f);
                    slot.sortingOrder = 1;
                }
            }
        }

        private void DrawRingCells(Transform root)
        {
            var safeGlobalCells = new HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };

            for (int i = 0; i < BoardLayout.RingCells.Length; i++)
            {
                var (row, col) = BoardLayout.RingCells[i];
                var cell = new GameObject($"Ring_{i}").AddComponent<SpriteRenderer>();
                cell.transform.SetParent(root, false);
                cell.sprite = ProceduralSprites.RoundedSquare(150, Color.white, new Color(0, 0, 0, 0.15f), 0.12f);
                cell.transform.position = CellToWorld(row, col);
                cell.transform.localScale = Vector3.one * (CellSize * 0.95f);
                cell.sortingOrder = 1;
                _cellObjects[(row, col)] = cell.transform;

                if (safeGlobalCells.Contains(i))
                {
                    var star = new GameObject($"Safe_{i}").AddComponent<SpriteRenderer>();
                    star.transform.SetParent(root, false);
                    star.sprite = ProceduralSprites.Star(100, new Color(0.6f, 0.6f, 0.6f, 0.6f), Color.clear);
                    star.transform.position = CellToWorld(row, col) + new Vector3(0, 0, -0.01f);
                    star.transform.localScale = Vector3.one * (CellSize * 0.5f);
                    star.sortingOrder = 2;
                }
            }
        }

        private void DrawHomeStretches(Transform root)
        {
            foreach (PlayerColor color in System.Enum.GetValues(typeof(PlayerColor)))
            {
                var cells = BoardLayout.GetHomeStretch(color);
                var tint = Color.Lerp(ColorMap[color], Color.white, 0.35f);
                for (int i = 0; i < cells.Length; i++)
                {
                    var cell = new GameObject($"Home_{color}_{i}").AddComponent<SpriteRenderer>();
                    cell.transform.SetParent(root, false);
                    cell.sprite = ProceduralSprites.RoundedSquare(150, tint, new Color(0, 0, 0, 0.15f), 0.12f);
                    cell.transform.position = CellToWorld(cells[i].row, cells[i].col);
                    cell.transform.localScale = Vector3.one * (CellSize * 0.95f);
                    cell.sortingOrder = 1;
                }
            }
        }

        private void DrawCenter(Transform root)
        {
            // Four colored triangles converging on the center "home" square - built from
            // simple quads rotated 90 degrees each, no external art.
            for (int i = 0; i < 4; i++)
            {
                var color = (PlayerColor)i;
                var tri = new GameObject($"CenterTriangle_{color}").AddComponent<SpriteRenderer>();
                tri.transform.SetParent(root, false);
                tri.sprite = ProceduralSprites.RoundedSquare(150, ColorMap[color], Color.clear, 0.05f);
                tri.transform.position = CellToWorld(BoardLayout.Center.row, BoardLayout.Center.col);
                tri.transform.localScale = Vector3.one * (CellSize * 0.9f);
                tri.transform.Rotate(0, 0, 90f * i);
                tri.sortingOrder = 1;
            }
        }
    }
}
