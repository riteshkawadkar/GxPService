using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GxPService.Helpers
{
    public class EncryptionHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string EncryptionKey = "123456789012345678901234"; // 24 bytes


        public static void Encrypt(string plainText, string filePath)
        {
            byte[] keyBytes = UTF8Encoding.UTF8.GetBytes(EncryptionKey);
            //Console.WriteLine($"Key size in bytes: {keyBytes.Length}");


            try
            {
                byte[] inputArray = UTF8Encoding.UTF8.GetBytes(plainText);
                using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
                {
                    tripleDES.Key = UTF8Encoding.UTF8.GetBytes(EncryptionKey);
                    tripleDES.Mode = CipherMode.ECB;
                    tripleDES.Padding = PaddingMode.PKCS7;

                    ICryptoTransform cTransform = tripleDES.CreateEncryptor();
                    byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);

                    string encryptedData = Convert.ToBase64String(resultArray);

                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encryptedBytes = UTF8Encoding.UTF8.GetBytes(encryptedData);
                        fs.Write(encryptedBytes, 0, encryptedBytes.Length);
                    }

                    //Console.WriteLine("Encrypted file written successfully.");
                    log.Info("Encrypted file written successfully.");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error writing encrypted file: {ex.Message}");
                log.Error($"Error writing encrypted file: {ex.Message}");
            }
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                byte[] inputArray = Convert.FromBase64String(cipherText);

                using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
                {
                    tripleDES.Key = UTF8Encoding.UTF8.GetBytes(EncryptionKey);
                    tripleDES.Mode = CipherMode.ECB;
                    tripleDES.Padding = PaddingMode.PKCS7;

                    ICryptoTransform cTransform = tripleDES.CreateDecryptor();
                    byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);

                    return UTF8Encoding.UTF8.GetString(resultArray);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error while decrypting file " + ex.StackTrace());
                log.Error("Error while decrypting file " + ex.Message);
            }
            return "";
        }
    }
}
