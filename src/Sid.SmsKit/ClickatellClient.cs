using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        private readonly int _maxHandsetsCount = 500;

        private readonly int _bulkHandsetsInterval;

        private readonly ILogger _logger;

        #endregion

        #region Constructor
        public ClickatellClient(RESTCredentials credentials, ILogger logger = null, int bulkHandsetsInterval = 5000)
        {
            //Sets the REST API credentials
            _credentials = credentials;

            _bulkHandsetsInterval = bulkHandsetsInterval;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.Message))
            {
                throw new SmsKitException("SMS message can not be null.");
            }

            if (request.PhoneNumbers == null || !request.PhoneNumbers.Any())
            {
                throw new SmsKitException("Must provide at least one phone number.");
            }

            // If phone number more than 500, it needs to be sent multiple times.
            if (request.PhoneNumbers.Length <= _maxHandsetsCount)
            {
                return await Send(request);
            }
            else
            {
                _logger?.LogInformation($"Since more than {_maxHandsetsCount} phone number, so send it separately. It will send 500 at a time.");

                var messages = new List<Message>();
                var phoneNumbers = request.PhoneNumbers.ToList();

                while (phoneNumbers.Count > 0)
                {
                    var newRequest = new SendMessageRequest
                    {
                        Message = request.Message,
                        PhoneNumbers = phoneNumbers.Take(_maxHandsetsCount).ToArray()
                    };

                    var response = await Send(newRequest);

                    messages.AddRange(response.Messages);

                    phoneNumbers.RemoveRange(0, phoneNumbers.Count< _maxHandsetsCount? phoneNumbers.Count:_maxHandsetsCount);

                    await Task.Delay(_bulkHandsetsInterval);
                }

                return new SendMessageResponse
                {
                    Messages = messages.ToArray()
                };
            }
        }

        #endregion

        #region Private Methods

        private async Task<SendMessageResponse> Send(SendMessageRequest request)
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
                    _logger?.LogInformation($"Start to send sms. Message is {request.Message}. Phone numbers are {string.Join(",", request.PhoneNumbers)}");

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

                    var res = new SendMessageResponse
                    {
                        Result = result,
                        Success = true,
                        Messages = jsonMessages
                    };

                    _logger?.LogInformation($"Send sms successful. Response is {JsonConvert.SerializeObject(res)}");

                    return res;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Send sms failed. Error is {ex.Message}");

                    throw new SmsKitException($"Request failed. {ex.Message}");
                }
            }
        }

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
