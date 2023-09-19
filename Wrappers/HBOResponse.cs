namespace elando.ELK.TraceLogging.Wrappers
{
    #region usings
    using elando.ELK.TraceLogging.Constants;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    #endregion

    public class HBOResponse<T>
    {
        public string? DocType;
        public string CopyRight;
        public string VersionNumber;
        public string CreatedDate;
        public int StatusId;
        public string StatusText;
        public Guid ResponseId;
        public List<T> Values;

        /// <summary>
        /// Default Response object type.
        /// </summary>
        public HBOResponse()
        {
            this.Values = new List<T>();
            this.DocType = "0";
            this.CopyRight = ELKConstants.COPY_RIGHT;
            this.VersionNumber = ELKConstants.VERSION_NUMBER;
            this.CreatedDate = getDate();
            this.StatusId = 0;
            this.StatusText = "";
            this.ResponseId = Guid.NewGuid();
        }

        public void UpdateRequestId(Guid requestId)
        {
            this.ResponseId = requestId;
        }

        /// <summary>
        /// Response object with Values array & DocType filled
        /// </summary>
        /// <param name="Values">List of values to be printed</param>
        /// <param name="DocType">type of document</param>
        public HBOResponse(List<T>? values) : this()
        {
            this.Values = values;
        }

        /// <summary>
        /// Response object with "bad action" status code
        /// </summary>
        public void BadAction(string message)
        {
            this.DocType = ELKConstants.UNKNOWN;
            this.CopyRight = ELKConstants.COPY_RIGHT;
            this.VersionNumber = ELKConstants.VERSION_NUMBER;
            this.CreatedDate = getDate();
            this.StatusId = 0;
            this.StatusText = message;
            this.ResponseId = Guid.NewGuid();
        }

        public void Success()
        {
            this.CreatedDate = getDate();
            this.StatusId = 200;
            this.ResponseId = Guid.NewGuid();
            this.VersionNumber = ELKConstants.VERSION_NUMBER;
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

    public class HBOResponseWrapper<T>
    {
        public HBOResponse<T> Response;

        public HBOResponseWrapper()
        {
            this.Response = new HBOResponse<T>();
        }

        public HBOResponseWrapper(ref List<T> values)
        {
            this.Response = new HBOResponse<T>(values);
        }

        public HBOResponseWrapper<T> BadAction(string message)
        {
            this.Response.BadAction(message);

            return this;
        }

        public HBOResponseWrapper<T> Success(string message = "")
        {
            this.Response.StatusText = message;
            this.Response.Success();

            return this;
        }

        public string ToJSON()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
