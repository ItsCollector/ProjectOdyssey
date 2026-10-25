namespace ProjectOdyssey.Screens.Components
{
    /// <summary>
    /// A vertical stack of items that slide smoothly to their positions.
    /// The caller describes the items (Begin / Add / End) and the stack works out where each one
    /// belongs around an anchor item, then eases each item's Y toward that target every frame.
    /// </summary>
    public class StackView
    {
        private readonly float stackHeight;
        private readonly float moveSpeed;

        private readonly Dictionary<long, StackItem> byKey = new(); // persists between rebuilds so items keep their current Y
        private readonly List<StackItem> items = new(); // the items in the current layout, in stack order
        private readonly List<long> staleKeys = new(); // scratch list reused to avoid allocating
        private int generation; // bumped on every Begin() to spot items that weren't re-added

        public int Count => items.Count;
        public IReadOnlyList<StackItem> Items => items;

        /// <param name="stackHeight">Height of the area the stack lives in; the anchor item is centred in it.</param>
        /// <param name="moveSpeed">How quickly items chase their target. ~10 is gentle, ~20 is snappy.</param>
        public StackView(float stackHeight, float moveSpeed = 20)
        {
            this.stackHeight = stackHeight;
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
                anchorIndex = Math.Clamp(anchorIndex, 0, items.Count - 1);

                var anchor = items[anchorIndex];
                anchor.TargetY = stackHeight / 2f - anchor.Height / 2f;

                for (int i = anchorIndex + 1; i < items.Count; i++)
                {
                    items[i].TargetY = items[i - 1].TargetY + items[i - 1].Height + items[i].SpacingBefore;
                }

                for (int i = anchorIndex - 1; i >= 0; i--)
                {
                    items[i].TargetY = items[i + 1].TargetY - items[i].Height - items[i + 1].SpacingBefore;
                }

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
