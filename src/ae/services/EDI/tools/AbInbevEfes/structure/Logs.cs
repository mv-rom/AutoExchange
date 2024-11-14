using System;



namespace ae.services.EDI.tools.AbInbevEfes.structure
{
    [Serializable]
    internal class LogsResponse
    {
        public string id { get; set; }
        public string traceIdentifier { get; set; }
        public string operation { get; set; }
        public string logLevel { get; set; }
        public string message { get; set; }
        public string custId { get; set; }
        public string date { get; set; }
    }

    [Serializable]
    internal class LogsErrorAnswer
    {
        public string error { get; set; }  //? "invalid_request"
        public string error_description { get; set; }
    }
}
