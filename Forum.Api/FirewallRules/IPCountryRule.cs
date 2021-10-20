using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Firewall;

namespace ForumJV.FirewallRules
{
    public class IPCountryRule : IFirewallRule
    {
        private readonly IFirewallRule _nextRule;
        private readonly IList<string> _allowedCountryCodes;

        public IPCountryRule(IFirewallRule nextRule, IList<string> allowedCountryCodes)
        {
            _nextRule = nextRule;
            _allowedCountryCodes = allowedCountryCodes;
        }

        public bool IsAllowed(HttpContext context)
        {
            const string headerKey = "CF-IPCountry";

            if (!context.Request.Headers.ContainsKey(headerKey))
                return _nextRule.IsAllowed(context);

            var countryCode = context.Request.Headers[headerKey].ToString();
            var isAllowed = _allowedCountryCodes.Contains(countryCode);

            return isAllowed || _nextRule.IsAllowed(context);
        }
    }
}