using System.Collections.Concurrent;

namespace ProjectOdyssey
{
    public class InputHistory
    {
        private ConcurrentQueue<InputEvent> events = new();
        
        public void RecordInputEvent(InputEvent inputEvent) 
        {
            events.Enqueue(inputEvent);
        }

        public bool TryGetNextEvent(out InputEvent inputEvent)
        {
            return events.TryDequeue(out inputEvent);
        }
    }
}
