using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sid.SmsKit.Data;

namespace Sid.SmsKit
{
    public class ClickatellClient : ISmsClient
    {
        #region Urls

        private const string SendMessageURL = "https://api.clickatell.com/rest/message";

        #endregion

        #region Private Properties

        private readonly RESTCredentials _credentials;

        #endregion

        #region Constructor
        public ClickatellClient(RESTCredentials credentials)
        {
            //Sets the REST API credentials
            _credentials = credentials;
        }

        #endregion

        #region Public Methods

        public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request)
        {
            using (var client = new HttpClient())
            {
                var requestJson = JsonConvert.SerializeObject(
                    new Data.Json.MessageRequest.Rootobject
                    {
                        text = request.Message,
                        to = request.PhoneNumbers
                    });

                try
                {
                    var response = await client.SendAsync(GetHttpRequestMessage(SendMessageURL, HttpMethod.Post, new StringContent(requestJson)));

                    if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.OK)
                        throw new SmsKitException($"Request failed. Received HTTP {response.StatusCode}");

                    var result = await response.Content.ReadAsStringAsync();
                    var resultObject = JsonConvert.DeserializeObject<Data.Json.MessageResponse.Rootobject>(result);

                    var jsonMessages = resultObject.data.message.Select(message => new Message
                    {
                        APIMessageID = message.apiMessageId,
                        To = message.to
                    }).ToArray();

                    return new SendMessageResponse
                    {
                        Result = result,
                        Success = true,
                        Messages = jsonMessages
                    };
                }
                catch (Exception ex)
                {
                    throw new SmsKitException($"Request failed. {ex.Message}");
                }
            }
        }

        #endregion

        #region Private Methods

        private HttpRequestMessage GetHttpRequestMessage(string url, HttpMethod method, HttpContent content = null)
        {
            var request = new HttpRequestMessage();

            request.Headers.Add("Authorization", $"bearer {_credentials.AuthenticationToken}");
            request.Headers.Add("X-Version", "1");
            request.Headers.Add("ContentType", "application/json");
            request.Headers.Add("Accept", "application/json");
            
            request.RequestUri = new Uri(url);
            request.Method = method;

            content.Headers.ContentType.MediaType = "application/json";
            request.Content = content;

            return request;
        }
        
        #endregion
    }
}
