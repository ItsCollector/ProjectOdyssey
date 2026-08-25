using System.Collections.Concurrent;

namespace ProjectOdyssey
{
    public class InputHistory
    {
        private ConcurrentQueue<InputEvent> events = new();
        private HashSet<ushort>? keysDown;

        public void RecordInputEvent(InputEvent inputEvent, HashSet<ushort> keysDown) 
        {
            events.Enqueue(inputEvent);
            this.keysDown = keysDown;
        }

        public bool TryGetNextEvent(out InputEvent inputEvent)
        {
            return events.TryDequeue(out inputEvent);
        }
    }
}
