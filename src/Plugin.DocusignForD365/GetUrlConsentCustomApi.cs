// Author: Adilson Capaia
// Blog: The Rational Developer
// Year: 2026

using Microsoft.Xrm.Sdk;
using Plugin.DocusignForD365.Docusign.Services;
using System;

namespace Plugin.DocusignForD365
{
    /// <summary>
    /// Custom API plugin that retrieves the DocuSign consent URL for the current user.
    /// If the user has already granted consent, Success = true and ConsentUrl may be used for reference.
    /// If consent is required, Success = false and ConsentUrl contains the URL to redirect the user to.
    /// </summary>
    public class GetUrlConsentCustomApi : IPlugin
    {
        // Output parameter names (match the Custom API definition in D365)
        private const string Param_Success = "Success";
        private const string Param_ConsentUrl = "ConsentUrl";

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                tracingService.Trace("GetUrlConsentCustomApi - Execution started.");

                var authService = new DocuSignAuthService(tracingService, service, context);
                var apiService = new DocusignApiService(tracingService, service, authService);

                var consentResponse = apiService.GetIndividualUrlConsent();
                tracingService.Trace($"Consent response: Success={consentResponse.Success}, Url={consentResponse.ConsentUrl}");

                context.OutputParameters[Param_Success] = consentResponse.Success;
                context.OutputParameters[Param_ConsentUrl] = consentResponse.ConsentUrl;

                tracingService.Trace("GetUrlConsentCustomApi - Execution completed.");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }
    }
}
