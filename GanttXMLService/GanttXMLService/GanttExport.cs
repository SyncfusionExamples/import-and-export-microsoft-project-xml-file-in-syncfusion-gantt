using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !(SyncfusionDNX5_0)
using System.Web;
using Syncfusion.EJ2.Gantt;
#endif
using Syncfusion.ProjIO;

namespace GanttXMLService
{
    public class GanttExport : Project
    {
        #region Fields
        private string _fileName = "";

        public IEnumerable TaskCollection;
        public List<GanttResources> ResourceCollection;
        public Project CurrentProject;
        public Gantt GanttObject;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the file.	
        /// </summary>
        /// <value>The name of the file.</value>
        /// <remarks></remarks>
        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
            }
        }
        #endregion

        #region Export To XML
        public void Export(Gantt ganttModel, HttpResponse response)
        {
            GanttObject = ganttModel;
            ExecuteResult(GanttObject);
            SaveServer(FileName, response);
        }


        private void SaveServer(String filename, HttpResponse response)
        {
            // Exporting the current CurrentProject to xml
            if (filename == null)
            {
                throw new ArgumentNullException("fileName");
            }
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            response.Clear();
                string ExpireDate = DateTime.UtcNow.AddMinutes(0).ToString("ddd, dd MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            response.Headers.Add("Expires", ExpireDate + " GMT");

            //response.Buffer = true;
            string disposition = "content-disposition";
            response.Headers.Add(disposition, "attachment; filename=\"" + filename + ".xml\"");
          ;
            CurrentProject.Save(response.Body);
            response.CompleteAsync();
        }

        private void ExecuteResult(Gantt GanttModel)
        {
            CurrentProject = new Project();
            string format = GanttModel.DateFormat;
            if (format == null)
            {
                format = "MM/dd/yyyy";
            }
            CurrentProject.StartDate = DateTime.ParseExact(GanttModel.ProjectStartDate, format, CultureInfo.InvariantCulture);
            CurrentProject.StartDate = CurrentProject.StartDate.Add(CurrentProject.DefaultStartTime);
            CurrentProject.FinishDate = DateTime.ParseExact(GanttModel.ProjectEndDate, format, CultureInfo.InvariantCulture);
            CurrentProject.FinishDate = CurrentProject.FinishDate.Add(CurrentProject.DefaultFinishTime);
            TaskCollection = (IEnumerable)GanttModel.DataSource;
            ResourceCollection = (List<GanttResources>)GanttModel.Resources;
            UpdateTaskCollection();           
            UpdateIncludeWeekend(GanttModel);
        }

        private void UpdateTaskCollection()
        {
            Dictionary<BusinessObject, Syncfusion.ProjIO.Task> likendTasks = new Dictionary<BusinessObject, Syncfusion.ProjIO.Task>();
            Dictionary<BusinessObject, Syncfusion.ProjIO.Task> linearTasks = new Dictionary<BusinessObject, Syncfusion.ProjIO.Task>();
            List<Assignment> assignedTasks = new List<Assignment>();
            List<Assignment> assignedMilestoneTasks = new List<Assignment>();
            CurrentProject.RootTask.Children = CreateProjectTasks(TaskCollection, likendTasks, assignedTasks, linearTasks, assignedMilestoneTasks);
            CurrentProject.CalculateTaskIDs();

            // Creating predecessors
            if (likendTasks.Count > 0)
                UpdateLinkedTasks(likendTasks, linearTasks);

            // Creating resource assignment
            if (assignedTasks.Count > 0)
            {
                CurrentProject.Resources = assignedTasks.Where(x => x.Task.IsMilestone == false).ToList().Select((a) => a.Resource).ToList();
                //if (assignedMilestoneTasks.Count > 0)
                //    assignedTasks.AddRange(assignedMilestoneTasks);
                CurrentProject.Assignments.AddRange(assignedTasks);
            }
            // Calculating UIDs of tasks and resources
            CurrentProject.CalculateResourceIDs();
        }

        private List<Syncfusion.ProjIO.Task> CreateProjectTasks(IEnumerable taskCollection, Dictionary<BusinessObject, Syncfusion.ProjIO.Task> likendTasks, List<Assignment> assignedTasks, Dictionary<BusinessObject, Syncfusion.ProjIO.Task> linearTasks,List<Assignment> assignedMilestoneTasks)
        {
            List<Syncfusion.ProjIO.Task> projectTaskList = new List<Syncfusion.ProjIO.Task>();

            foreach (BusinessObject ganttTask in taskCollection)
            {
                if (ganttTask == null)
                    continue;

                Syncfusion.ProjIO.Task projTask = new Syncfusion.ProjIO.Task();

                projTask.ID = ganttTask.TaskId;
                projTask.Name = ganttTask.TaskName;

                projTask.Start = (DateTime)ganttTask.StartDateObj;
               // projTask.Start = projTask.Start.Add(CurrentProject.DefaultStartTime);
                
                projTask.Finish =(DateTime)ganttTask.EndDateObj;
              //  projTask.Finish = projTask.Finish.Add(CurrentProject.DefaultFinishTime);
                
                projTask.PercentComplete = (int)ganttTask.PercentDone;
                projTask.PercentWorkComplete = (int)ganttTask.PercentDone;
                double min = (double)(ganttTask.Duration * projTask.PercentComplete) / 100;
                min = min * CurrentProject.MinutesPerDay;
                projTask.ActualWork = new TimeSpan(0, (int)min, 0);
                int reminingWork = (int)(ganttTask.Duration * CurrentProject.MinutesPerDay - (int)min);
                projTask.RemainingWork = new TimeSpan(0, reminingWork, 0);
                projTask.CalendarUID = -1;
                projTask.ConstraintType = TaskConstraintType.AsSoonAsPossible;
                projTask.IsManual = ganttTask.IsManual;
                if (ganttTask.Notes != "null" && ganttTask.Notes != "")
                    projTask.Notes = ganttTask.Notes;
               // projTask.ConstraintDate = (DateTime)ganttTask.StartDateObj;
                if (ganttTask.Duration == 0)
                {
                    projTask.Start = projTask.Finish;
                    projTask.Duration = new TimeSpan(0, 0, 0, 0);
                    projTask.IsMilestone = true;
                }
                if (GanttObject.RenderBaseline)
                {
                    if (ganttTask.BaselineStartDate.Length > 0 && ganttTask.BaselineEndDate.Length > 0)
                    {
                        projTask.Baseline = new TaskBaseline[1];
                        projTask.Baseline[0] = new TaskBaseline()
                        {
                            Start = DateTime.Parse(ganttTask.BaselineStartDate).Add(CurrentProject.DefaultStartTime),
                            Finish = DateTime.Parse(ganttTask.BaselineEndDate).Add(CurrentProject.DefaultFinishTime)
                        };
                    }
                }
                // Adding predecessor task
                if (ganttTask.Predecessor != null && ganttTask.Predecessor.Length > 0)
                    likendTasks.Add(ganttTask, projTask);

                // Creating hierarchy
                if (ganttTask.Children != null && ganttTask.Children.Count > 0)
                    projTask.Children = CreateProjectTasks(ganttTask.Children, likendTasks, assignedTasks, linearTasks, assignedMilestoneTasks);

                // Creating resource assignment
                if (ganttTask.ResourceIdCollection != null && ganttTask.ResourceIdCollection.Count > 0)
                {
                    foreach (int resourceId in ganttTask.ResourceIdCollection)
                    {
                        Assignment assignment = new Assignment();
                        assignment.Task = projTask;
                        GanttResources resource = ResourceCollection.Find(x => x.ResourceId == resourceId);
                        if (resource != null)
                        {
                            assignment.Resource = new Syncfusion.ProjIO.Resource { Name = resource.ResourceName, ID = resource.ResourceId, EmailAddress = resource.ResourceEmail, Type = ResourceType.Work, Start = CurrentProject.StartDate, Finish = CurrentProject.FinishDate };
                            assignedTasks.Add(assignment);
                        }
                    }
                }
                else if (projTask.IsMilestone)
                {
                    Assignment assignment = new Assignment();
                    assignment.Task = projTask;
                    assignment.Resource = new Syncfusion.ProjIO.Resource { ID = -65535, UID = -65535 };
                    //assignment.ResourceUID = -65535;
                    //assignment.ResourceUIDSerialized = "-65535";
                    assignedMilestoneTasks.Add(assignment);
                }
                linearTasks.Add(ganttTask, projTask);
                // Adding the created task to CurrentProject
                projectTaskList.Add(projTask);
            }

            return projectTaskList;
        }

        /* Updated predecessor values in Tasks */
        private void UpdateLinkedTasks(Dictionary<BusinessObject, Syncfusion.ProjIO.Task> likendTasks, Dictionary<BusinessObject, Syncfusion.ProjIO.Task> linearTasks)
        {
            // Creating predecessor Link from gantt predecessors
            foreach (BusinessObject task in likendTasks.Keys)
            {
                List<TaskLink> Predecessor = new List<TaskLink>();
                String[] PreColl;
                PreColl = task.Predecessor.Split(',');
                for (int count = 0; count < PreColl.Length; count++)
                {
                    string temp = PreColl[count];
                    string preVal = "", type;
                    int offset = 0, id;
                    if (temp.IndexOf('+') > -1)
                    {
                        preVal = temp.Substring(0, temp.IndexOf('+'));
                        offset = int.Parse(Regex.Match(temp.Substring(temp.IndexOf('+')), @"\d+").Value);
                    }
                    else if (temp.IndexOf('-') > -1)
                    {
                        preVal = temp.Substring(0, temp.IndexOf('-'));
                        offset = int.Parse(Regex.Match(temp.Substring(temp.IndexOf('-')), @"\d+").Value);
                        offset *= -1;
                    }
                    else
                    {
                        preVal = temp;
                        offset = 0;
                    }

                    id = int.Parse(Regex.Match(preVal, @"\d+").Value);
                    Regex rgx = new Regex("[^a-zA-Z -]");
                    type = rgx.Replace(preVal, "");

                    var result = linearTasks.Values.Where((t) => t.ID == id);
                    if (result == null || result.Count() <= 0 || result.First() == null)
                        continue;
                    Syncfusion.ProjIO.Task projTask = result.First();
                    TaskLinkType tLinkType = TaskLinkType.FinishToStart;
                    switch (type)
                    {
                        case "SS":
                            tLinkType = TaskLinkType.StartToStart;
                            break;
                        case "SF":
                            tLinkType = TaskLinkType.StartToFinish;
                            break;
                        case "FS":
                            tLinkType = TaskLinkType.FinishToStart;
                            break;
                        case "FF":
                            tLinkType = TaskLinkType.FinishToFinish;
                            break;
                    }
                    TaskLink taskLink = new TaskLink(projTask, linearTasks[task], tLinkType);
                    taskLink.LinkLag = offset * CurrentProject.MinutesPerDay * 10;
                }
            }
        }

        /* Update Weekdays value in calendar*/
        private void UpdateIncludeWeekend(Gantt model)
        {
            Syncfusion.ProjIO.Calendar calendar= new Syncfusion.ProjIO.Calendar();
            calendar.Name = "Standard";
            calendar.IsBaseCalendar = true;
            List<WeekDay> weekdays = new List<WeekDay>();
            for (int count = 1; count <= 7; count++)
            {
                WeekDay weekday = new WeekDay();
                switch (count)
                {
                    case 1: weekday.DayType = DayType.Monday;
                        break;
                    case 2: weekday.DayType = DayType.Tuesday;
                        break;
                    case 3: weekday.DayType = DayType.Wednesday;
                        break;
                    case 4: weekday.DayType = DayType.Thursday;
                        break;
                    case 5: weekday.DayType = DayType.Friday;
                        break;
                    case 6: weekday.DayType = DayType.Sunday;
                        break;
                    case 7: weekday.DayType = DayType.Saturday;
                        break;
                }
                if (model.IncludeWeekend || count < 6)
                {
                    weekday.DayWorking = true;
                    WorkingTimes w_times = new WorkingTimes();
                    w_times.Items = new List<WorkingTime>();
                    w_times.Items.Add(new WorkingTime() { FromTime = new TimeSpan(8, 0, 0), ToTime = new TimeSpan(12, 0, 0) });
                    w_times.Items.Add(new WorkingTime() { FromTime = new TimeSpan(13, 0, 0), ToTime = new TimeSpan(17, 0, 0) });
                    weekday.WorkingTimes = w_times;
                }
                else
                {
                    weekday.DayWorking = false;
                    WorkingTimes w_times = new WorkingTimes();
                    w_times.Items = new List<WorkingTime>();
                    w_times.Items.Add(null);
                    w_times.Items.Add(null);
                    weekday.WorkingTimes = w_times;
                }
                weekdays.Add(weekday);
            }
            calendar.WeekDays = weekdays;
            if (model.Holidays.Count > 0)
                UpdateHolidays(calendar, model.Holidays);
            CurrentProject.Calendar = calendar;
            CurrentProject.Calendars.Add(calendar);
        }       

        /* Add Exception collection in default calendar*/
        private void UpdateHolidays(Syncfusion.ProjIO.Calendar calendar, List<GanttHoliday> holidays)
        {
            CultureInfo culture = new CultureInfo("en-US");
            foreach (GanttHoliday holiday in holidays)
            {
                CalendarException exception = new CalendarException();
                exception.Occurrences = 1;
                exception.Name = holiday.Label;
                exception.TimePeriod = new TimePeriod();
                exception.TimePeriod.FromDateSpecified = true;
                exception.TimePeriod.ToDateSpecified = true;
                exception.TimePeriod.FromDate = Convert.ToDateTime(holiday.From, culture);
                exception.TimePeriod.ToDate = Convert.ToDateTime(holiday.To, culture);
                TimeSpan ts = new TimeSpan(23, 59, 0);
                exception.TimePeriod.ToDate = exception.TimePeriod.ToDate.Add(ts);
                exception.IsDayWorkingString = "0";
                exception.Type = ExceptionType.Daily;
                exception.TypeSpecified = true;

                WeekDay weekday = new WeekDay();
                weekday.DayWorking = false;                
                weekday.DayType = DayType.Exception;
                weekday.TimePeriod = exception.TimePeriod;
                calendar.WeekDays.Add(weekday);
                calendar.Exceptions.Add(exception);
            }
        }

        #endregion
    }
}
