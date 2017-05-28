using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace Deploy
{
    [DataContract]
    public class Setting
    {
        [DataMember(Name = "solution")]
        public string Solution { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "dist")]
        public string Dist { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "user")]
        public string User { get; set; }
        [DataMember(Name = "password")]
        public string Password { get; set; }

        public static Setting[] GetSettings()
        {
            if (System.IO.File.Exists("xrm.deploy.json"))
            {
                // the double read nature of this is to overcome the json BOM parse problem, when the json config file is created or maintained with visual studio
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Setting[]));
                var settingString = System.IO.File.ReadAllText("xrm.deploy.json", Encoding.UTF8);
                using (var mem = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(settingString)))
                {

                    return (Setting[])js.ReadObject(mem);
                }
            }
            else
            {
                throw new FileNotFoundException("Expected to find a file name [xrm.deploy.json] in current folder");
            }
        }
    }
}
