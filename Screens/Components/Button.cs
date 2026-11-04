namespace ProjectOdyssey.Screens.Components
{
    public class Button
    {
        public Rect Rect { get; private set; }
        public bool IsHovered { get; private set; } = false;

        private readonly Action OnClick;

        public Button(float centerX, float centerY, float width, float height, Action onClick)
        {
            Rect = Rect.FromCenter(centerX, centerY, width, height);
            this.OnClick = onClick;
        }

        public void UpdateHoverState(float mouseX, float mouseY)
        {
            IsHovered = Rect.Contains(mouseX, mouseY);
        }

        // Call from OnMouseDown. Returns true if this button was hovered and handled the click.
        public bool TryClick()
        {
            if (!IsHovered) return false;

            OnClick();
            return true;
        }
    }
}
