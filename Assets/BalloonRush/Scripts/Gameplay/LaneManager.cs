using System;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class LaneManager : MonoBehaviour
    {
        [SerializeField] private Transform[] laneAnchors = new Transform[3];
        [SerializeField] private SpriteRenderer[] laneHighlights = new SpriteRenderer[3];
        [SerializeField] private Color selectedColor = new Color(0.1f, 0.95f, 1f, 0.24f);
        [SerializeField] private Color idleColor = new Color(0.05f, 0.12f, 0.3f, 0.10f);

        public int SelectedLane { get; private set; } = 1;
        public event Action<int> SelectedLaneChanged;

        public void Configure(Transform[] anchors, SpriteRenderer[] highlights)
        {
            laneAnchors = anchors;
            laneHighlights = highlights;
            UpdateHighlights();
        }

        public void ResetLane()
        {
            SelectedLane = 1;
            UpdateHighlights();
            SelectedLaneChanged?.Invoke(SelectedLane);
        }

        public void MoveLeft()
        {
            SetSelectedLane(SelectedLane - 1);
        }

        public void MoveRight()
        {
            SetSelectedLane(SelectedLane + 1);
        }

        public void SetSelectedLane(int lane)
        {
            int clamped = Mathf.Clamp(lane, 0, 2);
            if (clamped == SelectedLane)
            {
                return;
            }

            SelectedLane = clamped;
            UpdateHighlights();
            SelectedLaneChanged?.Invoke(SelectedLane);
        }

        public Vector3 GetLanePosition(int lane, float y)
        {
            int clamped = Mathf.Clamp(lane, 0, 2);
            if (laneAnchors != null && clamped < laneAnchors.Length && laneAnchors[clamped] != null)
            {
                Vector3 position = laneAnchors[clamped].position;
                position.y = y;
                return position;
            }

            return new Vector3((clamped - 1) * 2.4f, y, 0f);
        }

        private void UpdateHighlights()
        {
            if (laneHighlights == null)
            {
                return;
            }

            for (int i = 0; i < laneHighlights.Length; i++)
            {
                if (laneHighlights[i] != null)
                {
                    laneHighlights[i].color = i == SelectedLane ? selectedColor : idleColor;
                }
            }
        }
    }
}
