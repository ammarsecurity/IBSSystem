
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections;
using System.Security.Cryptography;
using System.Text;


namespace IBSMobile.Functions
{
    public class Encryptions
    {

        // private static readonly string neptuneServer = "http://185.24.63.119:8090";
        private static readonly string saturnServer= "https://ibsapi2.alufiq.com";
        //private static readonly string localServer = "https://localhost:7250";


        public static string Encrypt(string plainText)
        {
            return Aes256CbcEncrypter.Encrypt(plainText, EncryptValues.Key);
        }

        public static string Decrypt(string plainText)
        {
            return Aes256CbcEncrypter.Decrypt(plainText, EncryptValues.Key);
        }

        public static async Task<string> EncryptDatabase(string plainText)
        {
            string connectionString = "";
            int index = plainText.IndexOf("Database");
            plainText = plainText.Substring(index);
            index = plainText.IndexOf("Trusted");
            plainText = plainText.Substring(0, index - 1);
            connectionString = plainText;


            var options = new RestClientOptions(saturnServer) { Timeout = Timeout.InfiniteTimeSpan };
            var client = new RestClient(options);
            var request = new RestRequest($"/api/IBS/Encrypt?text={connectionString}", Method.Get);

            var response = await client.ExecuteAsync(request);
            string stringResult = response.Content;
            return stringResult.Replace("\"", "");
        }

    }

    class Aes256CbcEncrypter
    {

        private static readonly Encoding encoding = Encoding.UTF8;

        public static string Encrypt(string plainText, string key)
        {
            try
            {
                RijndaelManaged aes = new RijndaelManaged();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                aes.Key = encoding.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(EncryptValues.initVector);

                ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] buffer = encoding.GetBytes(plainText);

                string encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));

                String mac = "";

                mac = BitConverter.ToString(HmacSHA256(Convert.ToBase64String(aes.IV) + encryptedText, key)).Replace("-", "").ToLower();

                var keyValues = new Dictionary<string, object>
                {
                    { "iv", Convert.ToBase64String(aes.IV) },
                    { "value", encryptedText },
                    { "mac", mac },
                };

                //JavaScriptSerializer serializer = new JavaScriptSerializer();

                return Convert.ToBase64String(encoding.GetBytes(JsonConvert.SerializeObject(keyValues)));
            }
            catch (Exception e)
            {
                throw new Exception("Error encrypting: " + e.Message);
            }
        }

        public static string Decrypt(string plainText, string key)
        {
            try
            {
                RijndaelManaged aes = new RijndaelManaged();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                aes.Key = encoding.GetBytes(key);

                // Base 64 decode
                byte[] base64Decoded = Convert.FromBase64String(plainText);
                string base64DecodedStr = encoding.GetString(base64Decoded);

                // JSON Decode base64Str
                //JavaScriptSerializer ser = new JavaScriptSerializer();
                var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(base64DecodedStr);

                aes.IV = Convert.FromBase64String(payload["iv"]);

                ICryptoTransform AESDecrypt = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] buffer = Convert.FromBase64String(payload["value"]);

                return encoding.GetString(AESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
            }
            catch (Exception e)
            {
                throw new Exception("Error decrypting: " + e.Message);
            }
        }

        static byte[] HmacSHA256(String data, String key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(key)))
            {
                return hmac.ComputeHash(encoding.GetBytes(data));
            }
        }
    }

    public class EncryptValues
    {
        public static string Key = "8UHjPgXZzXCGkhxV2QCnooyJexTzvJrO";

        public static string passPhrase = "OI&P5~MXqn^kU33^Mk8~@z#29D-s-I^y";
        public static string saltValue = "^@37yuaDD8KrTz3u96bvD+|-";
        public static string hashAlgorithm = "SHA256";
        public static int passwordIterations = 2;
        public static string initVector = "!kB$SUT^VD[+RO*E";
        public static int keySize = 256;

    }
}
