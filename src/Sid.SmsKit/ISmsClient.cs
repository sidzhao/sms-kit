using System.Threading.Tasks;
using Sid.SmsKit.Data;

namespace Sid.SmsKit
{
    public interface ISmsClient
    {
        /// <summary>
        /// Sends a message to the phonenumber(s) supplied.
        /// </summary>
        /// <param name="SendMessageRequest"></param>
        /// <returns>
        /// SendMessageResponse:
        /// Success = If call was successfully made to Clickatell
        /// Result  = Service response
        /// Messages[] = Message object which will have the APIMessageID(Guid created for message for reference) and To(The phonenumber)
        /// </returns>
        Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request);
    }
}
