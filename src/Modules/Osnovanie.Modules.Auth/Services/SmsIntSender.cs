using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Services;

public class SmsIntSender : ISmsSender
  {
      private readonly HttpClient _httpClient;
      private readonly ILogger<SmsIntSender> _logger;

      public SmsIntSender(HttpClient httpClient, ILogger<SmsIntSender> logger)
      {
          _httpClient = httpClient;
          _logger = logger;
      }

      public async Task<UnitResult<Error>> SendAsync(
          string phone, string message, CancellationToken cancellationToken)
      {
          var recipient = phone.TrimStart('+');

          var request = new SendSmsRequest
          {
              Messages = new[] { new SmsMessage { Recipient = recipient, Text = message } }
          };

          try
          {
              var response = await _httpClient.PostAsJsonAsync("sms/send/text", request, cancellationToken);
              var body = await response.Content.ReadFromJsonAsync<SendSmsResponse>(cancellationToken);

              if (body == null || !body.Success)
              {
                  _logger.LogError("SmsInt failed for {Phone}: code={Code} descr={Descr}",
                      phone, body?.Error?.Code, body?.Error?.Descr);
                  return AuthErrors.SmsSendFailed();
              }

              _logger.LogInformation("SMS sent to {Phone}", phone);
              return UnitResult.Success<Error>();
          }
          catch (Exception ex)
          {
              _logger.LogError(ex, "SmsInt exception for {Phone}", phone);
              return AuthErrors.SmsSendFailed();
          }
      }

      private class SendSmsRequest
      {
          [JsonPropertyName("messages")]
          public SmsMessage[] Messages { get; set; } = Array.Empty<SmsMessage>();
      }

      private class SmsMessage
      {
          [JsonPropertyName("recipient")]
          public string Recipient { get; set; } = string.Empty;

          [JsonPropertyName("text")]
          public string Text { get; set; } = string.Empty;
      }

      private class SendSmsResponse
      {
          [JsonPropertyName("success")]
          public bool Success { get; set; }

          [JsonPropertyName("error")]
          public SendSmsError? Error { get; set; }
      }

      private class SendSmsError
      {
          [JsonPropertyName("code")]
          public int Code { get; set; }

          [JsonPropertyName("descr")]
          public string Descr { get; set; } = string.Empty;
      }
  }