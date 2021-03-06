﻿
namespace CompliaShield.Sdk.Cryptography.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Management;
    using System.Reflection;
    using System.Security.Cryptography;
    using Utilities;
    using Encryption;
    using Encryption.SerializationHelpers;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Security;
    using Encryption.Keys;
    using System.Security;
    using Extensions;
    using System.Diagnostics;

    [TestClass]
    public class TestEncryption : _baseTest
    {
       
        [TestMethod]
        public void TestAsymmetricallyEncryptedBackupObject()
        {
            var cert2 = LoadCertificate();

            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

            var encryptor = new AsymmetricEncryptor() { AsymmetricStrategy = AsymmetricStrategyOption.Aes256_20000 };

            var asymEncObj = encryptor.EncryptObjectAsync(stringToEncrypt, publicKey).GetAwaiter().GetResult();
            asymEncObj.PublicMetadata = new Dictionary<string, string>();
            asymEncObj.PublicMetadata["keyA"] = "valueA";
            asymEncObj.PublicMetadata["keyB"] = "valueB";


            var asymEncObj2 = encryptor.EncryptObjectAsync(stringToEncrypt, publicKey).GetAwaiter().GetResult();
            asymEncObj.PublicMetadata = new Dictionary<string, string>();
            asymEncObj.PublicMetadata["keyA-2"] = "valueA-2";
            asymEncObj.PublicMetadata["keyB-2"] = "valueB-2";

            var backup = new AsymmetricallyEncryptedBackupObject()
            {
                AssociationObjectIdentifier = Guid.NewGuid().ToString(),
                AssociationObjectType = "test",
                BackupObjects = new Dictionary<string, AsymmetricallyEncryptedObject>()
            };

            backup.BackupObjects["objA"] = asymEncObj;
            backup.BackupObjects["objB"] = asymEncObj2;

            var asymBackup = encryptor.EncryptObjectAsync(backup, publicKey).GetAwaiter().GetResult();
            var decrypted = encryptor.DecryptObject(asymBackup, privateKey);

            Assert.IsTrue(decrypted is AsymmetricallyEncryptedBackupObject);

            var asBytes = asymBackup.ToByteArray();
            var newAsymmObj = new AsymmetricallyEncryptedObject();
            newAsymmObj.LoadFromByteArray(asBytes);
            var decrypted2 = encryptor.DecryptObject(newAsymmObj, privateKey);
            Assert.IsTrue(decrypted2 is AsymmetricallyEncryptedBackupObject);

            // let's decrypt the embedded types

            var backupObjFromBytes = decrypted2 as AsymmetricallyEncryptedBackupObject;
            Assert.AreEqual(backupObjFromBytes.AssociationObjectIdentifier, backup.AssociationObjectIdentifier);
            Assert.AreEqual(backupObjFromBytes.AssociationObjectType, backup.AssociationObjectType);

            var objA = backupObjFromBytes.BackupObjects["objA"];
            var decryptedObjA = encryptor.DecryptObject(objA, privateKey);
            Assert.AreEqual(stringToEncrypt, decryptedObjA);

            var objB = backupObjFromBytes.BackupObjects["objB"];
            var decryptedObjB = encryptor.DecryptObject(objB, privateKey);
            Assert.AreEqual(stringToEncrypt, decryptedObjB);

        }


        [TestMethod]
        public void TestProtectPassword()
        {
            var cert2 = LoadCertificate();
            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            int length = 100;
            var rand = new RandomGenerator();
            for (int i = 0; i < length; i++)
            {
                using (var password = rand.RandomSecureStringPassword(10, 50))
                {
                    var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

                    // base 64
                    var encryptedBase64 = AesEncryptor.Encrypt1000(stringToEncrypt, password);
                    var decryptedBase64 = AesEncryptor.Decrypt(encryptedBase64, password);
                    Assert.AreEqual(stringToEncrypt, decryptedBase64);

                    // base 36
                    var encryptedBase36 = AesEncryptor.Encrypt1000(stringToEncrypt, password, true);
                    var decryptedBase36 = AesEncryptor.Decrypt(encryptedBase36, password, true);
                    Assert.AreEqual(stringToEncrypt, decryptedBase36);

                    var protectedPwStr = AsymmetricEncryptor.EncryptToBase64StringAsync(password, publicKey).GetAwaiter().GetResult();

                    var unprotectedPwdStr = AsymmetricEncryptor.DecryptFromBase64StringAsync(protectedPwStr, privateKey).GetAwaiter().GetResult();

                    var decryptedString = AesEncryptor.Decrypt(encryptedBase64, unprotectedPwdStr);
                    Assert.AreEqual(stringToEncrypt, decryptedString);
                }
            }
        }

        [TestMethod]
        public void TestProtectPasswordDualKey()
        {
            var cert2 = LoadCertificate();
            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            var cert2Dual = LoadCertificate2();
            var publicKey2 = new X509CertificatePublicKey(cert2Dual);
            var privateKey2 = new X509Certificate2KeyEncryptionKey(cert2Dual);

            int length = 100;
            var rand = new RandomGenerator();
            for (int i = 0; i < length; i++)
            {
                using (var password = rand.RandomSecureStringPassword(10, 50))
                {
                    var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

                    // base 64
                    var encryptedBase64 = AesEncryptor.Encrypt1000(stringToEncrypt, password);
                    var decryptedBase64 = AesEncryptor.Decrypt(encryptedBase64, password);
                    Assert.AreEqual(stringToEncrypt, decryptedBase64);

                    // base 36
                    var encryptedBase36 = AesEncryptor.Encrypt1000(stringToEncrypt, password, true);
                    var decryptedBase36 = AesEncryptor.Decrypt(encryptedBase36, password, true);
                    Assert.AreEqual(stringToEncrypt, decryptedBase36);

                    var protectedPwStr = AsymmetricEncryptor.EncryptToBase64StringAsync(password, publicKey, publicKey2).GetAwaiter().GetResult();

                    var unprotectedPwdStr = AsymmetricEncryptor.DecryptFromBase64String(protectedPwStr, privateKey, privateKey2);

                    var decryptedUnprotectedPw = AesEncryptor.Decrypt(encryptedBase64, unprotectedPwdStr);
                    Assert.AreEqual(stringToEncrypt, decryptedUnprotectedPw);

                }
            }
        }

        [TestMethod]
        public void TestAes20000()
        {
            int length = 100;
            var rand = new RandomGenerator();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < length; i++)
            {
                using (var password = rand.RandomSecureStringPassword(10, 50))
                {
                    var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                    // base 64
                    var encryptedBase64 = AesEncryptor.Encrypt20000(stringToEncrypt, password);
                    var decryptedBase64 = AesEncryptor.Decrypt(encryptedBase64, password);
                    Assert.AreEqual(stringToEncrypt, decryptedBase64);
                    // base 36
                    var encryptedBase36 = AesEncryptor.Encrypt20000(stringToEncrypt, password, true);
                    var decryptedBase36 = AesEncryptor.Decrypt(encryptedBase36, password, true);
                    Assert.AreEqual(stringToEncrypt, decryptedBase36);

                    Console.WriteLine(string.Format("{0:N0}\t{1}", i, stopwatch.Elapsed));
                    stopwatch.Stop();
                    stopwatch.Reset();
                    stopwatch.Start();
                }
            }
        }

        [TestMethod]
        public void TestAes20000_Bytes()
        {
            int length = 100;
            var rand = new RandomGenerator();

            //byte[] entropy = new byte[20];

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < length; i++)
            {

                byte[] newKey = new byte[rand.RandomNumber(75, 88)];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    //rng.GetBytes(entropy);
                    rng.GetBytes(newKey);
                }

                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

                var bytes = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);

                // base 64
                var encryptedBytes = AesEncryptor.Encrypt20000(bytes, newKey);
                var decryptedBytes = AesEncryptor.Decrypt(encryptedBytes, newKey);

                Assert.IsTrue(decryptedBytes.SequenceEqual(bytes));

                var decryptedString = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                Assert.AreEqual(stringToEncrypt, decryptedString);

                Console.WriteLine(string.Format("{0:N0}\t{1}", i, stopwatch.Elapsed));
                stopwatch.Stop();
                stopwatch.Reset();
                stopwatch.Start();
            }
        }



        [TestMethod]
        public void TestAes1000()
        {
            int length = 100;
            var rand = new RandomGenerator();
            for (int i = 0; i < length; i++)
            {
                using (var password = rand.RandomSecureStringPassword(10, 50))
                {
                    var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                    // base 64
                    var encryptedBase64 = AesEncryptor.Encrypt1000(stringToEncrypt, password);
                    var decryptedBase64 = AesEncryptor.Decrypt(encryptedBase64, password);
                    Assert.AreEqual(stringToEncrypt, decryptedBase64);
                    // base 36
                    var encryptedBase36 = AesEncryptor.Encrypt1000(stringToEncrypt, password, true);
                    var decryptedBase36 = AesEncryptor.Decrypt(encryptedBase36, password, true);
                    Assert.AreEqual(stringToEncrypt, decryptedBase36);
                }
            }
        }
        
        [TestMethod]
        public void TestAes1000_Bytes()
        {
            int length = 100;
            var rand = new RandomGenerator();

            //byte[] entropy = new byte[20];


            for (int i = 0; i < length; i++)
            {

                byte[] newKey = new byte[rand.RandomNumber(75, 88)];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    //rng.GetBytes(entropy);
                    rng.GetBytes(newKey);
                }

                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

                var bytes = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);
    
                var encryptedBytes = AesEncryptor.Encrypt1000(bytes, newKey);
                var decryptedBytes = AesEncryptor.Decrypt(encryptedBytes, newKey);

                Assert.IsTrue(decryptedBytes.SequenceEqual(bytes));

                var decryptedString = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                Assert.AreEqual(stringToEncrypt, decryptedString);

                var newKeyAsSecureString = newKey.ToSecureString();
                var encryptedBytes2 = AesEncryptor.Encrypt1000(bytes, newKeyAsSecureString);
                var decryptedBytes2 = AesEncryptor.Decrypt(encryptedBytes2, newKeyAsSecureString);
                Assert.IsTrue(decryptedBytes2.SequenceEqual(bytes));

            }
        }

        [TestMethod]
        public void TestAes200()
        {
            int length = 100;
            var rand = new RandomGenerator();
            for (int i = 0; i < length; i++)
            {
                using (var password = rand.RandomSecureStringPassword(10, 50))
                {
                    var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                    // base 64
                    var encryptedBase64 = AesEncryptor.Encrypt200(stringToEncrypt, password);
                    var decryptedBase64 = AesEncryptor.Decrypt(encryptedBase64, password);
                    Assert.AreEqual(stringToEncrypt, decryptedBase64);
                    // base 36
                    var encryptedBase36 = AesEncryptor.Encrypt200(stringToEncrypt, password, true);
                    var decryptedBase36 = AesEncryptor.Decrypt(encryptedBase36, password, true);
                    Assert.AreEqual(stringToEncrypt, decryptedBase36);
                }
            }
        }

        [TestMethod]
        public void TestAes200_Bytes()
        {
            int length = 100;
            var rand = new RandomGenerator();

            //byte[] entropy = new byte[20];


            for (int i = 0; i < length; i++)
            {

                byte[] newKey = new byte[rand.RandomNumber(75, 88)];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    //rng.GetBytes(entropy);
                    rng.GetBytes(newKey);
                }

                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

                var bytes = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);

                // base 64
                var encryptedBytes = AesEncryptor.Encrypt200(bytes, newKey);
                var decryptedBytes = AesEncryptor.Decrypt(encryptedBytes, newKey);

                Assert.IsTrue(decryptedBytes.SequenceEqual(bytes));

                var decryptedString = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                Assert.AreEqual(stringToEncrypt, decryptedString);


            }
        }

        [TestMethod]
        public void TestAes5_Bytes()
        {
            int length = 100;
            var rand = new RandomGenerator();

            //byte[] entropy = new byte[20];


            for (int i = 0; i < length; i++)
            {

                byte[] newKey = new byte[rand.RandomNumber(75, 88)];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    //rng.GetBytes(entropy);
                    rng.GetBytes(newKey);
                }

                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

                var bytes = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);

                // base 64
                var encryptedBytes = AesEncryptor.Encrypt5(bytes, newKey);
                var decryptedBytes = AesEncryptor.Decrypt(encryptedBytes, newKey);

                Assert.IsTrue(decryptedBytes.SequenceEqual(bytes));

                var decryptedString = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                Assert.AreEqual(stringToEncrypt, decryptedString);


            }
        }

        [TestMethod]
        public void TestAesWithCertPw()
        {

            var cert2 = LoadCertificate();

            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            int length = 100;
            var rand = new RandomGenerator();

            for (int i = 0; i < length; i++)
            {
                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                var asymEnc = new AsymmetricEncryptor(AsymmetricStrategyOption.Legacy_Aes2);
                var asymObj = asymEnc.EncryptObjectAsync(stringToEncrypt, publicKey).GetAwaiter().GetResult();
                var decrypted = asymEnc.DecryptObject(asymObj, privateKey);
                Assert.AreEqual(stringToEncrypt, decrypted);
            }
        }

        [TestMethod]
        public void TestAes1000WithCertificateAndSerialization()
        {
            var cert2 = LoadCertificate();

            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);
            
            var rand = new RandomGenerator();

            //for (int i = 0; i < length; i++)
            //{
            //    var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
            //    var asymEnc = new AsymmetricEncryptor(AsymmetricStrategyOption.Aes256_1000);
            //    var asymObj = asymEnc.EncryptObject(stringToEncrypt, cert2.Thumbprint.ToString().ToLower(), publicKey);
            //    var decrypted = asymEnc.DecryptObject(asymObj, privateKey);
            //    Assert.AreEqual(stringToEncrypt, decrypted);
            //}

            var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";

            var encryptor = new AsymmetricEncryptor() { AsymmetricStrategy = AsymmetricStrategyOption.Aes256_1000 };
            var asymEncObj = encryptor.EncryptObjectAsync(stringToEncrypt, publicKey).GetAwaiter().GetResult();

            asymEncObj.PublicMetadata = new Dictionary<string, string>();
            asymEncObj.PublicMetadata["keyA"] = "valueA";
            asymEncObj.PublicMetadata["keyB"] = "valueB";

            asymEncObj.KeyId = cert2.Thumbprint.ToLower();
            var asymEncObjDirectSerializedBytes = Serializer.SerializeToByteArray(asymEncObj);

            // deserialize

            var asymEncObj2 = Serializer.DeserializeFromByteArray(asymEncObjDirectSerializedBytes) as AsymmetricallyEncryptedObject;
            Assert.IsNotNull(asymEncObj);
            Assert.IsTrue(!string.IsNullOrEmpty(asymEncObj.KeyId));

            var asymEncObjBytes2 = asymEncObj.ToByteArray();
            var asymEncObj3 = new AsymmetricallyEncryptedObject();
            asymEncObj3.LoadFromByteArray(asymEncObjBytes2);

            var decrypted = encryptor.DecryptObject(asymEncObj3, privateKey) as string;
            Assert.AreEqual(decrypted, stringToEncrypt);

            // test deserializing with direct
            var asymEncObj4 = new AsymmetricallyEncryptedObject();
            asymEncObj4.LoadFromByteArray(asymEncObjDirectSerializedBytes);

            var decrypted2 = encryptor.DecryptObject(asymEncObj4, privateKey) as string;
            Assert.AreEqual(decrypted2, stringToEncrypt);


        }

        [TestMethod]
        public void TestAes1000WithCertificate()
        {
            var cert2 = LoadCertificate();

            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            int length = 100;
            var rand = new RandomGenerator();

            for (int i = 0; i < length; i++)
            {
                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                var asymEnc = new AsymmetricEncryptor(AsymmetricStrategyOption.Aes256_1000);
                var asymObj = asymEnc.EncryptObjectAsync(stringToEncrypt, publicKey).GetAwaiter().GetResult();
                var decrypted = asymEnc.DecryptObject(asymObj, privateKey);
                Assert.AreEqual(stringToEncrypt, decrypted);
            }
        }

        [TestMethod]
        public void TestAes5WithCertificate()
        {
            var cert2 = LoadCertificate();

            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            int length = 100;
            var rand = new RandomGenerator();

            for (int i = 0; i < length; i++)
            {
                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                var asymEnc = new AsymmetricEncryptor(AsymmetricStrategyOption.Aes256_5);
                var asymObj = asymEnc.EncryptObjectAsync(stringToEncrypt, publicKey).GetAwaiter().GetResult();
                var decrypted = asymEnc.DecryptObject(asymObj, privateKey);
                Assert.AreEqual(stringToEncrypt, decrypted);
            }

        }

        [TestMethod]
        public void TestAesWithDualCertPw()
        {

            var cert2 = LoadCertificate();
            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            var cert2Dual = LoadCertificate2();
            var publicKey2 = new X509CertificatePublicKey(cert2Dual);
            var privateKey2 = new X509Certificate2KeyEncryptionKey(cert2Dual);

            int length = 100;
            var rand = new RandomGenerator();

            for (int i = 0; i < length; i++)
            {
                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                var asymEnc = new AsymmetricEncryptor(AsymmetricStrategyOption.Legacy_Aes2);
                var asymObj = asymEnc.EncryptObjectAsync(stringToEncrypt, publicKey, publicKey2).GetAwaiter().GetResult();
                var decrypted = asymEnc.DecryptObject(asymObj, privateKey, privateKey2);
                Assert.AreEqual(stringToEncrypt, decrypted);
            }
        }

        [TestMethod]
        public void TestAes1000WithDualCertificate()
        {

            var cert2 = LoadCertificate();
            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            var cert2Dual = LoadCertificate2();
            var publicKey2 = new X509CertificatePublicKey(cert2Dual);
            var privateKey2 = new X509Certificate2KeyEncryptionKey(cert2Dual);

            int length = 100;
            var rand = new RandomGenerator();

            for (int i = 0; i < length; i++)
            {
                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                var asymEnc = new AsymmetricEncryptor(AsymmetricStrategyOption.Aes256_1000);
                var asymObj = asymEnc.EncryptObjectAsync(stringToEncrypt, publicKey, publicKey2).GetAwaiter().GetResult();
                var decrypted = asymEnc.DecryptObject(asymObj, privateKey, privateKey2);
                Assert.AreEqual(stringToEncrypt, decrypted);
            }
        }

        [TestMethod]
        public void TestAes5WithDualCertificate()
        {

            var cert2 = LoadCertificate();
            var publicKey = new X509CertificatePublicKey(cert2);
            var privateKey = new X509Certificate2KeyEncryptionKey(cert2);

            var cert2Dual = LoadCertificate2();
            var publicKey2 = new X509CertificatePublicKey(cert2Dual);
            var privateKey2 = new X509Certificate2KeyEncryptionKey(cert2Dual);

            int length = 100;
            var rand = new RandomGenerator();

            for (int i = 0; i < length; i++)
            {
                var stringToEncrypt = Guid.NewGuid().ToString("N") + ":* d’une secrétairE chargée des affaires des étudiants de la section";
                var asymEnc = new AsymmetricEncryptor(AsymmetricStrategyOption.Aes256_5);
                var asymObj = asymEnc.EncryptObject(stringToEncrypt, publicKey, publicKey2);
                var decrypted = asymEnc.DecryptObject(asymObj, privateKey, privateKey2);
                Assert.AreEqual(stringToEncrypt, decrypted);
            }
        }

    }

}

