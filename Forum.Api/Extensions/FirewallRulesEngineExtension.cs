using Firewall;
using ForumJV.FirewallRules;
using ForumJV.Data.Options;

namespace ForumJV.Extensions
{
    public static class FirewallRulesEngineExtension
    {
        public static IFirewallRule ExceptFromCountryCodes(this IFirewallRule rule)
        {
            return new IPCountryRule(rule, CountryCodesAccessor.Accessor.CountryCodes);
        }
    }
}