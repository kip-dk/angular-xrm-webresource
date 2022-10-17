using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Deploy.XrmOrganization
{
    internal class TokenHelper
    {
        private Guid applicationId;
        private string secret;
        private string resourceUrl;
        private Guid tenentId;

        private string token;
        private DateTime expire;

        private string tokenUrl = "https://login.microsoftonline.com/{0}/oauth2/token";

        private static Dictionary<Guid, TokenHelper> helpers = new Dictionary<Guid, TokenHelper>();

        internal static TokenHelper GetTokenHelper(Guid applicationId, string secret, string resourceUrl, Guid tenentId)
        {
            lock (helpers)
            {
                if (helpers.ContainsKey(applicationId))
                {
                    return helpers[applicationId];
                }
                var helper = new TokenHelper(applicationId, secret, resourceUrl, tenentId);
                helpers.Add(applicationId, helper);
                return helper;
            }
        }

        private TokenHelper(Guid applicationId, string secret, string resourceUrl, Guid tenentId)
        {
            this.applicationId = applicationId;
            this.secret = secret;
            this.resourceUrl = resourceUrl;
            this.tenentId = tenentId;
        }

        internal string GetToken()
        {
            if (!string.IsNullOrEmpty(token) && expire > System.DateTime.Now)
            {
                return this.token;
            }

            var client = WebRequest.Create(string.Format(this.tokenUrl, this.tenentId.ToString()));

            client.Method = "POST";
            client.ContentType = "application/x-www-form-urlencoded";

            using (var req = client.GetRequestStream())
            {
                var v = "grant_type=client_credentials&client_id=" + System.Web.HttpUtility.UrlEncode(this.applicationId.ToString()) + "&client_secret=" + System.Web.HttpUtility.UrlEncode(this.secret) + "&resource=" + System.Web.HttpUtility.UrlEncode(this.resourceUrl);
                var data = System.Text.Encoding.UTF8.GetBytes(v);
                req.Write(data, 0, data.Length);

                var resp = client.GetResponse();
                using (var result = resp.GetResponseStream())
                {
                    var ser = new DataContractJsonSerializer(typeof(Token));
                    var token = (Token)ser.ReadObject(result);
                    this.token = token.access_token;
                    this.expire = System.DateTime.Now.AddSeconds((Int32.Parse(token.expires_in) - 5));
                }
            }
            return this.token;
        }

        [DataContract]
        internal class Token
        {
            [DataMember]
            public string token_type { get; set; }

            [DataMember]
            public string expires_in { get; set; }

            [DataMember]
            public string ext_expires_in { get; set; }

            [DataMember]
            public string expires_on { get; set; }

            [DataMember]
            public string not_before { get; set; }

            [DataMember]
            public string resource { get; set; }

            [DataMember]
            public string access_token { get; set; }
        }
    }
}
