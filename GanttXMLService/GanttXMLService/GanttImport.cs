using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
#if !(SyncfusionDNX5_0)
using System.Web;
#endif
using Syncfusion.ProjIO;
using System.Globalization;
using System.ComponentModel;
using Newtonsoft.Json;
namespace GanttXMLService
{
    
    public class GanttImport
    {
        public static string DateFormat = "";
        public static Boolean RenderBaseline = false;
        public static Boolean EnableResourceMappings = false;

        #region methods
        public static string ImportFromXML(GanttImportRequest importRequest)
        {
            try
            {
                Project project = ProjectReader.Open(importRequest.FileStream);
                Dictionary<string, object> GanttModel;
                GanttModel = new Dictionary<string, object> {{ "dataSource", ReadGanttTasks(project) } };
                GanttModel.Add("resources", GetGanttResourceCollection(project));
                GanttModel.Add("holidays", GetGanttHolidays(project));
                GanttModel.Add("scheduleStartDate", project.StartDate.ToShortDateString());
                GanttModel.Add("scheduleEndDate", project.FinishDate.ToShortDateString());
                GanttModel.Add("includeWeekend", CheckIncludeWeekEnd(project));
                GanttModel.Add("dateFormat", DateFormat);
                GanttModel.Add("renderBaseline", RenderBaseline);
                GanttModel.Add("enableResourceMappings", EnableResourceMappings);
                string outPutString = JsonConvert.SerializeObject(GanttModel, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                return outPutString;
            }
            catch (Exception e)
            {
                return "Invalid Project";
            }
        }

        private static List<BusinessObject> ReadGanttTasks(Project project)
        {
            DateFormat = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            List<BusinessObject> GanttTasks = new List<BusinessObject>();
            Dictionary<Syncfusion.ProjIO.Task, List<Syncfusion.ProjIO.Resource>> ResourceAssignment = null;
            // Importing the xml
            List<Syncfusion.ProjIO.Task> porjectTasks = project.RootTask.Children;
            // Getting the resource assignment from the imported xml
            if (project.Assignments != null && project.Assignments.Count > 0 && project.Resources != null && project.Resources.Count > 0)
                ResourceAssignment = GenerateResourceAssignment(project.Assignments);
            GanttTasks = CreateGanttTasks(porjectTasks, ResourceAssignment, project);
            return GanttTasks;
        }

        private static List<BusinessObject> CreateGanttTasks(List<Syncfusion.ProjIO.Task> taskList, Dictionary<Syncfusion.ProjIO.Task, List<Syncfusion.ProjIO.Resource>> resourcesAssignment, Project project)
        {
            List<BusinessObject> ganttTaskCollection = new List<BusinessObject>();
            RenderBaseline = false;
            // Iterating through the task to create corresponding Gantt tasks
            foreach (Syncfusion.ProjIO.Task task in taskList)
            {
                if (task == null)
                    continue;
                BusinessObject ganttTask = new BusinessObject();
                ganttTask.TaskId = task.ID;
                ganttTask.TaskName = task.Name;
                ganttTask.TaskStartDate = task.Start.ToShortDateString();
                ganttTask.IsManual = task.IsManual;
                ganttTask.Notes = task.Notes;
                if (task.IsMilestone)
                    ganttTask.TaskEndDate = ganttTask.TaskStartDate;
                else
                    ganttTask.TaskEndDate = task.Finish.ToShortDateString();
                ganttTask.Duration = (int)task.Duration.TotalMinutes / project.MinutesPerDay;
                ganttTask.PercentDone = task.PercentComplete;
                ganttTask.Predecessor = GetPredecessor(task.PredecessorLink, project.MinutesPerDay);
                if (task.Baseline != null)
                {
                    RenderBaseline = true;
                    ganttTask.BaselineStartDate = task.Baseline[0].Start.ToShortDateString();
                    if (task.IsMilestone)
                        ganttTask.BaselineEndDate = ganttTask.BaselineStartDate;
                    else
                        ganttTask.BaselineEndDate = task.Baseline[0].Finish.ToShortDateString();
                }
                List<Syncfusion.ProjIO.Resource> resurceList = new List<Syncfusion.ProjIO.Resource>();
                // Creating resource from assignments
                if (resourcesAssignment != null && resourcesAssignment.TryGetValue(task, out resurceList))
                {
                    foreach (Syncfusion.ProjIO.Resource res in resurceList)
                    {
                        if (res == null)
                            continue;
                        if (ganttTask.ResourceIdCollection == null)
                            ganttTask.ResourceIdCollection = new List<int>();

                        ganttTask.ResourceIdCollection.Add(res.ID);
                    }
                }
                ganttTaskCollection.Add(ganttTask);
                // creating hierarchy
                if (task.Children != null && task.Children.Count > 0)
                {
                    var result = CreateGanttTasks(task.Children, resourcesAssignment, project);
                    foreach (BusinessObject child in result)
                    {
                        if (ganttTask.Children == null)
                            ganttTask.Children = new List<BusinessObject>();
                        ganttTask.Children.Add(child);
                    }
                }
            }
            return ganttTaskCollection;
        }

        private static string GetPredecessor(List<TaskLink> predecessorLink, int minPerDay)
        {
            string GanttPredecessors = "";

            // Creating predecessors from the predecessor link
            foreach (TaskLink tlink in predecessorLink)
            {
                string _id = "", _type = "", offset="";
                int lag = tlink.LinkLag;
                DelayFormat linkLagFormat = tlink.LagFormat;
                _id = tlink.Predecessor.ID.ToString();

                // creating predecessor link type
                switch (tlink.Type)
                {
                    case TaskLinkType.FinishToFinish:
                        _type = "FF";
                        break;
                    case TaskLinkType.FinishToStart:
                        _type = "FS";
                        break;
                    case TaskLinkType.StartToFinish:
                        _type = "SF";
                        break;
                    case TaskLinkType.StartToStart:
                        _type = "SS";
                        break;
                }
                if (linkLagFormat == DelayFormat.Minutes & lag > 0)
                {
                    //offset = (lag / 10).ToString();
                    //offset += "m";
                }
                else if (linkLagFormat == DelayFormat.Hours & lag > 0)
                {
                    //offset = (lag / (10 * 60)).ToString();
                    //offset += "h";
                }
                else if (linkLagFormat == DelayFormat.Days & lag != 0)
                {
                    if (lag < 0)
                    {
                        offset = (lag / (minPerDay * 10)).ToString();
                        offset = "-" + offset;
                    }
                    else
                    {
                        offset = (lag / (minPerDay * 10)).ToString();
                        offset = "+" + offset;
                    }
                }
                if (GanttPredecessors.Length > 0)
                    GanttPredecessors = GanttPredecessors + "," + _id + _type + offset;
                else
                    GanttPredecessors = _id + _type +  offset;
            }
            return GanttPredecessors;

        }

        private static List<GanttResources> GetGanttResourceCollection(Project project)
        {
            List<Syncfusion.ProjIO.Resource> resources = project.Resources;
            List<GanttResources> ResourceCollection = new List<GanttResources>();
            foreach (Syncfusion.ProjIO.Resource resource in resources)
            {
                GanttResources _ganttResource = new GanttResources();
                if (resource.ID != 0)
                {
                    _ganttResource.ResourceId = resource.ID;
                    if (resource.Name != null)
                        _ganttResource.ResourceName = resource.Name;
                    ResourceCollection.Add(_ganttResource);
                }
            }
            return ResourceCollection;
        }

        /* Get Holidays from Project*/
        private static List<GanttServiceHoliday> GetGanttHolidays(Project project)
        {
            List<Syncfusion.ProjIO.CalendarException> holidays = project.Calendar.Exceptions;
            List<GanttServiceHoliday> GanttHolidays = new List<GanttServiceHoliday>();
            foreach (Syncfusion.ProjIO.CalendarException holiday in holidays)
            {
                List<GanttServiceHoliday> _holiday = GetHolidays(holiday);
                GanttHolidays.AddRange(_holiday);
            }
            return GanttHolidays;
        }

        private static List<GanttServiceHoliday> GetHolidays(Syncfusion.ProjIO.CalendarException exceptionDates)
        {
            List<GanttServiceHoliday> holidays = new List<GanttServiceHoliday>();
            var dates = new List<DateTime>();
            for (var dt = exceptionDates.TimePeriod.FromDate; dt <= exceptionDates.TimePeriod.ToDate; dt = dt.AddDays(1))
            {
                dates.Add(dt);
            }
            foreach( DateTime day in dates)
            {
                GanttServiceHoliday tempHoliday = new GanttServiceHoliday();
                tempHoliday.day = day.ToShortDateString();
                tempHoliday.label = exceptionDates.Name;
                tempHoliday.background = "rgb(255, 245, 245)";
                holidays.Add(tempHoliday);
            }
            return holidays;
        }

        private static Dictionary<Syncfusion.ProjIO.Task, List<Syncfusion.ProjIO.Resource>> GenerateResourceAssignment(List<Assignment> assignments)
        {
            Dictionary<Syncfusion.ProjIO.Task, List<Syncfusion.ProjIO.Resource>> assignedResources = new Dictionary<Syncfusion.ProjIO.Task, List<Syncfusion.ProjIO.Resource>>();

            // To get the assignment based on the task, this will help us to create resoure on iterating the task itself
            foreach (Assignment assign in assignments)
            {
                if (!assignedResources.Keys.Contains(assign.Task))
                {
                    assignedResources.Add(assign.Task, new List<Syncfusion.ProjIO.Resource>());
                    assignedResources[assign.Task].Add(assign.Resource);
                }
                else
                {
                    if (!assignedResources[assign.Task].Contains(assign.Resource))
                    {
                        assignedResources[assign.Task].Add(assign.Resource);
                    }
                }
            }
            return assignedResources;
        }

        private static Boolean CheckIncludeWeekEnd(Project project)
        {
            Boolean includeWeekend = false;
            var weekDays = project.Calendar.WeekDays;
            var sunDay = weekDays.Find(x => x.DayType == DayType.Sunday);
            var satDay = weekDays.Find(x => x.DayType == DayType.Saturday);

            if ((sunDay != null && sunDay.WorkingTimes != null && sunDay.WorkingTimes.Items.Count > 0) || (satDay != null && satDay.WorkingTimes != null && satDay.WorkingTimes.Items.Count > 0))
                includeWeekend = true;
            return includeWeekend;
        }

        #endregion
    }
    #region Create the BusinessObject
    [Serializable]
    public class BusinessObject
    {
        private string _startDate = "";
        private string _endDate = "";
        private DateTime? _startDateObj = null;
        private DateTime? _endDateObj = null;
        private string _baseLineStartDate = "";
        private string _baseLineEndDate = "";
        private int _id = -1;
        private string _name = "";
        private double _duration = -1;
        private int _percentDone = 0;
        private List<int> _resourceId = null;
        private string _predecessor = "";
        private List<BusinessObject> _children = null;
        private bool _isManual = false;
        private string _notes = "";

        [DefaultValue("")]
        public string TaskStartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }
        [DefaultValue("")]
        public string TaskEndDate
        {
            get { return _endDate; }
            set { _endDate = value; }
        }
        [DefaultValue("")]
        public string BaselineStartDate
        {
            get { return _baseLineStartDate; }
            set { _baseLineStartDate = value; }
        }
        [DefaultValue("")]
        public string BaselineEndDate
        {
            get { return _baseLineEndDate; }
            set { _baseLineEndDate = value; }
        }
        [DefaultValue(-1)]
        public int TaskId
        {
            get { return _id; }
            set { _id = value; }
        }
        [DefaultValue("")]
        public string TaskName
        {
            get { return _name; }
            set { _name = value; }
        }
        [DefaultValue(-1)]
        public double Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }
        [DefaultValue(0)]
        public int PercentDone
        {
            get { return _percentDone; }
            set { _percentDone = value; }
        }
        [DefaultValue(null)]
        public List<int> ResourceIdCollection
        {
            get { return _resourceId; }
            set { _resourceId = value; }
        }
        [DefaultValue(null)]
        public List<BusinessObject> Children
        {
            get { return _children; }
            set { _children = value; }
        }
        [DefaultValue("")]
        public string Predecessor
        {
            get { return _predecessor; }
            set { _predecessor = value; }
        }

        [DefaultValue(null)]
        public DateTime? StartDateObj
        {
            get { return _startDateObj; }
            set { _startDateObj = value; }
        }

        [DefaultValue(null)]
        public DateTime? EndDateObj
        {
            get { return _endDateObj; }
            set { _endDateObj = value; }
        }
        [DefaultValue(false)]
        public bool IsManual
        {
            get { return _isManual; }
            set { _isManual = value; }
        }
        [DefaultValue(false)]
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; }
        }
    }
    #endregion

    #region Resources
    [Serializable]
    public class GanttResources
    {
        private int _id = -1;
        private string _name = "";
        private string _resourceEmail = "";
        [DefaultValue(-1)]
        public int ResourceId
        {
            get { return _id; }
            set { _id = value; }
        }
        [DefaultValue("")]
        public string ResourceName
        {
            get { return _name; }
            set { _name = value; }
        }
        [DefaultValue("")]
        public string ResourceEmail
        {
            get { return _resourceEmail; }
            set { _resourceEmail = value; }
        }
    }
    #endregion

    #region Resources
    [Serializable]
    public class GanttServiceHoliday
    {
        private string _day = "";
        private string _label = "";
        private string _background= "";

        [DefaultValue("")]
        public string day
        {
            get { return _day; }
            set { _day= value; }
        }
        [DefaultValue("")]
        public string label
        {
            get { return _label; }
            set { _label = value; }
        }
        [DefaultValue("")]
        public string background
        {
            get { return _background; }
            set { _background = value; }
        }
    }
    #endregion
}
