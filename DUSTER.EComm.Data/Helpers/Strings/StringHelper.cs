using System.Security.Cryptography;
using System.Text;

namespace DUSTER.EComm.Data.Helpers.Strings
{
    public static class StringHelper
    {
        public static string GetUniqueString(int length = 5)
        {
            Random random = new Random();

            string chars = $"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789{DateTime.Now.ToString("ddMMyyyyHHmmssfffFFF")}"; // Alphanumeric (Uppercase + Digits)
            string code = string.Empty;

            code = new string(Enumerable.Range(0, length)
                .Select(_ => chars[random.Next(chars.Length)]).ToArray());

            return code;
        }

        public static string GetUniqueStringV2(int length = 5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var data = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            return new string(data.Select(b => chars[b % chars.Length]).ToArray());
        }

        public static string GetBase64String(object value)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
    }
}
