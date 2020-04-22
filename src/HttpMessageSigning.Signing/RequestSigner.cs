using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dalion.HttpMessageSigning.Signing {
    internal class RequestSigner : IRequestSigner {
        private readonly ISigningSettingsSanitizer _signingSettingsSanitizer;
        private readonly ISignatureCreator _signatureCreator;
        private readonly IAuthorizationHeaderParamCreator _authorizationHeaderParamCreator;
        private readonly SigningSettings _signingSettings;
        private readonly ISignatureHeaderEnsurer _signatureHeaderEnsurer;
        private readonly ISystemClock _systemClock;
        private readonly ILogger<RequestSigner> _logger;

        public RequestSigner(
            ISigningSettingsSanitizer signingSettingsSanitizer,
            ISignatureCreator signatureCreator,
            IAuthorizationHeaderParamCreator authorizationHeaderParamCreator,
            SigningSettings signingSettings,
            ISignatureHeaderEnsurer signatureHeaderEnsurer, 
            ISystemClock systemClock,
            ILogger<RequestSigner> logger = null) {
            _signingSettingsSanitizer = signingSettingsSanitizer ?? throw new ArgumentNullException(nameof(signingSettingsSanitizer));
            _signatureCreator = signatureCreator ?? throw new ArgumentNullException(nameof(signatureCreator));
            _authorizationHeaderParamCreator = authorizationHeaderParamCreator ?? throw new ArgumentNullException(nameof(authorizationHeaderParamCreator));
            _signingSettings = signingSettings ?? throw new ArgumentNullException(nameof(signingSettings));
            _signatureHeaderEnsurer = signatureHeaderEnsurer ?? throw new ArgumentNullException(nameof(signatureHeaderEnsurer));
            _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            _logger = logger;
        }

        public async Task Sign(HttpRequestMessage request) {
            try {
                if (request == null) throw new ArgumentNullException(nameof(request));

                var clonedSettings = (SigningSettings)_signingSettings.Clone();
                var onRequestSigningTask = _signingSettings.Events?.OnRequestSigning?.Invoke(request, clonedSettings);
                if (onRequestSigningTask != null) await onRequestSigningTask;
                
                clonedSettings.Validate();

                _signingSettingsSanitizer.SanitizeHeaderNamesToInclude(clonedSettings, request);
                
                var timeOfSigning = _systemClock.UtcNow;
                await _signatureHeaderEnsurer.EnsureHeader(request, clonedSettings, timeOfSigning);
                
                var signature = await _signatureCreator.CreateSignature(request, clonedSettings, timeOfSigning);
                var authParam = _authorizationHeaderParamCreator.CreateParam(signature);

                _logger?.LogDebug("Setting Authorization scheme to '{0}' and param to '{1}'.", clonedSettings.AuthorizationScheme, authParam);

                request.Headers.Authorization = new AuthenticationHeaderValue(clonedSettings.AuthorizationScheme, authParam);
                
                var onRequestSignedTask = _signingSettings.Events?.OnRequestSigned?.Invoke(request, signature, clonedSettings);
                if (onRequestSignedTask != null) await onRequestSignedTask;
            }
            catch (Exception ex) {
                _logger?.LogError(ex, "Could not sign the specified request. See inner exception.");
                throw;
            }
        }

        public void Dispose() {
            _signingSettings?.Dispose();
        }
    }
}