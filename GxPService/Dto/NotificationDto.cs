using System;

namespace GxPService.Dto
{
    public class NotificationDto
    {
        public string MachineName { get; set; }
        public bool IsApplied { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
