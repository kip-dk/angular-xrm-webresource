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

        IOrganizationService orgService;

        public ImportManager(string user, string pwd, string url)
        {
            var credentials = new ClientCredentials();
            credentials.UserName.UserName = user;
            credentials.UserName.Password = pwd;

            var config = ServiceConfigurationFactory.CreateConfiguration<IOrganizationService>(
                new Uri(url));


            orgService = new OrganizationServiceProxy(config, credentials);
            IOrganizationService service = (IOrganizationService)orgService;
        }

        public void Import(string dist, string name, string subPath, string solution, string prefix = null)
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

                var webResource = findWebresource(resourceName);
                if (webResource != null)
                {
                    if (new FileInfo(file).LastWriteTimeUtc > ((DateTime)webResource["modifiedon"]).ToUniversalTime())
                    {
                        Console.WriteLine("Updating " + resourceName);

                        webResource["content"] = Convert.ToBase64String(File.ReadAllBytes(file));
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
                    webResource["content"] = Convert.ToBase64String(File.ReadAllBytes(file));
                    webResource["displayname"] = name + ": " + resourceName;
                    webResource["description"] = "Imported as part of the " + name + " application";
                    webResource["webresourcetype"] = new Microsoft.Xrm.Sdk.OptionSetValue((int)filename.ToResourceType());
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
                this.Import(dist, name, dirname, solution, prefix);
            }
            #endregion
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
