using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deploy
{
    public static class Extensions
    {
        public static ResourceTypeEnum ToResourceType(this string filename)
        {
            var upperExtension = filename.ToUpper().Split('.').Last();

            switch (upperExtension)
            {
                case "HTM":
                case "HTML": return ResourceTypeEnum.Html;
                case "CSS": return ResourceTypeEnum.Css;
                case "JS": return ResourceTypeEnum.Jscript;
                case "XML": return ResourceTypeEnum.Xml;
                case "PNG": return ResourceTypeEnum.Png;
                case "JPEG":
                case "JPG": return ResourceTypeEnum.Jpg;
                case "GIF": return ResourceTypeEnum.Gif;
                case "XAP": return ResourceTypeEnum.Xap;
                case "XLS": return ResourceTypeEnum.Xsl;
                case "XSLT": return ResourceTypeEnum.Xsl;
                case "ICO": return ResourceTypeEnum.Ico;
            }
            return ResourceTypeEnum.Unknown;
        }

        public static byte[] DefaultContentForEmplyFile(this string filename)
        {
            var type = filename.ToResourceType();
            switch(type)
            {
                case ResourceTypeEnum.Html: return System.Text.Encoding.ASCII.GetBytes("<!-- Empty html file -->");
                case ResourceTypeEnum.Css: return System.Text.Encoding.ASCII.GetBytes("/* Empty css file */");
                case ResourceTypeEnum.Jscript: return System.Text.Encoding.ASCII.GetBytes("/* Empty javascript file */");
            }
            return new byte[0];
        }
    }
}
