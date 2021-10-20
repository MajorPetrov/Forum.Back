namespace ForumJV.Data.Services
{
    public interface IAccount
    {
        bool VerifyLegacyPassword(string actualPassword, string password);
    }
}