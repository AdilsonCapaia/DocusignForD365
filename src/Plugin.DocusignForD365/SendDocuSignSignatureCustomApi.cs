// Author: Adilson Capaia
// Blog: The Rational Developer
// Year: 2026

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin.DocusignForD365.Docusign;
using Plugin.DocusignForD365.Docusign.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Plugin.DocusignForD365
{
    /// <summary>
    /// Custom API plugin that sends a DocuSign signature request.
    /// Register this class as a Custom API plugin step in D365.
    /// </summary>
    public class SendDocuSignSignatureCustomApi : IPlugin
    {
        // Input parameter names (match the Custom API definition in D365)
        private const string Param_EntityLogicalName = "EntityLogicalName";
        private const string Param_RecordId = "RecordId";
        private const string Param_EnvelopeTemplateId = "EnvelopeTemplateId";
        private const string Param_EmailSubject = "EmailSubject";
        private const string Param_EmailBody = "EmailBody";
        private const string Param_IsInPerson = "IsInPerson";

        // Output parameter names
        private const string Param_EnvelopeId = "EnvelopeId";
        private const string Param_EnvelopeStatus = "EnvelopeStatus";
        private const string Param_EnvelopeUri = "EnvelopeUri";

        private string _emailSubject;
        private string _emailBody;

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                tracingService.Trace("SendDocuSignSignatureCustomApi - Execution started.");

                // Read input parameters from external caller
                var inputParams = context.InputParameters;

                string entityLogicalName = inputParams.Contains(Param_EntityLogicalName) ? inputParams[Param_EntityLogicalName] as string : null;
                string recordId = inputParams.Contains(Param_RecordId) ? inputParams[Param_RecordId] as string : null;
                string envelopeTemplateId = inputParams.Contains(Param_EnvelopeTemplateId) ? inputParams[Param_EnvelopeTemplateId] as string : null;
                string emailSubject = inputParams.Contains(Param_EmailSubject) ? inputParams[Param_EmailSubject] as string : string.Empty;
                string emailBody = inputParams.Contains(Param_EmailBody) ? inputParams[Param_EmailBody] as string : string.Empty;
                bool? isInPerson = inputParams.Contains(Param_IsInPerson) ? inputParams[Param_IsInPerson] as bool? : false;

                _emailBody = emailBody;
                _emailSubject = emailSubject;

                // Docusign has a maximum of 100 caracters for email subject
                if (emailSubject.Length > 100)
                {
                    emailSubject = emailSubject.Substring(0, 100);
                    _emailSubject = emailSubject;
                }

                // Get recipients
                Recipients recipients = new Recipients();
                
                recipients = BuildRecipients(service, tracingService, new Guid(recordId), (bool)isInPerson, context.UserId);

                tracingService.Trace("recipients = " + JsonSerializer.Serialize(recipients,
                    new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    }));

                // Get your physical documents, that are attached to your functional entity ( Contract, Proposal etc etc)
                var documents = RetrieveDocuments(service, tracingService, recordId);
                tracingService.Trace("NB of documents = " + documents.Count);

                // Build composite template envelope
                var envelope = new CompositeTemplateEnvelope
                {
                    Status = "sent",
                    CompositeTemplates = new List<CompositeTemplate>
                    {
                        new CompositeTemplate
                        {
                            CompositeTemplateId = "1",
                            ServerTemplates = new List<ServerTemplate>
                            {
                                new ServerTemplate
                                {
                                    Sequence = "1",
                                    TemplateId = envelopeTemplateId
                                }
                            },
                            InlineTemplates = new List<InlineTemplate>
                            {
                                new InlineTemplate
                                {
                                    Sequence = "1",
                                    Recipients = recipients,
                                    Documents = documents,
                                }
                            }
                        }
                    }
                };

                if (!string.IsNullOrEmpty(emailSubject))
                    envelope.EmailBlurb = emailBody;

                // Here we create the Auth and DS Api services 
                var authService = new DocuSignAuthService(tracingService, service, context);
                var apiService = new DocusignApiService(tracingService, service, authService);

                var response = apiService.CreateEnvelopeWithCompositeTemplatesAsync(envelope).GetAwaiter().GetResult();
                tracingService.Trace("Envelope created successfully!");
                tracingService.Trace("Envelope ID: " + response.EnvelopeId);
                tracingService.Trace("Status: " + response.Status);

                // Set output parameters
                context.OutputParameters[Param_EnvelopeId] = response.EnvelopeId;
                context.OutputParameters[Param_EnvelopeStatus] = response.Status;
                context.OutputParameters[Param_EnvelopeUri] = response.Uri;

                // Create the D365 envelope tracking record
                var d365EnvelopeId = CreateEnvelopeSignatureRecord(
                    service,
                    tracingService,
                    response.EnvelopeId,
                    envelopeTemplateId,
                    new Guid(recordId),
                    entityLogicalName,
                    context.UserId);

                tracingService.Trace("Docusign Envelope record created successfully!");

                // Create envelope document records
                CreateEnvelopeDocumentsFromRecipients(service, tracingService, documents, d365EnvelopeId);
                tracingService.Trace("Docusign Envelope Documents record created successfully!");

                tracingService.Trace("SendDocuSignSignatureCustomApi - Execution completed.");
            }
            catch (Exception ex)
            {
                tracingService.Trace("Error: " + ex.ToString());
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Builds the recipients list from contacts linked to the record via M:N relationship.
        /// Only contacts with trd_iseligibleasrecipient = true are included.
        /// </summary>
        private Recipients BuildRecipients(IOrganizationService service, ITracingService tracingService, Guid recordId, bool isInPerson, Guid userId)
        {
            var recipients = new Recipients
            {
                Signers = new List<Signer>(),
                InPersonSigners = new List<InPersonSigner>()
            };

            // Get host info (current user) for InPerson signing
            string hostEmail = string.Empty;
            string hostFullName = string.Empty;
            if (isInPerson)
            {
                var userEntity = service.Retrieve("systemuser", userId, new ColumnSet("internalemailaddress", "fullname"));
                hostEmail = userEntity.GetAttributeValue<string>("internalemailaddress");
                hostFullName = userEntity.GetAttributeValue<string>("fullname");
                tracingService.Trace($"Host (InPerson): {hostFullName} / {hostEmail}");
            }

            // Query contacts via M:N relationship where trd_iseligibleasrecipient = true
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet(
                    "contactid", "fullname", "emailaddress1", "mobilephone", "trd_dialincode"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("trd_iseligibleasrecipient", ConditionOperator.Equal, true);

            var relationship = query.AddLink(
                "trd_trdcontract_contact",
                "contactid",
                "contactid");
            relationship.LinkCriteria.AddCondition("trd_trdcontractid", ConditionOperator.Equal, recordId);

            var contacts = service.RetrieveMultiple(query);
            tracingService.Trace($"Eligible contacts found: {contacts.Entities.Count}");

            // Check if SMS authentication is required.  OBS: It need license to for the sms auth to be enabled
            string shouldAddPhoneNumber = Helpers.EnvironmentVariableHelper.GetValue(service, Helpers.EnvironmentVariableNames.DocuSignShouldAddPhoneNumber);
            bool smsEnabled = shouldAddPhoneNumber == "yes";

            for (int pos = 0; pos < contacts.Entities.Count; pos++)
            {
                var contact = contacts.Entities[pos];

                string contactEmail = contact.GetAttributeValue<string>("emailaddress1");
                string contactFullName = contact.GetAttributeValue<string>("fullname");
                string mobilePhone = contact.GetAttributeValue<string>("mobilephone");
                string dialInCode = contact.FormattedValues.Contains("trd_dialincode")
                    ? contact.FormattedValues["trd_dialincode"]
                    : string.Empty;

                if (string.IsNullOrEmpty(contactEmail) || string.IsNullOrEmpty(contactFullName))
                {
                    tracingService.Trace($"Skipping contact at position {pos} - missing email or name.");
                    continue;
                }

                string smsConfigName = "SMS Auth $";
                int recipientId = pos + 1;
                string recipientIdStr = recipientId.ToString();

                // Create SMS authentication if mobile phone is available
                SmsAuthentication smsAuth = null;
                if (smsEnabled && !string.IsNullOrEmpty(mobilePhone) && !string.IsNullOrEmpty(dialInCode))
                {
                    string formattedDialCode = dialInCode.StartsWith("+") ? dialInCode : "+" + dialInCode;
                    smsAuth = new SmsAuthentication
                    {
                        SenderProvidedNumbers = new List<string> { formattedDialCode + " " + mobilePhone },
                        SenderProvidedNumber = new SenderProvidedNumber
                        {
                            Code = dialInCode.Replace("+", ""),
                            Number = mobilePhone
                        }
                    };
                    tracingService.Trace($"SMS auth set for {contactFullName}");
                }

                // Build email notification if subject/body are set
                EmailNotification emailNotification = null;
                if (!string.IsNullOrEmpty(_emailBody))
                {
                    emailNotification = new EmailNotification
                    {
                        emailSubject = _emailSubject,
                        emailBody = _emailBody
                    };
                }

                // Parallel routing: all recipients receive the email at the same time
                var defaultRoutingOrder = "1";

                if (isInPerson)
                {
                    var inPersonSigner = new InPersonSigner
                    {
                        HostEmail = hostEmail,
                        HostName = hostFullName,
                        SignerEmail = contactEmail,
                        SignerName = contactFullName,
                        RecipientId = recipientIdStr,
                        RoutingOrder = defaultRoutingOrder
                    };

                    if (smsAuth != null)
                    {
                        inPersonSigner.IdCheckConfigurationName = smsConfigName;
                        inPersonSigner.SmsAuthentication = smsAuth;
                    }
                    if (emailNotification != null)
                        inPersonSigner.EmailNotification = emailNotification;

                    recipients.InPersonSigners.Add(inPersonSigner);
                }
                else
                {
                    var signer = new Signer
                    {
                        Email = contactEmail,
                        Name = contactFullName,
                        RecipientId = recipientIdStr,
                        RoutingOrder = defaultRoutingOrder
                    };

                    if (smsAuth != null)
                    {
                        signer.IdCheckConfigurationName = smsConfigName;
                        signer.SmsAuthentication = smsAuth;
                    }
                    if (emailNotification != null)
                        signer.EmailNotification = emailNotification;

                    recipients.Signers.Add(signer);
                }
            }

            return recipients;
        }

        /// <summary>
        /// Retrieves documents from trd_document where trd_docusignrequiredcode = true
        /// and trd_regardingid = recordId. Convert file content as base64
        /// </summary>
        private List<Document> RetrieveDocuments(IOrganizationService service, ITracingService tracingService, string recordId)
        {
            var documentList = new List<Document>();

            var query = new QueryExpression("trd_document")
            {
                ColumnSet = new ColumnSet("trd_documentid", "trd_file", "trd_name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("trd_docusignrequiredcode", ConditionOperator.Equal, true),
                        new ConditionExpression("trd_regardingid", ConditionOperator.Equal, new Guid(recordId))
                    }
                }
            };

            EntityCollection results = service.RetrieveMultiple(query);
            tracingService.Trace($"Documents found: {results.Entities.Count}");

            if (results != null && results.Entities.Count > 0)
            {
                int idDoc = 0;
                foreach (var doc in results.Entities)
                {
                    string docName = doc.GetAttributeValue<string>("trd_name");

                    var documentContent = Convert.ToBase64String(
                        DownloadFileColumn(service, new EntityReference("trd_document", doc.Id), "trd_file"));

                    documentList.Add(new Document
                    {
                        DocumentBase64 = documentContent,
                        Name = docName,
                        FileExtension = GetFileExtension(docName),
                        DocumentId = (++idDoc).ToString(),
                        D365CoumentId = doc.Id
                    });

                    tracingService.Trace($"Document added: {docName}");
                }
            }

            return documentList;
        }

        /// <summary>
        /// Downloads a file column from Dataverse using chunked download.
        /// </summary>
        private static byte[] DownloadFileColumn(IOrganizationService service, EntityReference entityReference, string fileAttributeName)
        {
            var initRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = entityReference,
                FileAttributeName = fileAttributeName
            };

            var initResponse = (InitializeFileBlocksDownloadResponse)service.Execute(initRequest);

            string fileContinuationToken = initResponse.FileContinuationToken;
            long fileSizeInBytes = initResponse.FileSizeInBytes;

            List<byte> fileBytes = new List<byte>((int)fileSizeInBytes);

            long offset = 0;
            long blockSize = !initResponse.IsChunkingSupported ? fileSizeInBytes : 4 * 1024 * 1024;

            if (fileSizeInBytes < blockSize)
                blockSize = fileSizeInBytes;

            while (fileSizeInBytes > 0)
            {
                var downloadRequest = new DownloadBlockRequest
                {
                    BlockLength = blockSize,
                    FileContinuationToken = fileContinuationToken,
                    Offset = offset
                };

                var downloadResponse = (DownloadBlockResponse)service.Execute(downloadRequest);
                fileBytes.AddRange(downloadResponse.Data);

                fileSizeInBytes -= blockSize;
                offset += blockSize;
            }

            return fileBytes.ToArray();
        }

        /// <summary>
        /// Gets the file extension from a filename.
        /// </summary>
        private string GetFileExtension(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return string.Empty;

            return new FileInfo(filename).Extension;
        }

        /// <summary>
        /// Creates an envelope signature tracking record in D365.
        /// </summary>
        private Guid CreateEnvelopeSignatureRecord(IOrganizationService service, ITracingService tracingService, string envelopeId, string templateId, Guid recordId, string entityLogicalName, Guid userId)
        {
            tracingService.Trace("Starting CreateEnvelopeSignatureRecord");

            // Get current user name
            var currentUser = service.Retrieve("systemuser", userId, new ColumnSet("fullname"));
            string userName = currentUser.GetAttributeValue<string>("fullname");

            Entity envelopeRecord = new Entity("trd_docusignenvelope");
            envelopeRecord["trd_name"] = $"Envelope tracking - {userName} - {DateTime.UtcNow:g}";
            envelopeRecord["trd_envelopetemplateid"] = templateId;
            envelopeRecord["trd_envelopeid"] = envelopeId;
            envelopeRecord["statuscode"] = new OptionSetValue(933340002); // Draft
            envelopeRecord["trd_regardingobjectid"] = new EntityReference(entityLogicalName, recordId);
            envelopeRecord["trd_sender"] = new EntityReference("systemuser", userId);

            Guid newRecordId = service.Create(envelopeRecord);
            tracingService.Trace($"Envelope record created with ID: {newRecordId}");

            return newRecordId;
        }

        /// <summary>
        /// Creates document records linked to the envelope.
        /// </summary>
        private void CreateEnvelopeDocumentsFromRecipients(IOrganizationService service, ITracingService tracingService, List<Document> documents, Guid d365EnvelopeId)
        {
            foreach (var document in documents)
            {
                Entity docRecord = new Entity("trd_docusignenvelopedocuments");
                docRecord["trd_name"] = document.Name;
                docRecord["trd_docusignenvelopeid"] = new EntityReference("trd_docusignenvelope", d365EnvelopeId);
                docRecord["trd_documentid"] = new EntityReference("trd_document", document.D365CoumentId);
                docRecord["trd_ds_documentid"] = document.DocumentId;

                service.Create(docRecord);
                tracingService.Trace($"Envelope document record created: {document.Name}");
            }
        }
    }
}
