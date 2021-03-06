using System.Threading.Tasks;

namespace Dalion.HttpMessageSigning.Verification {
    public interface ISignatureVerifier {
        Task<SignatureVerificationFailure> VerifySignature(HttpRequestForSigning signedRequest, Signature signature, Client client);
    }
}