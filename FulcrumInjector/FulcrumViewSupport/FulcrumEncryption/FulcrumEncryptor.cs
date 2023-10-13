using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FulcrumInjector.Properties;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumEncryption
{
    /// <summary>
    /// Static helper class used to encrypt and decrypt content being sent back and forth to the API
    /// These routines are used mainly for when we're dealing with passwords for users, but allows us an extra
    /// layer of security if needed
    /// </summary>
    public static class FulcrumEncryptor
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // RNG Helper object for building random keys
        private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

        // Preconfigured Encryption Parameters
        public static readonly int KeyBitSize = 256;
        public static readonly int BlockBitSize = 128;
        public static readonly int KeyByteSize = KeyBitSize / 8;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper that generates a random key on each call.
        /// </summary>
        /// <returns></returns>
        public static byte[] GenerateKey()
        {
            // Build a new key using a random set of bytes
            var SeedKey = new byte[KeyBitSize / 8];
            Random.GetBytes(SeedKey);
            return SeedKey;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Simple Encryption (AES) then Authentication (HMAC) for a UTF8 Message.
        /// </summary>
        /// <param name="ContentToEncrypt">The content we want to encrypt</param>
        /// <param name="OptionalPayload">(Optional) Non-Secret Payload</param>
        /// <returns> The content provided as an encrypted Message</returns>
        /// <exception cref="ArgumentException">Thrown when the input content is not provided for encryption</exception>
        public static string Encrypt(string ContentToEncrypt, byte[] OptionalPayload = null)
        {
            // Make sure the content we're encrypting exists and has length 
            if (string.IsNullOrEmpty(ContentToEncrypt))
                throw new ArgumentException("Payload for encryption required!", nameof(ContentToEncrypt));

            // Convert the content into a byte array and then store the cypher content from the encryption of it
            var PlainText = Encoding.UTF8.GetBytes(ContentToEncrypt);
            var CipherContent = Encrypt(PlainText, OptionalPayload);

            // Convert the encrypted bytes into a base 64 string and return it out
            return Convert.ToBase64String(CipherContent);
        }
        /// <summary>
        /// Simple Encryption(AES) then Authentication (HMAC) for a UTF8 Message.
        /// </summary>
        /// <param name="ContentToEncrypt">The content we want to encrypt</param>
        /// <param name="OptionalPayload">(Optional) Non-Secret Payload</param>
        /// <returns>Encrypted Message</returns>
        /// <exception cref="ArgumentException">Thrown when one of the key values or the input content is null</exception>
        public static byte[] Encrypt(byte[] ContentToEncrypt, byte[] OptionalPayload = null)
        {
            // Do basic error checking on input arguments to ensure they're populated
            if (EncryptionKeys.CryptographicKey == null || EncryptionKeys.CryptographicKey.Length != KeyBitSize / 8)
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", KeyBitSize), nameof(EncryptionKeys.CryptographicKey));
            if (EncryptionKeys.AutorizationKey == null || EncryptionKeys.AutorizationKey.Length != KeyBitSize / 8)
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", KeyBitSize), nameof(EncryptionKeys.AutorizationKey));
            if (ContentToEncrypt == null || ContentToEncrypt.Length < 1)
                throw new ArgumentException("Secret Message Required!", nameof(ContentToEncrypt));

            // Configure byte arrays for optional payload content and our cipher/IV content
            byte[] CipherText; byte[] IVKey;
            OptionalPayload ??= new byte[] { };

            // Build a new AES manager and begin encrypting now
            using (var AesManager = new AesManaged { KeySize = KeyBitSize, BlockSize = BlockBitSize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            {
                // Spawn in a random IV key here for the encryptor and store it
                AesManager.GenerateIV();
                IVKey = AesManager.IV;

                // Spawn in the encryptor object here using the IV key provided 
                using (var AesEncryptor = AesManager.CreateEncryptor(EncryptionKeys.CryptographicKey, IVKey))
                using (var CipherStream = new MemoryStream())
                {
                    // Copy the content from our encryptor into a crypto stream and prepare to save it as our cipher 
                    using (var CryptoStream = new CryptoStream(CipherStream, AesEncryptor, CryptoStreamMode.Write))
                    using (var BinaryWriter = new BinaryWriter(CryptoStream))
                    {
                        // Perform the actual encryption routine here
                        BinaryWriter.Write(ContentToEncrypt);
                    }

                    // Store the content from the cipher stream into the cipher buffer
                    CipherText = CipherStream.ToArray();
                }
            }

            // Assemble encrypted message and add authentication
            using (var HMAC256 = new HMACSHA256(EncryptionKeys.AutorizationKey))
            using (var EncryptedStream = new MemoryStream())
            {
                using (var BinaryWriter = new BinaryWriter(EncryptedStream))
                {
                    BinaryWriter.Write(OptionalPayload);   // Prepend the non-encrypted payload
                    BinaryWriter.Write(IVKey);             // Prepend the IV
                    BinaryWriter.Write(CipherText);        // Write the cipher text
                    BinaryWriter.Flush();                  // Flush out once we've written content

                    // Authenticate all data in the binary writer and write this checksum to it
                    var ChecksumTag = HMAC256.ComputeHash(EncryptedStream.ToArray());
                    BinaryWriter.Write(ChecksumTag);
                }

                // Finally, return out the encrypted content we've built
                return EncryptedStream.ToArray();
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Simple Authentication (HMAC) then Decryption (AES) for a secrets UTF8 Message.
        /// </summary>
        /// <param name="EncryptedMessage">The encrypted message to decrypt</param>
        /// <param name="OptionalPayloadLength">(Optional) Non-Secret Payload</param>
        /// <returns>The decrypted Message from our encrypted input content</returns>
        /// <exception cref="ArgumentException">Thrown when the input content is not provided for decryption or one of our keys is missing</exception>
        public static string Decrypt(string EncryptedMessage, int OptionalPayloadLength = 0)
        {
            // Make sure the content we're encrypting exists and has length 
            if (string.IsNullOrEmpty(EncryptedMessage))
                throw new ArgumentException("Payload for decryption required!", nameof(EncryptedMessage));

            // Convert the content into a byte array and then store the cypher content from the decryption of it
            var CipherText = Convert.FromBase64String(EncryptedMessage);
            var PlainText = Decrypt(CipherText, OptionalPayloadLength);

            // Convert the decrypted bytes into a base 64 string and return it out
            return Encoding.UTF8.GetString(PlainText);
        }
        /// <summary>
        /// Simple Authentication (HMAC) then Decryption (AES) for a secrets UTF8 Message.
        /// </summary>
        /// <param name="EncryptedMessage">The encrypted message to decrypt</param>
        /// <param name="OptionalPayloadLength">(Optional) Non-Secret Payload</param>
        /// <returns>The decrypted Message from our encrypted input content</returns>
        /// <exception cref="ArgumentException">Thrown when the input content is not provided for decryption</exception>
        public static byte[] Decrypt(byte[] EncryptedMessage, int OptionalPayloadLength = 0)
        {
            // Do basic error checking on input arguments to ensure they're populated
            if (EncryptionKeys.CryptographicKey == null || EncryptionKeys.CryptographicKey.Length != KeyBitSize / 8)
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", KeyBitSize), nameof(EncryptionKeys.CryptographicKey));
            if (EncryptionKeys.AutorizationKey == null || EncryptionKeys.AutorizationKey.Length != KeyBitSize / 8)
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", KeyBitSize), nameof(EncryptionKeys.AutorizationKey));
            if (EncryptedMessage == null || EncryptedMessage.Length < 1)
                throw new ArgumentException("Secret Message Required!", nameof(EncryptedMessage));

            // Build a new HMAC 256 Helper using the auth key to start decryption
            using (var hmac = new HMACSHA256(EncryptionKeys.AutorizationKey))
            {
                // Calculate the checksum tag on the input content and attempt to calculate it
                var ChecksumTag = new byte[hmac.HashSize / 8];
                var CalculatedTad = hmac.ComputeHash(EncryptedMessage, 0, EncryptedMessage.Length - ChecksumTag.Length);
                var IvLength = (BlockBitSize / 8);

                // If the message length is to small just return null from this routine
                if (EncryptedMessage.Length < ChecksumTag.Length + OptionalPayloadLength + IvLength) return null;

                // Grab the sent checksum tag and copy it into the checksum array
                Array.Copy(
                    EncryptedMessage,
                    EncryptedMessage.Length - ChecksumTag.Length,
                    ChecksumTag,
                    0,
                    ChecksumTag.Length
                );

                // Compare the Tag with constant time comparison
                var TagCompare = 0;
                for (var i = 0; i < ChecksumTag.Length; i++)
                    TagCompare |= ChecksumTag[i] ^ CalculatedTad[i];

                // If message doesn't authenticate return null (decryption failed)
                if (TagCompare != 0) return null;

                using (var AesManaged = new AesManaged { KeySize = KeyBitSize, BlockSize = BlockBitSize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
                {
                    // Grab the IV key from the input encrypted message and copy it into our temp buffer
                    var IVKey = new byte[IvLength];
                    Array.Copy(EncryptedMessage, OptionalPayloadLength, IVKey, 0, IVKey.Length);

                    // Spawn a new AES decrypter using the AES manager and our key values found
                    using (var Decrypter = AesManaged.CreateDecryptor(EncryptionKeys.CryptographicKey, IVKey))
                    using (var PlainTextStream = new MemoryStream())
                    {
                        // Copy the content from the decrypted stream values into our memory stream to hold the content needed
                        using (var decrypterStream = new CryptoStream(PlainTextStream, Decrypter, CryptoStreamMode.Write))
                        using (var BinaryWriter = new BinaryWriter(decrypterStream))
                        {
                            // Decrypt Cipher Text from Message and store it in the writer
                            BinaryWriter.Write(
                                EncryptedMessage,
                                OptionalPayloadLength + IVKey.Length,
                                EncryptedMessage.Length - OptionalPayloadLength - IVKey.Length - ChecksumTag.Length
                            );
                        }

                        // Finally return the plain text found during decryption
                        return PlainTextStream.ToArray();
                    }
                }
            }
        }
    }
}
