namespace Sid.SmsKit.Data.Json
{
    public class MessageRequest
    {
        public class Rootobject
        {
            public string text { get; set; }
            public string[] to { get; set; }
        }
    }
}
