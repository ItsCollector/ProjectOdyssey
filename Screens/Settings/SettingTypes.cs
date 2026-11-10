namespace ProjectOdyssey.Screens.Settings
{
    public class BoolSetting
    {
        public string Label { get; }
        public Func<bool> Get { get; }
        public Action<bool> Set { get; }

        public BoolSetting(string label, Func<bool> get, Action<bool> set)
        { 
            Label = label; 
            Get = get; 
            Set = set; 
        }
    }
}
