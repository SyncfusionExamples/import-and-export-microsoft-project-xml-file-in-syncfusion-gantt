using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Web;
namespace GanttXMLService
{
        [Serializable]
        public class GanttImportRequest
        {
            public IFormFile? File
            {
                get;
                set;
            }

            public string Url
            {
                get;
                set;
            }

            public Stream FileStream
            {
                get;
                set;
            }

            public string FileType
            {
                get;
                set;
            }

        }
}