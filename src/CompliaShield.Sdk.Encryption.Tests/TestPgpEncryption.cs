﻿
namespace CompliaShield.Sdk.Cryptography.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Org.BouncyCastle.Bcpg.OpenPgp;
    using Encryption;

    [TestClass]
    public class TestPgpEncryption
    {
        [TestMethod]
        public void TestBytesAndStreams()
        {
            var fi = new FileInfo(@"testfiles\dataFeb-8-2016_40M-pipe.csv");
            var fiOut = new FileInfo(fi.FullName + ".gpg");
            var fiDecrypted = new FileInfo(@"testfiles\dataFeb-8-2016_40M-pipe-decrypted.csv");

            var fiPublicKey = new FileInfo(@"testfiles\dummy2.asc");
            var fiPrivateKey = new FileInfo(@"testfiles\dummyprivate2.asc");
            var privateKeyPassPhrase = "hello world!";

            // encrypt
            using (Stream publicKeyStream = File.OpenRead(fiPublicKey.FullName))
            {
                var bytes = File.ReadAllBytes(fi.FullName);
                var output = PgpEncryptor.EncryptAes256(bytes, fi.Name, publicKeyStream, withIntegrityCheck: true, armor: false, compress: true);
                File.WriteAllBytes(fiOut.FullName, output.ToArray());
            }

            // decrypt
            using (Stream privateKeyStream = File.OpenRead(fiPrivateKey.FullName))
            {
                using (Stream encryptedDataStream = File.OpenRead(fiOut.FullName))
                {
                    var output = PgpEncryptor.DecryptPgpData(encryptedDataStream, privateKeyStream, privateKeyPassPhrase);

                    using (var fileStream = File.Create(fiDecrypted.FullName))
                    {
                        output.CopyTo(fileStream);
                    }
                }
            }
        }

        [TestMethod]
        public void TestFiles()
        {
            var fi = new FileInfo(@"testfiles\dataFeb-8-2016_40M-pipe.csv");
            var fiOut = new FileInfo(fi.FullName + ".gpg");
            var fiDecrypted = new FileInfo(@"testfiles\dataFeb-8-2016_40M-pipe-decrypted.csv");

            var fiPublicKey = new FileInfo(@"testfiles\dummy2.asc");
            var fiPrivateKey = new FileInfo(@"testfiles\dummyprivate2.asc");
            var privateKeyPassPhrase = "hello world!";

            // encrypt
            using (Stream publicKeyStream = File.OpenRead(fiPublicKey.FullName))
            {
                PgpEncryptor.EncryptAes256(fi.FullName, fiOut.FullName, publicKeyStream, withIntegrityCheck: true, armor: false, compress: true, overwrite: true);
            }

            using (Stream privateKeyStream = File.OpenRead(fiPrivateKey.FullName))
            {
                PgpEncryptor.DecryptPgpData(fiOut.FullName, fiDecrypted.FullName, privateKeyStream, privateKeyPassPhrase, true);
            }
       
        }
    }
}