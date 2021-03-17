using System;
using System.Security.Cryptography;
using System.Text;

namespace ServiceComponents.Core.Extensions
{
    public static class StringHelper
    {
        public static string AddRandomPostfix(this string str, char separator = '-', int length = 10, string characterSet = RandomCharacterSet.Alphanumeric)
        {
            return $"{str}{separator}{Random(length, characterSet)}";
        }

        public static string AddRandomPrefix(this string str, char separator = '-', int length = 10, string characterSet = RandomCharacterSet.Alphanumeric)
        {
            return $"{Random(length, characterSet)}{separator}{str}";
        }

        public static string Random(int length, string characterSet = RandomCharacterSet.Alphanumeric)
        {
            var res = new StringBuilder();
            
            using (var rng = new RNGCryptoServiceProvider())
            {
                var uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    var num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(characterSet[(int)(num % (uint)characterSet.Length)]);
                }
            }

            return res.ToString();
        }
    }

    public class RandomCharacterSet
    {
        public const string Alphanumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        public const string AlphanumericAndSpecial = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890 !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
        public const string Letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string Numbers = "1234567890";
        public const string Special = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ ";
    }
}
