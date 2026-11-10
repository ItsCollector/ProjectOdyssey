namespace ProjectOdyssey.Diagnostics
{
    public sealed class FpsCounter
    {
        private double elapsed;
        private int frames;
        public double Fps { get; private set; }

        // Call once per rendered frame with the frame time in seconds (args.Time)
        public void Tick(double deltaSeconds)
        {
            elapsed += deltaSeconds;
            frames++;
            if (elapsed >= 0.5) // refresh twice a second so the number is readable
            {
                Fps = frames / elapsed;
                frames = 0;
                elapsed = 0;
            }
        }
    }
}
