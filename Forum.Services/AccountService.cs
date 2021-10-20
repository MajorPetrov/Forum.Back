using System;
using System.Text;
using System.Security.Cryptography;
using ForumJV.Data.Services;

namespace ForumJV.Services
{
    public class AccountService : IAccount
    {
        /// <summary>
        /// Service Legacy. À Supprimer dans un futur proche.
        /// </summary>
        public AccountService()
        {
        }

        public bool VerifyLegacyPassword(string actualPassword, string hashedPassword)
        {
            var hash = HashLegacy(actualPassword);

            return hashedPassword.Equals(hash);
        }

        private string HashLegacy(string password)
        {
            password = Salt(password);
            var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        private string Salt(string password)
        {
            var salt = "3cd2{password}03{password}5ef85d4{passwor{password}d}f3fd26bcfd909bdd82923";

            return str_replace("{password}", password, salt);
        }

        private string str_replace(string oldValue, string newValue, string template)
        {
            return template.Replace(oldValue, newValue);
        }
    }
}