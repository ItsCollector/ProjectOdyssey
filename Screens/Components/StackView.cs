namespace ProjectOdyssey.Screens.Components
{
    /// <summary>
    /// A vertical stack of items that slide smoothly to their positions.
    /// The caller describes the items (Begin / Add / End) and the stack works out where each one
    /// belongs around an anchor item, then eases each item's Y toward that target every frame.
    /// </summary>
    public class StackView
    {
        private float OriginalCenterY;
        public float CenterY { get; set; }
        private readonly float moveSpeed;
        private readonly float viewportHeight = 1080;
        private readonly float snapBackSpeed = 12f; // how quickly an over-scrolled list eases home
        private int anchorIndex;

        /// <summary>How far (px) the player can drag past either end before the wheel stops pulling further.</summary>
        public float MaxOverscroll { get; set; } = 120f;

        private readonly Dictionary<long, StackItem> byKey = new(); // persists between rebuilds so items keep their current Y
        private readonly List<StackItem> items = new(); // the items in the current layout, in stack order
        private readonly List<long> staleKeys = new(); // scratch list reused to avoid allocating
        private int generation; // bumped on every Begin() to spot items that weren't re-added

        public int Count => items.Count;
        public IReadOnlyList<StackItem> Items => items;

        /// <param name="stackHeight">Height of the area the stack lives in; the anchor item is centred in it.</param>
        /// <param name="moveSpeed">How quickly items chase their target. ~10 is gentle, ~20 is snappy.</param>
        public StackView(float centerY, float moveSpeed = 20)
        {
            this.CenterY = centerY;
            this.OriginalCenterY = centerY;
            this.moveSpeed = moveSpeed;
        }

        /// <summary>Starts a new layout. Call Add() for each item in order, then End().</summary>
        public void Begin()
        {
            items.Clear();
            generation++;
        }

        /// <summary>
        /// Adds the next item to the stack. The key must uniquely identify the item and stay the same between
        /// rebuilds, so the item can slide from where it was instead of appearing in its new spot.
        /// </summary>
        /// <param name="tag">Anything the caller wants to read back later (e.g. a chart index) when hit-testing.</param>
        public StackItem Add(long key, float x, float width, float height, float spacingBefore, int tag = 0, int kind = 0, StackItem? spawnFrom = null)
        {
            if (!byKey.TryGetValue(key, out var item))
            {
                item = new StackItem { IsNew = true };   // only allocates the first time this key is seen
                byKey[key] = item;
            }

            item.X = x;
            item.Width = width;
            item.Height = height;
            item.SpacingBefore = spacingBefore;
            item.Tag = tag;
            item.Kind = kind;
            item.SpawnFrom = spawnFrom;
            item.LastSeen = generation;

            items.Add(item);
            return item;
        }

        /// <summary>Finishes the layout: centres the anchor item and stacks the rest above and below it.</summary>
        public void End(int anchorIndex)
        {
            if (items.Count > 0)
            {
                this.anchorIndex = Math.Clamp(anchorIndex, 0, items.Count - 1);
                ComputeTargets();

                // Items seen for the first time appear in place; only existing items slide.
                foreach (var item in items)
                {
                    if (item.IsNew)
                    {
                        item.Y = item.SpawnFrom?.Y ?? item.TargetY; // start under the parent if there is one, else appear in place
                        item.IsNew = false;
                    }

                    item.SpawnFrom = null;
                }
            }

            // Forget items that weren't added this round so the dictionary doesn't grow as the player scrolls.
            staleKeys.Clear();
            foreach (var pair in byKey)
            {
                if (pair.Value.LastSeen != generation)
                {
                    staleKeys.Add(pair.Key);
                }
            }
            foreach (long key in staleKeys)
            {
                byKey.Remove(key);
            }
        }

        /// <summary>Recomputes every item's TargetY from the current CenterY. Cheap; no allocation.</summary>
        private void ComputeTargets()
        {
            if (items.Count == 0) return;

            var anchor = items[anchorIndex];
            anchor.TargetY = CenterY - anchor.Height / 2f;

            for (int i = anchorIndex + 1; i < items.Count; i++)
            {
                items[i].TargetY = items[i - 1].TargetY + items[i - 1].Height + items[i].SpacingBefore;
            }

            for (int i = anchorIndex - 1; i >= 0; i--)
            {
                items[i].TargetY = items[i + 1].TargetY - items[i].Height - items[i + 1].SpacingBefore;
            }
        }

        /// <summary>
        /// Scrolls the stack by a number of pixels (positive = content moves down). The stack may be dragged
        /// a little past its limits (rubber band); SnapBack() then eases it home.
        /// </summary>
        public void Scroll(float pixels)
        {
            if (items.Count == 0) return;

            GetScrollLimits(out float min, out float max);
            CenterY = Math.Clamp(CenterY + pixels, min - MaxOverscroll, max + MaxOverscroll);
            ComputeTargets();
        }

        /// <summary>Call every frame. Moves each item's Y a fraction of the way toward its target.</summary>
        public void Update(float deltaMs)
        {
            float blend = 1f - MathF.Exp(-moveSpeed * deltaMs / 1000f);

            foreach (var item in items)
            {
                float distance = item.TargetY - item.Y;

                if (MathF.Abs(distance) < 0.5f)
                {
                    item.Y = item.TargetY; // close enough: stop
                }
                else
                {
                    item.Y += distance * blend;
                }
            }
        }

        /// <summary>
        /// Call every frame. If the stack has been scrolled past either end (best score no longer at the top,
        /// or the last score pulled above the bottom of the view), eases it back inside its limits.
        /// </summary>
        public void SnapBack(float deltaMs)
        {
            if (items.Count == 0) return;

            GetScrollLimits(out float min, out float max);
            float targetCenterY = Math.Clamp(CenterY, min, max);
            float distance = targetCenterY - CenterY;
            if (distance == 0f) return;

            float blend = 1f - MathF.Exp(-snapBackSpeed * deltaMs / 1000f);

            if (MathF.Abs(distance) < 0.5f)
            {
                CenterY = targetCenterY;
            }
            else
            {
                CenterY += distance * blend;
            }

            ComputeTargets();
        }

        /// <summary>
        /// The range CenterY may rest in. max = the original position (first item at the top);
        /// min = the position where the last item's bottom edge sits on the bottom of the viewport.
        /// If everything fits on screen, min == max and the list can't scroll at all.
        /// </summary>
        private void GetScrollLimits(out float min, out float max)
        {
            max = OriginalCenterY;

            // Height from the first item's top edge to the last item's bottom edge (no spacing before the first item).
            float contentHeight = items[0].Height;
            for (int i = 1; i < items.Count; i++)
            {
                contentHeight += items[i].SpacingBefore + items[i].Height;
            }

            float restingTop = OriginalCenterY - items[0].Height / 2f;
            float overflow = Math.Max(0f, contentHeight - (viewportHeight - restingTop));
            min = max - overflow;
        }
    }

    public class StackItem
    {
        public float X;
        public float Y; // where it is drawn right now (animated)
        public float Width;
        public float Height;
        public float SpacingBefore;
        public float TargetY; // where it belongs; Y chases this
        public int Tag; // caller-defined, e.g. set slot or chart index
        public int Kind;
        internal StackItem? SpawnFrom;

        internal bool IsNew;
        internal int LastSeen;

        // Hit-test against the animated position, since that's what the player sees under the mouse.
        public bool Contains(float px, float py)
        {
            return px >= X && px < X + Width && py >= Y && py < Y + Height;
        }
    }
}