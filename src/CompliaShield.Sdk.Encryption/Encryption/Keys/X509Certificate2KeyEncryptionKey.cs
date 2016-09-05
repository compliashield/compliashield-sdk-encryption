
namespace CompliaShield.Sdk.Cryptography.Encryption.Keys
{
    using Extensions;
    using Hashing;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Utilities;

    public sealed class X509Certificate2KeyEncryptionKey : IKeyEncyrptionKey
    {

        private X509Certificate2 _x5092;

        private bool _isDisposed;

        public bool IsDisposed { get { return _isDisposed; } }

        public string Actor { get; set; }

        public string KeyId { get; private set; }

        public DateTime NotBefore
        {
            get
            {
                if (_x5092 == null)
                {
                    // max value means it should not be used
                    return DateTime.MaxValue;
                }
                return _x5092.NotBefore;
            }
        }

        public DateTime NotAfter
        {
            get
            {
                if (_x5092 == null)
                {
                    // min value means it should not be used
                    return DateTime.MinValue;
                }
                return _x5092.NotAfter;
            }
        }

        public PublicKey PublicKey
        {
            get
            {
                this.EnsureNotDisposed();
                return _x5092.PublicKey;
            }
        }

        #region .ctors

        public X509Certificate2KeyEncryptionKey(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                throw new ArgumentException("x509Certificate2");
            }
            if (x509Certificate2.Thumbprint != null)
            {
                this.KeyId = x509Certificate2.Thumbprint.ToLower();
            }
            if (!x509Certificate2.HasPrivateKey)
            {
                throw new ArgumentException(string.Format("x509Certificate2 with thumbprint '{0}' does not have a private key.", x509Certificate2.Thumbprint));
            }
            var badPrivateKeyEx = new ArgumentException(string.Format("x509Certificate2 with thumbprint '{0}' does not have a private key.", x509Certificate2.Thumbprint));

            try
            {
                if (x509Certificate2.PrivateKey == null)
                {
                    throw badPrivateKeyEx;
                }
            }
            catch (Exception ex)
            {
                if (ex == badPrivateKeyEx)
                {
                    throw;
                }
                throw new ArgumentException(badPrivateKeyEx.Message, ex);
            }
            _x5092 = x509Certificate2;
        }

        #endregion

        public async Task<Tuple<byte[], string>> SignAsync(byte[] digest, string algorithm)
        {
            return await this.SignAsync(digest, algorithm, new CancellationToken());
        }

        /// <summary>
        /// Signs a byte array by creating an appropriate hash.
        /// </summary>
        /// <param name="digest">Any data to sign.</param>
        /// <param name="algorithm">MD5 or SHA1</param>
        /// <param name="token"></param>
        /// <returns>The signature for the digest as a byte array and string based hash code.</returns>
        public async Task<Tuple<byte[], string>> SignAsync(byte[] digest, string algorithm, CancellationToken token)
        {
            this.EnsureNotDisposed();

            if (digest == null || !digest.Any())
            {
                throw new ArgumentException("digest");
            }
            if (algorithm == null)
            {
                throw new ArgumentNullException("algorithm");
            }

            var crypto = this.GetRSACryptoServiceProvider();
            var signer = new Signing.Signer(crypto);

            algorithm = algorithm.ToLower();
            switch (algorithm)
            {
                case "md5":
                    var hashMd5 = BasicHasher.GetMd5HashBytes(digest);
                    return await Task.FromResult(signer.SignMd5(hashMd5));
                case "sha1":
                    var hashSha1 = BasicHasher.GetSha1HashBytes(digest);
                    return await Task.FromResult(signer.SignSha1(hashSha1));
                default:
                    throw new NotImplementedException(string.Format("algorithm '{0}' is not implemented.", algorithm));
            }
        }

        public async Task<Tuple<byte[], string>> SignAsync(string hex)
        {
            this.EnsureNotDisposed();

            if (hex == null || !(hex.Length == 32 | hex.Length == 40))
            {
                throw new ArgumentException("hex must be a valid MD5 or SHA1 hash in HEX format");
            }
            string algorithm = "md5";
            if (hex.Length == 40)
            {
                algorithm = "sha1";
            }

            var crypto = this.GetRSACryptoServiceProvider();
            var signer = new Signing.Signer(crypto);

            algorithm = algorithm.ToLower();
            var hashBytes = hex.HexStringToByteArray();

            switch (algorithm)
            {
                case "md5":
                    return await Task.FromResult(signer.SignMd5(hashBytes));
                case "sha1":
                    return await Task.FromResult(signer.SignSha1(hashBytes));
                default:
                    throw new NotImplementedException(string.Format("algorithm '{0}' is not implemented.", algorithm));
            }
        }

        public async Task<byte[]> UnwrapKeyAsync(byte[] encryptedKey)
        {
            return await this.UnwrapKeyAsync(encryptedKey, new CancellationToken());
        }

        public async Task<byte[]> UnwrapKeyAsync(byte[] encryptedKey, CancellationToken token)
        {
            this.EnsureNotDisposed();

            var alg = this.GetRSACryptoServiceProvider();
            byte[] keyOut;
            try
            {
                keyOut = await Task.FromResult(alg.Decrypt(encryptedKey, false));
            }
            catch (Exception ex)
            {
                throw new CryptographicException(string.Format("X509Certificate2 with thumbprint '{0}' throw an exception on Decrypt. See inner exception for details", _x5092.Thumbprint), ex);
            }
            return keyOut;
        }

        public async Task<bool> VerifyAsync(byte[] digest, byte[] signature, string algorithm)
        {
            return await this.VerifyAsync(digest, signature, algorithm, new CancellationToken());
        }

        /// <summary>
        /// Verifies the byte array against a signature.
        /// </summary>
        /// <param name="digest"></param>
        /// <param name="signature"></param>
        /// <param name="algorithm"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> VerifyAsync(byte[] digest, byte[] signature, string algorithm, CancellationToken token)
        {
            this.EnsureNotDisposed();
            if (digest == null || !digest.Any())
            {
                throw new ArgumentException("digest");
            }
            if (algorithm == null)
            {
                throw new ArgumentNullException("algorithm");
            }

            var verifier = new Signing.Verifier(this.GetPublicRSACryptoServiceProvider());

            algorithm = algorithm.ToLower();
            switch (algorithm)
            {
                case "md5":
                    var hashMd5 = BasicHasher.GetMd5HashBytes(digest);
                    return await Task.FromResult(verifier.VerifyMd5Hash(hashMd5, signature));
                case "sha1":
                    var hashSha1 = BasicHasher.GetSha1HashBytes(digest);
                    return await Task.FromResult(verifier.VerifySha1Hash(hashSha1, signature));
                default:
                    throw new NotImplementedException(string.Format("algorithm '{0}' is not implemented.", algorithm));
            }
        }

        public async Task<bool> VerifyAsync(byte[] digest, string signature, string algorithm)
        {
            return await this.VerifyAsync(digest, signature, algorithm, new CancellationToken());
        }

        public async Task<bool> VerifyAsync(byte[] digest, string signature, string algorithm, CancellationToken token)
        {
            this.EnsureNotDisposed();
            if (digest == null || !digest.Any())
            {
                throw new ArgumentException("digest");
            }
            if (algorithm == null)
            {
                throw new ArgumentNullException("algorithm");
            }

            var verifier = new Signing.Verifier(this.GetPublicRSACryptoServiceProvider());

            algorithm = algorithm.ToLower();
            switch (algorithm)
            {
                case "md5":
                    var hashMd5 = BasicHasher.GetMd5Hash(digest);
                    return await Task.FromResult(verifier.VerifyMd5Hash(hashMd5, signature));
                case "sha1":
                    var hashSha1 = BasicHasher.GetSha1Hash(digest);
                    return await Task.FromResult(verifier.VerifySha1Hash(hashSha1, signature));
                default:
                    throw new NotImplementedException(string.Format("algorithm '{0}' is not implemented.", algorithm));
            }
        }

        public async Task<bool> VerifyAsync(string hex, string signature)
        {
            return await this.VerifyAsync(hex, signature, new CancellationToken());
        }

        public async Task<bool> VerifyAsync(string hex, string signature, CancellationToken token)
        {
            this.EnsureNotDisposed();
            if (hex == null || !(hex.Length == 32 | hex.Length == 40))
            {
                throw new ArgumentException("hex must be a valid MD5 or SHA1 hash in HEX format");
            }
            string algorithm = "md5";
            if (hex.Length == 40)
            {
                algorithm = "sha1";
            }

            var verifier = new Signing.Verifier(this.GetPublicRSACryptoServiceProvider());

            algorithm = algorithm.ToLower();
            switch (algorithm)
            {
                case "md5":
                    return await Task.FromResult(verifier.VerifyMd5Hash(hex, signature));
                case "sha1":
                    return await Task.FromResult(verifier.VerifySha1Hash(hex, signature));
                default:
                    throw new NotImplementedException(string.Format("algorithm '{0}' is not implemented.", algorithm));
            }
        }
        
        public async Task<byte[]> WrapKeyAsync(byte[] key)
        {
            return await this.WrapKeyAsync(key,  CancellationToken.None);
        }

        public async Task<byte[]> WrapKeyAsync(byte[] key, CancellationToken token)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            //if (algorithm == null)
            //{
            //    throw new ArgumentNullException(nameof(algorithm));
            //}

            this.EnsureNotDisposed();
            if (this.PublicKey == null)
            {
                throw new InvalidOperationException("There is no PublicKey");
            }

            var fOAEP = false;
            var rsa = X509CertificateHelper.GetRSACryptoServiceProviderFromPublicKey(_x5092);
            var encrypted = await Task.FromResult(rsa.Encrypt(key, fOAEP));
            return encrypted;
            //var encryptedAsString = Encoding.UTF8.GetString(encrypted);
            //return new Tuple<byte[], string>(encrypted, encryptedAsString);
        }

        public string PublicKeyToPEM()
        {
            this.EnsureNotDisposed();
            return X509CertificateHelper.ExportToPEM(_x5092);
        }

        public void Dispose()
        {
            _isDisposed = true;
        }

        #region helpers

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private RSACryptoServiceProvider GetPublicRSACryptoServiceProvider()
        {
            RSACryptoServiceProvider alg = null;
            if (_x5092.PublicKey != null)
            {
                alg = _x5092.PublicKey.Key as RSACryptoServiceProvider;
            }            
            if (alg == null)
            {
                throw new NotImplementedException(string.Format("X509Certificate2 with thumbprint '{0}' PublicKey.Key is not a valid RSACryptoServiceProvider.", _x5092.Thumbprint));
            }
            return alg;
        }


        private RSACryptoServiceProvider GetRSACryptoServiceProvider()
        {
            var alg = _x5092.PrivateKey as RSACryptoServiceProvider;
            if (alg == null)
            {
                throw new NotImplementedException(string.Format("X509Certificate2 with thumbprint '{0}' PrivateKey is not a valid RSACryptoServiceProvider. PrivateKey is of type '{1}'.", _x5092.Thumbprint, _x5092.PrivateKey.GetType().FullName));
            }
            return alg;
        }

        #endregion
    }
}
