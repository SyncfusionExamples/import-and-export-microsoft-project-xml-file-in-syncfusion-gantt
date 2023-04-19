using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Syncfusion.EJ2.Gantt;
using System.Text.Json.Serialization;

namespace GanttXMLService
{
    public class GanttFileHandler
    {

        public string Open(GanttImportRequest importRequest)
        {
            Stream respStream;
            string xml = "xml", unSupportedFile = "UnsupportedFile";
            if (importRequest.FileStream != null)
            {
                if (string.IsNullOrEmpty(importRequest.FileType))
                    importRequest.FileType = xml;
            }
            else if (!string.IsNullOrEmpty(importRequest.Url))
            {
                try
                {
                    int read;
                    importRequest.FileStream = new MemoryStream();
                    HttpWebRequest fileReq = (HttpWebRequest)HttpWebRequest.Create(importRequest.Url);
                    HttpWebResponse fileResp = (HttpWebResponse)fileReq.GetResponse();
                    respStream = fileResp.GetResponseStream();
                    byte[] buffer = new byte[fileResp.ContentLength];
                    while ((read = respStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        importRequest.FileStream.Write(buffer, 0, read);
                    }
                    importRequest.FileStream.Position = 0;
                    if (string.IsNullOrEmpty(importRequest.FileType))
                        importRequest.FileType = xml;
                }
                catch
                {
                    return "InvalidUrl";
                }
            }
            else if (importRequest.File != null)
            {
                importRequest.FileStream = importRequest.File.OpenReadStream();
                if (string.IsNullOrEmpty(importRequest.FileType))
                    importRequest.FileType = importRequest.File.FileName.Split(new char[] { '.' }).Last();
            }
            else
                return unSupportedFile;

           return GanttImport.ImportFromXML(importRequest);
        }

        public void Save(string ganttModel, string filename, HttpResponse response)
        {
            GanttExport exportObj = new GanttExport();
            exportObj.FileName = filename;
            exportObj.Export(ConvertGanttObject(ganttModel), response);
        }


        private Gantt ConvertGanttObject(string ganttModel)
        {
            Dictionary<string, object> div = JsonSerializer.Deserialize<Dictionary<string, object>>(ganttModel);
            if (div == null) { return null; }
            Gantt ganttProp = new Gantt();
            foreach (KeyValuePair<string, object> ds in div)
            {
                var property = ganttProp.GetType().GetProperty(ds.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    Type type = property.PropertyType;
                    string serialize = JsonSerializer.Serialize(ds.Value);
                    object value ;
                    if (ds.Key == "dataSource")
                    {
                        value = JsonSerializer.Deserialize<List<BusinessObject>>(serialize);
                    }
                    else if (ds.Key == "resources")
                    {
                        value = JsonSerializer.Deserialize<List<GanttResources>>(serialize);
                    }
                    else
                    {
                        var converter = new JsonStringEnumConverter();
                        JsonSerializerOptions opt = new JsonSerializerOptions();
                        opt.Converters.Add(converter);
                        value = JsonSerializer.Deserialize(serialize, type, opt);
                    }
                    property.SetValue(ganttProp, value, null);
                }
            }
            return ganttProp;
        }
    }
}
