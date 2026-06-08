// Author: Adilson Capaia
// Blog: The Rational Developer
// Year: 2026

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin.DocusignForD365.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Plugin.DocusignForD365.Docusign.Services
{
    public class DocuSignAuthService
    {
        private ITracingService logger;
        private HttpClient httpClient;
        private IOrganizationService service;
        private IPluginExecutionContext executionContext;

        public DocuSignAuthService(
            ITracingService logger,
            IOrganizationService service,
            IPluginExecutionContext executionContext
        )
        {
            this.logger = logger;
            this.httpClient = new HttpClient();
            this.service = service;
            this.executionContext = executionContext;
        }
        public bool RequestJwtUserToken(
            out OAuthToken oAuthToken,
            out string consentUrl,
            Guid? AccountServiceCrmUserId = null
        )
        {
            logger.Trace(MethodBase.GetCurrentMethod().Name);
            oAuthToken = null;
            consentUrl = null;

            const int expiresInHours = 1;
            var userIdCrm = executionContext.InitiatingUserId;

            // if you provide AccountServiceCrmUserId, then it will use you admin account, the one that you used to install Docusign ISV.
            // Otherwise it will use current user info, so the user must also exist on Docusign.This case allows multi user management, each sales agent has it owns account and separates selectronic signature by user, business unit etc. Good for large business scenario
            if (AccountServiceCrmUserId != null)
                userIdCrm = (Guid)AccountServiceCrmUserId;

            // Get the DocuSign User ID directly from the SystemUser record
            var userEntity = service.Retrieve("systemuser", userIdCrm, new ColumnSet("trd_docusignuserid"));
            var userId = userEntity.GetAttributeValue<string>("trd_docusignuserid");
            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("DocuSign user ID not found on the current user record. Please fill in the trd_docusignuserid field on the System User.");
            }

            var scopes = new List<string>
            {
                "signature",
                "impersonation",
            };

            var baseUrl = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignAuthBaseUrl);
            var privateKeyPem = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignPrivateKey);
            var clientId = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.DocuSignIntegrationKey);

            if (string.IsNullOrEmpty(userId))
            {
                throw new Exception("User Id not supplied or is invalid!");
            }

            if (string.IsNullOrEmpty(privateKeyPem))
            {
                throw new Exception("Private key not supplied or is invalid!");
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new Exception("Integration Key not supplied or is invalid!");
            }

            // Get the D365 environment URL for the redirect
            var environmentUrl = EnvironmentVariableHelper.GetValue(service, EnvironmentVariableNames.EnvironmentBaseUrl) ?? "";

            // Ensure the URL ends with a slash
            if (!environmentUrl.EndsWith("/"))
            {
                environmentUrl += "/";
            }

            var returnUrl = $"{environmentUrl}webresources/trd_/html/docusign_iframe.html";

            // Create JWT token
            var jwtToken = CreateJwtToken(clientId, userId, baseUrl, privateKeyPem, expiresInHours, scopes);

            // Prepare request
            var requestContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
            new KeyValuePair<string, string>("assertion", jwtToken)
            });

            var tokenUrl = $"https://{baseUrl}/oauth/token";

            try
            {
                var response = httpClient.PostAsync(tokenUrl, requestContent).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    oAuthToken = JsonSerializer.Deserialize<OAuthToken>(responseContent);
                    consentUrl = $"https://{baseUrl}/oauth/auth?response_type=code&scope=impersonation%20signature&client_id={clientId}&redirect_uri={Uri.EscapeDataString(returnUrl)}";
                    return true;
                }

                // Try check if we have consent required error
                var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(errorContent) && errorContent.Contains("consent_required"))
                {
                    logger.Trace("DocuSign consent required for the user.");
                    consentUrl = $"https://{baseUrl}/oauth/auth?response_type=code&scope=impersonation%20signature&client_id={clientId}&redirect_uri={Uri.EscapeDataString(returnUrl)}";
                    logger.Trace(consentUrl);
                    return false;
                }

                throw new Exception(
                    $"Error retrieving DocuSign token: {response.StatusCode} - {response.ReasonPhrase} - {errorContent}");
            }
            catch (Exception ex)
            {
                logger.Trace($"Error retrieving DocuSign token: {ex.Message}");
                throw new Exception($"Error retrieving DocuSign token: {ex.Message}", ex);
            }
        }

        private string CreateJwtToken(
            string clientId,
            string userId,
            string oAuthBasePath,
            string privateKeyPem,
            int expiresInHours,
            List<string> scopes)
        {
            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(expiresInHours);

            // Create JWT header
            var header = new
            {
                alg = "RS256",
                typ = "JWT"
            };

            // Create JWT payload
            var payload = new
            {
                iss = clientId,
                sub = userId,
                aud = oAuthBasePath,
                iat = ToUnixTimestamp(now),
                exp = ToUnixTimestamp(expiresAt),
                scope = string.Join(" ", scopes)
            };

            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);

            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            var signatureInput = $"{headerBase64}.{payloadBase64}";
            var signature = SignWithRsa(signatureInput, privateKeyPem);
            var signatureBase64 = Base64UrlEncode(signature);

            return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        }

        private byte[] SignWithRsa(string data, string privateKeyPem)
        {
            using (var rsa = CreateRsaKeyFromPem(privateKeyPem))
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                return rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        private RSACryptoServiceProvider CreateRsaKeyFromPem(string privateKeyPem)
        {
            // Remove PEM headers and footers
            var pemContent = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            var privateKeyBytes = Convert.FromBase64String(pemContent);

            // Parse the ASN.1 DER encoded key
            RSAParameters rsaParams;

            try
            {
                // Try PKCS#8 format first (-----BEGIN PRIVATE KEY-----)
                rsaParams = DecodePrivateKeyInfo(privateKeyBytes);
            }
            catch
            {
                // Fall back to PKCS#1 format (-----BEGIN RSA PRIVATE KEY-----)
                rsaParams = DecodeRsaPrivateKey(privateKeyBytes);
            }

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaParams);
            return rsa;
        }

        private RSAParameters DecodePrivateKeyInfo(byte[] privateKey)
        {
            // PKCS#8 format decoder
            using (var stream = new MemoryStream(privateKey))
            {
                using (var reader = new BinaryReader(stream))
                {

                    // Skip SEQUENCE tag and length
                    if (reader.ReadByte() != 0x30) throw new InvalidDataException("Invalid key format");
                    ReadLength(reader);

                    // Skip version
                    if (reader.ReadByte() != 0x02) throw new InvalidDataException("Invalid key format");
                    ReadLength(reader);
                    reader.ReadByte(); // version number

                    // Skip algorithm identifier
                    if (reader.ReadByte() != 0x30) throw new InvalidDataException("Invalid key format");
                    ReadLength(reader);
                    // Skip OID and NULL
                    var oidLength = ReadLength(reader);
                    reader.ReadBytes((int)oidLength);
                    if (reader.ReadByte() == 0x05) reader.ReadByte(); // NULL

                    // Read the private key OCTET STRING
                    if (reader.ReadByte() != 0x04) throw new InvalidDataException("Invalid key format");
                    ReadLength(reader);

                    // Now we have the RSA private key in PKCS#1 format
                    return DecodeRsaPrivateKey(reader.ReadBytes((int)(stream.Length - stream.Position)));
                }
            }

        }

        private RSAParameters DecodeRsaPrivateKey(byte[] privateKey)
        {
            // PKCS#1 format decoder
            using (var stream = new MemoryStream(privateKey))
            {
                using (var reader = new BinaryReader(stream))
                {
                    // Skip SEQUENCE tag and length
                    if (reader.ReadByte() != 0x30) throw new InvalidDataException("Invalid RSA key format");
                    ReadLength(reader);

                    // Skip version
                    if (reader.ReadByte() != 0x02) throw new InvalidDataException("Invalid RSA key format");
                    var versionLength = ReadLength(reader);
                    reader.ReadBytes((int)versionLength);

                    var rsaParams = new RSAParameters
                    {
                        Modulus = ReadInteger(reader),
                        Exponent = ReadInteger(reader),
                        D = ReadInteger(reader),
                        P = ReadInteger(reader),
                        Q = ReadInteger(reader),
                        DP = ReadInteger(reader),
                        DQ = ReadInteger(reader),
                        InverseQ = ReadInteger(reader)
                    };

                    return rsaParams;
                }
            }
        }

        private byte[] ReadInteger(BinaryReader reader)
        {
            if (reader.ReadByte() != 0x02) throw new InvalidDataException("Expected INTEGER tag");
            var length = ReadLength(reader);
            var data = reader.ReadBytes((int)length);

            // Remove leading zero if present (used for positive number indication)
            if (data.Length > 0 && data[0] == 0x00 && data.Length > 1)
            {
                var trimmed = new byte[data.Length - 1];
                Array.Copy(data, 1, trimmed, 0, trimmed.Length);
                return trimmed;
            }

            return data;
        }

        private uint ReadLength(BinaryReader reader)
        {
            var firstByte = reader.ReadByte();

            if ((firstByte & 0x80) == 0)
            {
                // Short form
                return firstByte;
            }
            else
            {
                // Long form
                var lengthBytes = firstByte & 0x7F;
                uint length = 0;

                for (int i = 0; i < lengthBytes; i++)
                {
                    length = (length << 8) | reader.ReadByte();
                }

                return length;
            }
        }

        private string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private long ToUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public class OAuthToken
        {
            [JsonPropertyName("access_token")] public string AccessToken { get; set; }

            [JsonPropertyName("token_type")] public string TokenType { get; set; }

            [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

            [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
        }

    }
}
