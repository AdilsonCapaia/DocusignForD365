// Author: Adilson Capaia
// Blog: The Rational Developer
// Year: 2026

using Microsoft.Xrm.Sdk;
using Plugin.DocusignForD365.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Plugin.DocusignForD365.Docusign.Services
{
    public class DocusignApiService
    {
        private ITracingService logger;
        private IOrganizationService service;
        private DocuSignAuthService authService;

        public DocusignApiService(
            ITracingService logger,
            IOrganizationService service,
            DocuSignAuthService authService
        )
        {
            this.logger = logger;
            this.service = service;
            this.authService = authService;
        }

        // DTOs for JSON serialization
        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        private class EnvelopeUpdateRequest
        {
            [JsonPropertyName("envelopeId")]
            public string EnvelopeId { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("voidedReason")]
            public string VoidedReason { get; set; }
        }

        private class DocuSignErrorResponse
        {
            [JsonPropertyName("errorCode")]
            public string ErrorCode { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }
        }

        private class EnvelopeViewRequest
        {
            [JsonPropertyName("envelopeId")]
            public string EnvelopeId { get; set; }
        }

        private class CorrectViewRequest
        {
            [JsonPropertyName("returnUrl")]
            public string ReturnUrl { get; set; }

            [JsonPropertyName("settings")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public CorrectViewSettings Settings { get; set; }
        }

        private class CorrectViewSettings
        {
            [JsonPropertyName("startingScreen")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string StartingScreen { get; set; }

            [JsonPropertyName("showBackButton")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string ShowBackButton { get; set; }

            [JsonPropertyName("showHeaderActions")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string ShowHeaderActions { get; set; }

            [JsonPropertyName("sendButtonAction")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string SendButtonAction { get; set; }

            [JsonPropertyName("backButtonAction")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string BackButtonAction { get; set; }
        }

        private class EnvelopeViewResponse
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public async Task<EnvelopeResponse> CreateEnvelopeAsync(EnvelopeDefinition envelopeDefinition)
        {

            return await this.CreateEnvelope(envelopeDefinition);

        }
        private async Task<EnvelopeResponse> CreateEnvelope(object envelopeDefinition)
        {
            var accountId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAccountId);
            var baseUri = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignBaseUrl);

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(baseUri))
            {
                throw new InvalidPluginExecutionException("DocuSign account configuration is missing required information.");
            }

            if (baseUri.EndsWith("/"))
            {
                baseUri = baseUri.TrimEnd('/');
            }

            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl);

            if (hasToken)
            {
                var accessToken = token.AccessToken;

                using (var httpClient = new HttpClient())
                {

                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var jsonContent = JsonSerializer.Serialize(envelopeDefinition,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

                        });

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var url = $"{baseUri}/restapi/v2.1/accounts/{accountId}/envelopes";
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("response content : " + responseContent);
                        return JsonSerializer.Deserialize<EnvelopeResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("error content : " + errorContent);
                        throw new Exception($"Error creating envelope: {response.StatusCode} - {errorContent}");

                    }
                }
            }
            else
            {
                throw new Exception($"Authentication Error:  it was not possible to create an access token");
            }
        }
        public async Task<AddRecipientsResponse> AddRecipientsToEnvelopeAsync(string envelopeId, AddRecipientsRequest recipients)
        {
            var accountId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAccountId);
            var baseUri = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignBaseUrl);

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(baseUri))
            {
                throw new InvalidPluginExecutionException("DocuSign account configuration is missing required information.");
            }

            if (baseUri.EndsWith("/"))
            {
                baseUri = baseUri.TrimEnd('/');
            }

            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl);
            if (hasToken)
            {
                var accessToken = token.AccessToken;
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var jsonContent = JsonSerializer.Serialize(recipients,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

                        });

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var url = $"{baseUri}/restapi/v2.1/accounts/{accountId}/envelopes/{envelopeId}/recipients";
                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("responseContent : " + responseContent);
                        return JsonSerializer.Deserialize<AddRecipientsResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("errorContent : " + errorContent);
                        throw new Exception($"Error adding recipients: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            else
            {
                throw new Exception($"Authentication Error:  it was not possible to create an access token");
            }
        }

        public async Task<EnvelopeResponse> CreateMinimalisticEnvelopeAsync(EnvelopeDefinition envelopeDefinition)
        {
            return await this.CreateEnvelope(envelopeDefinition);
        }

        public async Task<AddDocumentsResponse> AddDocumentsToEnvelopeAsync(string envelopeId, AddDocumentsRequest documents)
        {
            var accountId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAccountId);
            var baseUri = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignBaseUrl);

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(baseUri))
            {
                throw new InvalidPluginExecutionException("DocuSign account configuration is missing required information.");
            }

            if (baseUri.EndsWith("/"))
            {
                baseUri = baseUri.TrimEnd('/');
            }

            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl);
            if (hasToken)
            {
                var accessToken = token.AccessToken;
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var jsonContent = JsonSerializer.Serialize(documents,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

                        });

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var url = $"{baseUri}/restapi/v2.1/accounts/{accountId}/envelopes/{envelopeId}/documents";
                    var response = await httpClient.PutAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("Response : " + responseContent);
                        return JsonSerializer.Deserialize<AddDocumentsResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("errorContent : " + errorContent);
                        throw new Exception($"Error adding documents: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            else
            {
                throw new Exception($"Authentication Error:  it was not possible to create an access token");
            }
        }
        // Helper method to convert byte array to base64
        private string ConvertBytesToBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public async Task<string> GetEnvelopeViewUrlAsync(string envelopeId, SenderViewRequest viewRequest, string viewType)
        {

            var accountId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAccountId);
            var baseUri = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignBaseUrl);

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(baseUri))
            {
                throw new InvalidPluginExecutionException("DocuSign account configuration is missing required information.");
            }

            if (baseUri.EndsWith("/"))
            {
                baseUri = baseUri.TrimEnd('/');
            }

            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl);

            if (hasToken)
            {
                using (var httpClient = new HttpClient())
                {

                    var accessToken = token.AccessToken;

                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var jsonContent = JsonSerializer.Serialize(viewRequest,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

                        });


                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var url = $"{baseUri}/restapi/v2.1/accounts/{accountId}/";
                    if (viewType == "correct")
                        url += $"envelopes/{envelopeId}/views/correct";
                    else if (viewType == "edit")
                        url += $"envelopes/{envelopeId}/views/edit";
                    else
                        url += "views/console";

                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("responseContent : " + responseContent);
                        var viewResponse = JsonSerializer.Deserialize<SenderViewResponse>(responseContent);
                        return viewResponse.Url;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("errorContent : " + errorContent);
                        throw new Exception($"Error getting sender view: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            else
            {
                throw new Exception($"Authentication Error:  it was not possible to create an access token");
            }
        }

        public async Task<VoidEnvelopeResponse> VoidEnvelopeAsync(string envelopeId, string reason)
        {

            var accountId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAccountId);
            var baseUri = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignBaseUrl);

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(baseUri))
            {
                throw new InvalidPluginExecutionException("DocuSign account configuration is missing required information.");
            }

            if (baseUri.EndsWith("/"))
            {
                baseUri = baseUri.TrimEnd('/');
            }

            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl);
            if (hasToken)
            {

                using (var httpClient = new HttpClient())
                {
                    var accessToken = token.AccessToken;
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var voidRequest = new VoidEnvelopeRequest
                    {
                        Status = "voided",
                        VoidedReason = reason
                    };

                    var jsonContent = JsonSerializer.Serialize(voidRequest,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase

                        });

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var url = $"{baseUri}/restapi/v2.1/accounts/{accountId}/envelopes/{envelopeId}";

                    var response = await httpClient.PutAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("responseContent : " + responseContent);
                        return JsonSerializer.Deserialize<VoidEnvelopeResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("errorContent : " + errorContent);
                        throw new Exception($"Error voiding envelope: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            else
            {
                throw new Exception($"Authentication Error:  it was not possible to create an access token");
            }

        }

        public async Task<SendReminderResponse> SendReminderAsync(string envelopeId)
        {
            var accountId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAccountId);
            var baseUri = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignBaseUrl);

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(baseUri))
            {
                throw new InvalidPluginExecutionException("DocuSign account configuration is missing required information.");
            }

            if (baseUri.EndsWith("/"))
            {
                baseUri = baseUri.TrimEnd('/');
            }

            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl);
            if (hasToken)
            {
                using (var httpClient = new HttpClient())
                {
                    var accessToken = token.AccessToken;
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var url = $"{baseUri}/restapi/v2.1/accounts/{accountId}/envelopes/{envelopeId}";

                    // Use PUT with resend_envelope=true
                    var fullUrl = $"{url}?resend_envelope=true";

                    // Empty body for resend
                    var content = new StringContent("{}", Encoding.UTF8, "application/json");
                    var response = await httpClient.PutAsync(fullUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        // If response is empty or success, return a simple confirmation
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            return new SendReminderResponse { RemindersSent = "true" };
                        }

                        return JsonSerializer.Deserialize<SendReminderResponse>(responseContent)
                               ?? new SendReminderResponse { RemindersSent = "true" };
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error sending reminder: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            else
            {
                throw new Exception($"Authentication Error:  it was not possible to create an access token");
            }


        }

        public async Task<EnvelopeResponse> CreateEnvelopeWithCompositeTemplatesAsync(CompositeTemplateEnvelope envelope)
        {
            return await this.CreateEnvelope(envelope);
        }

        public async Task<string> GetEnvelopeRecipientViewUrlAsync(string envelopeId, RecipientViewRequest viewRequest, Guid? AccountServiceCrmUserId = null)
        {
            var accountId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAccountId);
            var baseUri = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignBaseUrl);

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(baseUri))
            {
                throw new InvalidPluginExecutionException("DocuSign account configuration is missing required information.");
            }

            if (baseUri.EndsWith("/"))
            {
                baseUri = baseUri.TrimEnd('/');
            }

            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl, AccountServiceCrmUserId);

            if (hasToken)
            {
                using (var httpClient = new HttpClient())
                {

                    var accessToken = token.AccessToken;

                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var jsonContent = JsonSerializer.Serialize(viewRequest,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });


                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var url = $"{baseUri}/restapi/v2.1/accounts/{accountId}/envelopes/{envelopeId}/views/recipient";


                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("responseContent : " + responseContent);
                        var viewResponse = JsonSerializer.Deserialize<RecipientViewResponse>(responseContent,
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            }
                        );
                        return viewResponse.Url;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.Trace("errorContent : " + errorContent);
                        throw new Exception($"Error getting recipient view: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            else
            {
                throw new Exception($"Authentication Error:  it was not possible to create an access token");
            }
        }

        public DocuSignBaseResponse GetIndividualUrlConsent()
        {
            var hasToken = authService.RequestJwtUserToken(out var token, out var consentUrl);
            return new DocuSignBaseResponse() { ConsentUrl = consentUrl, Success = hasToken };
        }
    }

    public class DocuSignBaseResponse
    {
        public string ConsentUrl { get; set; }
        public bool Success { get; set; }
    }

    public class DocuSignViewUrlResponse : DocuSignBaseResponse
    {
        public string ViewUrl { get; set; }
    }
}
