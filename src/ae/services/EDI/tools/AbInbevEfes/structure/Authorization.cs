using System;



namespace ae.services.EDI.tools.AbInbevEfes.structure
{
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
        public string error { get; set; } // ? "invalid_grant"
        public string error_description { get; set; }
    }
}
