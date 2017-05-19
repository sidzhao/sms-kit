using System;

namespace Sid.SmsKit
{
    public class SmsKitException : Exception
    {
        public SmsKitException(string message) : base(message) { }
    }
}
