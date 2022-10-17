using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Linq;
using System.ServiceModel.Description;

namespace Deploy
{
    public class ImportManager
    {
        private const string HREF = "href=\"";
        private const string HREF_HTTP = "href=\"http";
        private const string HREF_PROTECT = "$HREFPROTECT$";
        private const string SRC = "src=\"";
        private const string SRC_HTTP = "src=\"http";
        private const string SRC_PROTECT = "$SRCPROTECT";

        IOrganizationService orgService;

        public ImportManager(string user, string pwd, string url)
        {
            var credentials = new ClientCredentials();
            credentials.UserName.UserName = user;
            credentials.UserName.Password = pwd;

            var config = ServiceConfigurationFactory.CreateConfiguration<IOrganizationService>(
                new Uri(url));

            orgService = new OrganizationServiceProxy(config, credentials);
        }

        public ImportManager(string connectionStrig)
        {
            orgService = new XrmOrganization.OAuthOrganizationService(connectionStrig);
        }

        public void Import(string dist, string name, string subPath, string solution, string[] routes, string prefix = null)
        {
            var path = dist;

            if (!string.IsNullOrEmpty(subPath))
            {
                path += @"\" + subPath;
            }

            if (prefix == null)
            {
                prefix = findCustomizationPrefix(solution);
            }

            #region upload files
            foreach (var file in Directory.GetFiles(path))
            {
                var filename = Path.GetFileName(file);
                var resourceName =  prefix + "_/" + name + (!string.IsNullOrEmpty(subPath) ? "/" + subPath.Replace("\\", "/") : "") + "/" + filename;
                this.UploadContent(name, solution, resourceName, file, null, true);

                if (file.EndsWith("index.html") && routes != null && routes.Length > 0)
                {
                    foreach (var route in routes)
                    {
                        resourceName = prefix + "_/" + name + "/" + route;
                        var html = File.ReadAllText(file);
                        var count = route.Split('/').Length;

                        if (count > 0)
                        {
                            html = html.Replace(HREF_HTTP, HREF_PROTECT);
                            var newhref = HREF + "./%7B" + System.DateTime.Now.Ticks.ToString() + "%7D/";
                            for (var i=0;i<count;i++)
                            {
                                newhref += "../";
                            }

                            html = html.Replace(HREF, newhref);
                            html = html.Replace(HREF_PROTECT, HREF_HTTP);

                            html = html.Replace(SRC_HTTP, SRC_PROTECT);
                            var newsrc = SRC + "./%7B" + System.DateTime.Now.Ticks.ToString() + "%7D/";

                            for (var i=0;i<count;i++)
                            {
                                newsrc += "../";
                            }

                            html = html.Replace(SRC, newsrc);
                            html = html.Replace(SRC_PROTECT, SRC_HTTP);
                        }

                        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
                        var data = Convert.ToBase64String(bytes);

                        this.UploadContent(name, solution, resourceName, $"({route}){file}", data, false);
                    }
                }
            }
            #endregion
            #region upload subs
            var sub = path;
            if (!string.IsNullOrEmpty(subPath))
            {
                sub = path + @"\" + subPath;
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirname = Path.GetFileName(dir);
                if (dirname == "out-tsc")
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(subPath))
                {
                    dirname = subPath + @"\" + dirname;
                }
                this.Import(dist, name, dirname, solution, null, prefix);
            }
            #endregion
        }


        private void UploadContent(string name, string solution, string resourceName, string file, string data, bool modifycheck)
        {
            var hasData = data != null;

            var filename = Path.GetFileName(file);
            var webResource = findWebresource(resourceName);

            if (data == null)
            {
                data = Convert.ToBase64String(File.ReadAllBytes(file));
            }

            if (data.Length == 0)
            {
                data = Convert.ToBase64String(filename.DefaultContentForEmplyFile());
            }

            if (webResource != null)
            {
                if (!modifycheck || new FileInfo(file).LastWriteTimeUtc > ((DateTime)webResource["modifiedon"]).ToUniversalTime())
                {
                    Console.WriteLine("Updating " + resourceName);

                    webResource["content"] = data;
                    orgService.Update(webResource);

                    var publishRequest = new PublishXmlRequest
                    {
                        ParameterXml = string.Format("<importexportxml><webresources><webresource>{0}</webresource></webresources></importexportxml>", webResource.Id)
                    };
                    orgService.Execute(publishRequest);
                }
            }
            else
            {
                webResource = new Entity
                {
                    Id = Guid.NewGuid(),
                    LogicalName = "webresource"
                };
                webResource["name"] = resourceName;
                webResource["content"] = data;
                webResource["displayname"] = name + ": " + resourceName;
                webResource["description"] = "Imported as part of the " + name + " application";
                var type = hasData ? ResourceTypeEnum.Html : filename.ToResourceType();

                if (type == ResourceTypeEnum.Unknown)
                {
                    Console.WriteLine("Warning : unable to map file to Dynamics 365 web resource type " + filename + ". The file was ignored");
                    return;
                }
                webResource["webresourcetype"] = new Microsoft.Xrm.Sdk.OptionSetValue((int)type);
                orgService.Create(webResource);

                var publishRequest = new PublishXmlRequest
                {
                    ParameterXml = string.Format("<importexportxml><webresources><webresource>{0}</webresource></webresources></importexportxml>", webResource.Id)
                };
                orgService.Execute(publishRequest);

                // attach new webResource to solution
                var request = new Microsoft.Crm.Sdk.Messages.AddSolutionComponentRequest
                {
                    ComponentType = 61, // Web Resource,
                    ComponentId = webResource.Id,
                    SolutionUniqueName = solution
                };

                orgService.Execute(request);
                Console.WriteLine("Created " + resourceName);
            }
        }

        private Entity findWebresource(string filename)
        {
            var query = new QueryExpression("webresource");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddCondition("name", ConditionOperator.Equal, filename);

            var res = orgService.RetrieveMultiple(query);
            return res.Entities.SingleOrDefault();
        }

        private string findCustomizationPrefix(string uniqueName)
        {
            var query = new QueryExpression("solution");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);

            var res = orgService.RetrieveMultiple(query);
            Entity solution = null;
            solution = res.Entities.SingleOrDefault();

            if (solution != null)
            {
                var publisherid = ((Microsoft.Xrm.Sdk.EntityReference)solution["publisherid"]).Id;

                query = new QueryExpression("publisher");
                query.ColumnSet = new ColumnSet(true);
                query.Criteria.AddCondition("publisherid", ConditionOperator.Equal, publisherid);

                res = orgService.RetrieveMultiple(query);
                return (string)res.Entities.Single()["customizationprefix"];

            }
            throw new InvalidPluginExecutionException("Unable to map solution name " + uniqueName + " to a customization prefix");
        }

    }
}
