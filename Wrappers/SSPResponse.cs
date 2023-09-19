namespace elando.ELK.TraceLogging.Wrappers
{
    #region Usings
    using elando.ELK.TraceLogging.Constants;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    #endregion

    public class SSPResponse<T>
    {
        public string? DocType;
        public string CopyRight;
        public string VersionNumber;
        public string CreatedDate;
        public int StatusId;
        /// <summary>
        /// First Bg then En
        /// </summary>
        public List<string> StatusText;
        public Guid ResponseId;
        public List<T>? Values;

        /// <summary>
        /// Default Response object type.
        /// </summary>
        public SSPResponse()
        {
            Values = new List<T>();
            ResponseId = new Guid();
            DocType = "0";
            CopyRight = ELKConstants.COPY_RIGHT;
            VersionNumber = ELKConstants.VERSION_NUMBER;
            CreatedDate = getDate();
            StatusId = 0;
            StatusText = new List<string>();
            ResponseId = Guid.NewGuid();
        }

        public void UpdateRequestId(Guid requestId)
        {
            ResponseId = requestId;
        }

        /// <summary>
        /// Response object with Values array & DocType filled
        /// </summary>
        /// <param name="Values">List of values to be printed</param>
        /// <param name="DocType">type of document</param>
        public SSPResponse(List<T>? values) : this()
        {
            Values = values;
        }

        /// <summary>
        /// Response object with "bad action" status code
        /// </summary>
        public void BadAction(List<string> messages)
        {
            DocType = ELKConstants.UNKNOWN;
            CopyRight = ELKConstants.COPY_RIGHT;
            VersionNumber = ELKConstants.VERSION_NUMBER;
            CreatedDate = getDate();
            StatusId = 0;
            StatusText.AddRange(messages);
            ResponseId = Guid.NewGuid();
        }

        public void Success()
        {
            CreatedDate = getDate();
            StatusId = 200;
            ResponseId = Guid.NewGuid();
            VersionNumber = ELKConstants.VERSION_NUMBER;
        }

        /// <returns>Current UTC Time in format (YYYY-MM-DDTHH:MM:SSZ)</returns>
        private string getDate()
        {
            //NOTE: Does this need to handle exceptions?
            var r = DateTime.UtcNow.ToString("s");
            if (r != null)
            {
                return r + 'Z';
            }
            return "unknown";
        }
    }

    public class SSPResponseWrapper<T>
    {
        public SSPResponse<T> Response;

        public SSPResponseWrapper()
        {
            Response = new SSPResponse<T>();
        }

        public SSPResponseWrapper(ref List<T> values)
        {
            Response = new SSPResponse<T>(values);
        }

        public SSPResponseWrapper<T> BadAction(List<string> messages)
        {
            Response.BadAction(messages);

            return this;
        }

        public SSPResponseWrapper<T> Success(List<string>? messages)
        {
            if (messages is not null && messages.Any())
            {
                Response.StatusText.AddRange(messages);
            }

            Response.Success();

            return this;
        }

        public string ToJSON()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
