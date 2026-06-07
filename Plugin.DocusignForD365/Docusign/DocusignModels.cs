// Author: Adilson Capaia
// Blog: The Rational Developer
// Year: 2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Plugin.DocusignForD365.Docusign
{
    public class EnvelopeDefinition
    {
        [JsonPropertyName("templateId")]
        public string TemplateId { get; set; }

        [JsonPropertyName("emailSubject")]
        public string EmailSubject { get; set; }

        [JsonPropertyName("emailBlurb")]
        public string EmailBlurb { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("documents")]
        public List<Document> Documents { get; set; }

        [JsonPropertyName("recipients")]
        public Recipients Recipients { get; set; }

        [JsonPropertyName("templateRoles")]
        public List<TemplateRole> TemplateRoles { get; set; }
    }

    public class Document
    {
        [JsonPropertyName("documentBase64")]
        public string DocumentBase64 { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("fileExtension")]
        public string FileExtension { get; set; }

        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("order")]
        public string Order { get; set; }

        [JsonPropertyName("pages")]
        public string Pages { get; set; }

        [JsonPropertyName("transformPdfFields")]
        public string TransformPdfFields { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public Guid D365CoumentId { get; set; }
    }

    public class Recipients
    {
        [JsonPropertyName("signers")]
        public List<Signer> Signers { get; set; }

        [JsonPropertyName("carbonCopies")]
        public List<CarbonCopy> CarbonCopies { get; set; }

        [JsonPropertyName("inPersonSigners")]
        public List<InPersonSigner> InPersonSigners { get; set; }
    }

    public class Signer
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }

        [JsonPropertyName("routingOrder")]
        public string RoutingOrder { get; set; }

        [JsonPropertyName("tabs")]
        public Tabs Tabs { get; set; }
        [JsonPropertyName("roleName")]
        public string RoleName { get; set; }
        [JsonPropertyName("identityVerification")]
        public IdentityVerification IdentityVerification { get; set; }

        [JsonPropertyName("phoneAuthentication")]
        public PhoneAuthentication PhoneAuthentication { get; set; }

        [JsonPropertyName("smsAuthentication")]
        public SmsAuthentication SmsAuthentication { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string D365ClientId { get; set; }
        [JsonPropertyName("clientUserId")]
        public string ClientUserId { get; set; }
        [JsonPropertyName("idCheckConfigurationName")]
        public string IdCheckConfigurationName { get; set; }
        [JsonPropertyName("emailNotification")]
        public EmailNotification EmailNotification { get; set; }

    }
    public class EmailNotification
    {
        public string emailSubject { get; set; }
        public string emailBody { get; set; }
        public string supportedLanguage { get; set; }
    }
    public class InPersonSigner
    {
        [JsonPropertyName("hostEmail")]
        public string HostEmail { get; set; }

        [JsonPropertyName("hostName")]
        public string HostName { get; set; }

        [JsonPropertyName("signerName")]
        public string SignerName { get; set; }

        [JsonPropertyName("signerEmail")]
        public string SignerEmail { get; set; }

        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }

        [JsonPropertyName("routingOrder")]
        public string RoutingOrder { get; set; }

        [JsonPropertyName("tabs")]
        public Tabs Tabs { get; set; }
        [JsonPropertyName("smsAuthentication")]
        public SmsAuthentication SmsAuthentication { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string D365ClientId { get; set; }
        [JsonPropertyName("clientUserId")]
        public string ClientUserId { get; set; }
        [JsonPropertyName("idCheckConfigurationName")]
        public string IdCheckConfigurationName { get; set; }
        [JsonPropertyName("emailNotification")]
        public EmailNotification EmailNotification { get; set; }
    }
    public class CarbonCopy
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }

        [JsonPropertyName("routingOrder")]
        public string RoutingOrder { get; set; }
    }

    public class Tabs
    {
        [JsonPropertyName("signHereTabs")]
        public List<SignHereTab> SignHereTabs { get; set; }

        [JsonPropertyName("dateSignedTabs")]
        public List<DateSignedTab> DateSignedTabs { get; set; }

        [JsonPropertyName("textTabs")]
        public List<TextTab> TextTabs { get; set; }

        [JsonPropertyName("checkboxTabs")]
        public List<CheckboxTab> CheckboxTabs { get; set; }

        [JsonPropertyName("initialHereTabs")]
        public List<InitialHereTab> InitialHereTabs { get; set; }
    }



    public class DateSignedTab
    {
        [JsonPropertyName("anchorString")]
        public string AnchorString { get; set; }

        [JsonPropertyName("anchorXOffset")]
        public string AnchorXOffset { get; set; }

        [JsonPropertyName("anchorYOffset")]
        public string AnchorYOffset { get; set; }

        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }

        [JsonPropertyName("tabLabel")]
        public string TabLabel { get; set; }

        [JsonPropertyName("font")]
        public string Font { get; set; }

        [JsonPropertyName("fontSize")]
        public string FontSize { get; set; }

    }

    public class TextTab
    {
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("pageNumber")]
        public string PageNumber { get; set; }

        [JsonPropertyName("xPosition")]
        public string XPosition { get; set; }

        [JsonPropertyName("yPosition")]
        public string YPosition { get; set; }

        [JsonPropertyName("tabLabel")]
        public string TabLabel { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("required")]
        public string Required { get; set; }
    }

    public class CheckboxTab
    {
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("pageNumber")]
        public string PageNumber { get; set; }

        [JsonPropertyName("xPosition")]
        public string XPosition { get; set; }

        [JsonPropertyName("yPosition")]
        public string YPosition { get; set; }

        [JsonPropertyName("tabLabel")]
        public string TabLabel { get; set; }

        [JsonPropertyName("selected")]
        public string Selected { get; set; }
    }

    public class InitialHereTab
    {
        [JsonPropertyName("tabId")]
        public string TabId { get; set; }

        [JsonPropertyName("tabLabel")]
        public string TabLabel { get; set; }

        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("pageNumber")]
        public string PageNumber { get; set; }

        [JsonPropertyName("xPosition")]
        public string XPosition { get; set; }

        [JsonPropertyName("yPosition")]
        public string YPosition { get; set; }

        [JsonPropertyName("anchorString")]
        public string AnchorString { get; set; }

        [JsonPropertyName("anchorXOffset")]
        public string AnchorXOffset { get; set; }

        [JsonPropertyName("anchorYOffset")]
        public string AnchorYOffset { get; set; }

        [JsonPropertyName("anchorUnits")]
        public string AnchorUnits { get; set; }

        [JsonPropertyName("anchorCaseSensitive")]
        public string AnchorCaseSensitive { get; set; }

        [JsonPropertyName("anchorMatchWholeWord")]
        public string AnchorMatchWholeWord { get; set; }

        [JsonPropertyName("optional")]
        public string Optional { get; set; }

        [JsonPropertyName("scaleValue")]
        public string ScaleValue { get; set; }
        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }
    }

    public class TemplateRole
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("roleName")]
        public string RoleName { get; set; }

        [JsonPropertyName("tabs")]
        public Tabs Tabs { get; set; }
    }

    // Response model
    public class EnvelopeResponse
    {
        [JsonPropertyName("envelopeId")]
        public string EnvelopeId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("statusDateTime")]
        public string StatusDateTime { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }

    public class AddRecipientsRequest
    {
        [JsonPropertyName("signers")]
        public List<Signer> Signers { get; set; }
        [JsonPropertyName("inPersonSigners")]
        public List<InPersonSigner> InPersonSigners { get; set; }

        [JsonPropertyName("carbonCopies")]
        public List<CarbonCopy> CarbonCopies { get; set; }

        [JsonPropertyName("certifiedDeliveries")]
        public List<CertifiedDelivery> CertifiedDeliveries { get; set; }

        [JsonPropertyName("agents")]
        public List<Agent> Agents { get; set; }

        [JsonPropertyName("resendEnvelope")]
        public string ResendEnvelope { get; set; }
    }


    public class CertifiedDelivery
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }

        [JsonPropertyName("routingOrder")]
        public string RoutingOrder { get; set; }
    }

    public class Agent
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }

        [JsonPropertyName("routingOrder")]
        public string RoutingOrder { get; set; }
    }


    public class SignHereTab
    {
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("pageNumber")]
        public string PageNumber { get; set; }

        [JsonPropertyName("xPosition")]
        public string XPosition { get; set; }

        [JsonPropertyName("yPosition")]
        public string YPosition { get; set; }

        [JsonPropertyName("anchorString")]
        public string AnchorString { get; set; }

        [JsonPropertyName("anchorXOffset")]
        public string AnchorXOffset { get; set; }

        [JsonPropertyName("anchorYOffset")]
        public string AnchorYOffset { get; set; }

        [JsonPropertyName("optional")]
        public string Optional { get; set; }

        [JsonPropertyName("anchorIgnoreIfNotPresent")]
        public string AnchorIgnoreIfNotPresent { get; set; }

        [JsonPropertyName("anchorAllowWhiteSpaceInCharacters")]
        public string AnchorAllowWhiteSpaceInCharacters { get; set; }

        [JsonPropertyName("anchorMatchWholeWord")]
        public string AnchorMatchWholeWord { get; set; }
    }


    public class AddRecipientsResponse
    {
        [JsonPropertyName("recipientUpdateResults")]
        public List<RecipientUpdateResult> RecipientUpdateResults { get; set; }
    }

    public class RecipientUpdateResult
    {
        [JsonPropertyName("recipientId")]
        public string RecipientId { get; set; }

        [JsonPropertyName("errorDetails")]
        public ErrorDetails ErrorDetails { get; set; }

        [JsonPropertyName("success")]
        public string Success { get; set; }
    }

    public class ErrorDetails
    {
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class AddDocumentsRequest
    {
        [JsonPropertyName("documents")]
        public List<Document> Documents { get; set; }
    }



    public class AddDocumentsResponse
    {
        [JsonPropertyName("envelopeDocuments")]
        public List<EnvelopeDocument> EnvelopeDocuments { get; set; }
    }

    public class EnvelopeDocument
    {
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("documentIdGuid")]
        public string DocumentIdGuid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("order")]
        public string Order { get; set; }

        [JsonPropertyName("pages")]
        public string Pages { get; set; }
    }

    public class IdentityVerification
    {
        [JsonPropertyName("workflowId")]
        public string WorkflowId { get; set; }

        [JsonPropertyName("inputOptions")]
        public List<InputOption> InputOptions { get; set; }
    }

    public class InputOption
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("valueType")]
        public string ValueType { get; set; }

        [JsonPropertyName("phoneNumberList")]
        public List<PhoneNumber> PhoneNumberList { get; set; }
    }

    public class PhoneNumber
    {
        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }
    }

    public class PhoneAuthentication
    {
        [JsonPropertyName("recipMayProvideNumber")]
        public string RecipMayProvideNumber { get; set; }

        [JsonPropertyName("senderProvidedNumbers")]
        public List<string> SenderProvidedNumbers { get; set; }

        [JsonPropertyName("recordVoicePrint")]
        public string RecordVoicePrint { get; set; }
    }

    public class SmsAuthentication
    {
        [JsonPropertyName("senderProvidedNumbers")]
        public List<string> SenderProvidedNumbers { get; set; }
        [JsonPropertyName("senderProvidedNumber")]
        public SenderProvidedNumber SenderProvidedNumber { get; set; }
    }

    public class SenderProvidedNumber
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
    }
    // Request for creating sender view (edit URL)
    public class SenderViewRequest
    {
        [JsonPropertyName("returnUrl")]
        public string ReturnUrl { get; set; }
    }
    public class SenderViewResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class GetSignatureProcessConfigurationDataRequest
    {
        [DataMember(Name = "entityLogicalName")]
        public string EntityLogicalName { get; set; }

        [DataMember(Name = "recordId")]
        public string RecordId { get; set; }
    }

    public class GetSignatureProcessConfigurationDataResponse
    {
        [DataMember(Name = "envelopeTemplateId")]
        public string EnvelopeTemplateId { get; set; }

        [DataMember(Name = "emailSubject")]
        public string EmailSubject { get; set; }

        [DataMember(Name = "emailBody")]
        public string EmailBody { get; set; }
    }

    public class VoidEnvelopeRequest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("voidedReason")]
        public string VoidedReason { get; set; }
    }

    public class VoidEnvelopeResponse
    {
        [JsonPropertyName("voidedDateTime")]
        public string VoidedDateTime { get; set; }

        [JsonPropertyName("voidedReason")]
        public string VoidedReason { get; set; }
    }

    public class SendReminderRequest
    {
        [JsonPropertyName("reminderEnabled")]
        public string ReminderEnabled { get; set; }

        [JsonPropertyName("reminderDelay")]
        public string ReminderDelay { get; set; }

        [JsonPropertyName("reminderFrequency")]
        public string ReminderFrequency { get; set; }
    }

    public class SendReminderResponse
    {
        [JsonPropertyName("remindersSent")]
        public string RemindersSent { get; set; }
    }

    public class CompositeTemplateEnvelope
    {
        [JsonPropertyName("emailSubject")]
        public string EmailSubject { get; set; }

        [JsonPropertyName("emailBlurb")]
        public string EmailBlurb { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("compositeTemplates")]
        public List<CompositeTemplate> CompositeTemplates { get; set; }
    }

    public class CompositeTemplate
    {
        [JsonPropertyName("compositeTemplateId")]
        public string CompositeTemplateId { get; set; }

        [JsonPropertyName("serverTemplates")]
        public List<ServerTemplate> ServerTemplates { get; set; }

        [JsonPropertyName("inlineTemplates")]
        public List<InlineTemplate> InlineTemplates { get; set; }

        [JsonPropertyName("document")]
        public Document Document { get; set; }
    }

    public class ServerTemplate
    {
        [JsonPropertyName("sequence")]
        public string Sequence { get; set; }

        [JsonPropertyName("templateId")]
        public string TemplateId { get; set; }
    }

    public class InlineTemplate
    {
        [JsonPropertyName("sequence")]
        public string Sequence { get; set; }

        [JsonPropertyName("recipients")]
        public Recipients Recipients { get; set; }

        [JsonPropertyName("documents")]
        public List<Document> Documents { get; set; }
    }

    public class RecipientViewRequest
    {

        [JsonPropertyName("returnUrl")]
        public string ReturnUrl { get; set; }

        [JsonPropertyName("authenticationMethod")]
        public string AuthenticationMethod { get; set; }


        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }


        [JsonPropertyName("clientUserId")]
        public string ClientUserId { get; set; }

    }

    public class RecipientViewResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
