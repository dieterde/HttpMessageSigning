using System;
using System.Security.Cryptography;
using System.Text;

namespace Dalion.HttpMessageSigning {
    public class RSASignatureAlgorithm : ISignatureAlgorithm {
        private readonly RSA _rsa;

        public RSASignatureAlgorithm(HashAlgorithmName hashAlgorithm, RSA rsa) {
            HashAlgorithm = hashAlgorithm;
            _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));
        }

        public HashAlgorithmName HashAlgorithm { get; }

        public void Dispose() {
            _rsa?.Dispose();
        }

        public string Name => "RSA";

        public byte[] ComputeHash(string contentToSign) {
            var inputBytes = Encoding.UTF8.GetBytes(contentToSign);
            using (var hasher = HashAlgorithmFactory.Create(HashAlgorithm)) {
                var hashedData = hasher.ComputeHash(inputBytes);
                return _rsa.SignHash(hashedData, HashAlgorithm, RSASignaturePadding.Pkcs1);
            }
        }

        public bool VerifySignature(string contentToSign, byte[] signature) {
            if (contentToSign == null) throw new ArgumentNullException(nameof(contentToSign));
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var signedBytes = Encoding.UTF8.GetBytes(contentToSign);
            using (var hasher = HashAlgorithmFactory.Create(HashAlgorithm)) {
                var hashedData = hasher.ComputeHash(signedBytes);
                return _rsa.VerifyHash(hashedData, signature, HashAlgorithm, RSASignaturePadding.Pkcs1);
            }
        }

        public static RSASignatureAlgorithm CreateForSigning(HashAlgorithmName hashAlgorithmName, RSAParameters privateParameters) {
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(privateParameters);
            return new RSASignatureAlgorithm(hashAlgorithmName, rsa);
        }

        public static RSASignatureAlgorithm CreateForVerification(HashAlgorithmName hashAlgorithmName, RSAParameters publicParameters) {
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(publicParameters);
            return new RSASignatureAlgorithm(hashAlgorithmName, rsa);
        }

        public RSAParameters GetPublicKey() {
            return _rsa.ExportParameters(false);
        }
    }
}