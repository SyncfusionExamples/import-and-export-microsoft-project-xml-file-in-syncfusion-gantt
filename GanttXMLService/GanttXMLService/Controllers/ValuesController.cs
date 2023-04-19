using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace GanttXMLService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class XmlImportController : ControllerBase
    {

        [HttpGet(Name = "GetXML")]
        public IEnumerable<int> Get()
        {
            return new int[] { 1, 2, };
        }

        // GET api/<controller>
        [HttpPost(Name ="ExportToXML")]
        [Route("ExportToXML")]
        [AcceptVerbs("POST")]
        public void ExportToXML()
        {
            var queryString = Request.Form;
            string GanttModel = queryString["GanttData"];
            GanttFileHandler xmlHelper = new GanttFileHandler();
            xmlHelper.Save(GanttModel, "Gantt", Response);
        }


        [HttpPost(Name = "Import")]
        [Route("Import")]
        [AcceptVerbs("Post")]
        public string Import()
        {
            var files = Request.Form.Files;
            GanttImportRequest importRequest = new GanttImportRequest();
            if (files.Count == 0)
            {
                importRequest.Url = Request.Form["url"];
            }
            else if (files.Count > 0)
            {
                var obj = files[0];
                importRequest.FileStream = obj.OpenReadStream();
                importRequest.FileType = obj.FileName.Split('.')[obj.FileName.Split('.').Length - 1];
                importRequest.File = null;
            }
            GanttFileHandler xmlHelper = new GanttFileHandler();
            string str = xmlHelper.Open(importRequest);
            return str;
        }
    }
}
