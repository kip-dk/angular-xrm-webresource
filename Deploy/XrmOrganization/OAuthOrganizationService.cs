using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.WebServiceClient;
using System;

namespace Deploy.XrmOrganization
{
    internal class OAuthOrganizationService : IOrganizationService, IDisposable
    {
        private OrganizationWebProxyClient client;
        private string organizationUrl;
        private Guid tenentid;

        private TokenHelper tokenHelper;

        internal OAuthOrganizationService(string connectionString)
        {
            var conn = new Connection(connectionString);
            this.organizationUrl = conn.organizationUrl;
            this.tenentid = conn.tenentid;
            this.Initialze(conn.clientId, conn.secret);
        }

        private bool Initialze(Guid clientId, string secret)
        {
            this.tokenHelper = TokenHelper.GetTokenHelper(clientId, secret, this.organizationUrl, tenentid);
            this.client = new OrganizationWebProxyClient(GetServiceUrl(), new TimeSpan(0, 15, 0), this.GetType().Assembly);
            this.RefreshToken();
            return true;
        }

        public Guid CallerId
        {
            get
            {
                return this.client.CallerId;
            }
            set
            {
                this.client.CallerId = value;
            }
        }

        private bool RefreshToken()
        {
            this.client.HeaderToken = tokenHelper.GetToken();
            return true;
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            this.RefreshToken();
            client.Associate(entityName, entityId, relationship, relatedEntities);
        }

        public Guid Create(Entity entity)
        {
            this.RefreshToken();
            return client.Create(entity);
        }

        public void Delete(string entityName, Guid id)
        {
            this.RefreshToken();
            client.Delete(entityName, id);
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            this.RefreshToken();
            client.Disassociate(entityName, entityId, relationship, relatedEntities);
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            this.RefreshToken();
            return client.Execute(request);
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            this.RefreshToken();
            return client.Retrieve(entityName, id, columnSet);
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            this.RefreshToken();
            return client.RetrieveMultiple(query);
        }

        public void Update(Entity entity)
        {
            this.RefreshToken();
            this.client.Update(entity);
        }

        private Uri GetServiceUrl()
        {
            return new Uri(this.organizationUrl + @"/xrmservices/2011/organization.svc/web?SdkClientVersion=9.0");
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        internal class Connection
        {
            internal Connection(string connectionString)
            {
                var pms = connectionString.Split(';');
                foreach (var pm in pms)
                {
                    if (!string.IsNullOrEmpty(pm))
                    {
                        var splitpos = pm.IndexOf('=');
                        if (splitpos > 0)
                        {
                            var name = pm.Substring(0, splitpos).Trim().ToUpper();
                            var value = pm.Substring(splitpos + 1);
                            switch (name)
                            {
                                case "URL": this.organizationUrl = value; break;
                                case "CLIENTID": this.clientId = new Guid(value); break;
                                case "PASSWORD": this.secret = value; break;
                                case "CLIENTSECRET": this.secret = value; break;
                                case "TENENTID": this.tenentid = new Guid(value); break;
                            }
                        }
                    }
                }
            }

            internal string organizationUrl;
            internal System.Guid clientId;
            internal string secret;
            internal Guid tenentid;
        }
    }
}
