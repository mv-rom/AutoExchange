using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ae.lib.classes.AbInbevEfes
{
    internal class Authorization
    {
    }

    [Serializable]
    internal class AuthorizationAnswer
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    [Serializable]
    internal class AuthorizationErrorAnswer
    {
        public string error { get; set; } //"invalid_request"
    }
}
