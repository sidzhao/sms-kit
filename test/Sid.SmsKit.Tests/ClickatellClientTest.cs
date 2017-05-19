using System.Threading.Tasks;
using Sid.SmsKit.Data;
using Xunit;

namespace Sid.SmsKit.Tests
{
    public class ClickatellClientTest
    {
        [Fact]
        public async Task TestSendSms()
        {
            // Need to use real phone number
            var client = new ClickatellClient(new RESTCredentials("1NddRb4U6If7rSKAiVRkUXvLCUM31VTLloXNWAQBYyC1oDgVxfcTdmpHTzMcAduVE3mPhEPvnbiAX1"));
            var response = await client.SendMessageAsync(new SendMessageRequest("test message", "86xxxxx"));
            Assert.True(response.Success);
            
            await Assert.ThrowsAnyAsync<SmsKitException>(async ()=> await client.SendMessageAsync(new SendMessageRequest("test message", "xxxxxxx")));
        }
    }
}
