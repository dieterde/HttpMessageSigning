using System;
using System.Threading.Tasks;
using Dalion.HttpMessageSigning.SigningString;
using Microsoft.Extensions.Logging;

namespace Dalion.HttpMessageSigning.Verification.VerificationTasks {
    internal class MatchingSignatureStringVerificationTask : IVerificationTask {
        private readonly ISigningStringComposer _signingStringComposer;
        private readonly IBase64Converter _base64Converter;
        private readonly ILogger<MatchingSignatureStringVerificationTask> _logger;

        public MatchingSignatureStringVerificationTask(
            ISigningStringComposer signingStringComposer, 
            IBase64Converter base64Converter,
            ILogger<MatchingSignatureStringVerificationTask> logger = null) {
            _signingStringComposer = signingStringComposer ?? throw new ArgumentNullException(nameof(signingStringComposer));
            _base64Converter = base64Converter ?? throw new ArgumentNullException(nameof(base64Converter));
            _logger = logger;
        }

        public Task<Exception> Verify(HttpRequestForSigning signedRequest, Signature signature, Client client) {
            if (!signature.Created.HasValue) {
                return new SignatureVerificationException($"The signature does not contain a value for the {nameof(signature.Created)} property, but it is required.")
                    .ToTask<Exception>();
            }           
            
            if (!signature.Expires.HasValue) {
                return new SignatureVerificationException($"The signature does not contain a value for the {nameof(signature.Expires)} property, but it is required.")
                    .ToTask<Exception>();
            }

            var expires = signature.Expires.Value - signature.Created.Value;
            var signingString = _signingStringComposer.Compose(signedRequest, signature.Headers, signature.Created.Value, expires, signature.Nonce);
            
            _logger?.LogDebug("Composed the following signing string for request verification: {0}", signingString);

            var receivedSignature = _base64Converter.FromBase64(signature.String);
            var isValidSignature = client.SignatureAlgorithm.VerifySignature(signingString, receivedSignature);

            _logger?.LogDebug("The verification of the signature {0}.", isValidSignature ? "succeeded" : "failed");
            
            if (!isValidSignature) {
                return new SignatureVerificationException("The signature string does not match the expected value.")
                    .ToTask<Exception>();
            }
            
            return Task.FromResult<Exception>(null);
        }
    }
}