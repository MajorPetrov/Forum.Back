namespace ForumJV.Models.Manage
{
    public class TwoFactorAuthenticationModel
    {
        public bool HasAuthenticator { get; set; }
        public bool Is2FAEnabled { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }
}