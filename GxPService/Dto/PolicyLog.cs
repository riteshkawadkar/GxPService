namespace GxPService.Dto
{
    public class PolicyLog
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Entry { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string RegType { get; set; }
        public string WindowsOperatingSystem { get; set; }
        public string State { get; set; }
        public string Timestamp { get; set; }
        public string ServerTimestamp { get; set; }
    }
}
