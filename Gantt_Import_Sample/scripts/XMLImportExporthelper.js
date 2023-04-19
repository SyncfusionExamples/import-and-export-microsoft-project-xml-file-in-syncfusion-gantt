//Append import options to formdata
function getFormData(options) {
    if (window.FormData && Object.prototype.toString.call(options) === "[object Object]") {
        var formData = new FormData();
        for (prop in options)
            formData.append(prop, options[prop]);
        return formData;
    }
    else
        return options;
}
/*Import method for XML formatted file*/
function xmlImport(options) {
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {     
        if (xmlHttp.readyState == 4) {
            if (xmlHttp.status == 200) {
                success(this);
            }
            else {
                error(this)
            }
        }
    }
    xmlHttp.open("post", "https://localhost:7281/api/XmlImport/Import", true);
    xmlHttp.send(getFormData(options));
}
function success(e) {
    importFromJSON(e.response);
}
function error(err) {
    ej.popups.hideSpinner(document.getElementById('Default'));
}
/*Import data from server response text*/
function importFromJSON(val) {
    if (val == "Invalid Project" || val == "InvalidUrl") {
        alert(val);
        ej.popups.hideSpinner(document.getElementById('Default'));
    } else {
        var mapping = ganttChart.columnMapping, data;

        if (mapping.id)
            val = val.replace(/\"TaskId\":/g, "\"" + mapping.id + "\":");
        else
            mapping.id = "TaskId";

        if (mapping.name)
            val = val.replace(/\"TaskName\":/g, "\"" + mapping.name + "\":");
        else
            mapping.name = "TaskName";

        if (mapping.startDate)
            val = val.replace(/\"TaskStartDate\":/g, "\"" + mapping.startDate + "\":");
        else
            mapping.startDate = "TaskStartDate";

        if (mapping.endDate)
            val = val.replace(/\"TaskEndDate\":/g, "\"" + mapping.endDate + "\":");
        else
            mapping.endDate = "TaskEndDate";

        if (mapping.progress)
            val = val.replace(/\"PercentDone\":/g, "\"" + mapping.progress + "\":");
        else
            mapping.name = "PercentDone";

        if (mapping.duration)
            val = val.replace(/\"Duration\":/g, "\"" + mapping.duration + "\":");
        else
            mapping.duration = "Duration";

        if (ganttChart.taskFields.child)
            val = val.replace(/\"Children\":/g, "\"" + ganttChart.taskFields.child + "\":");
        else
            ganttChart.taskFields.child = "Children";

        if (mapping.resourceInfo)
            val = val.replace(/\"ResourceIdCollection\":/g, "\"" + mapping.resourceInfo + "\":");
        else
            mapping.resourceInfo = "ResourceIdCollection";

        if (mapping.dependency)
            val = val.replace(/\"Predecessor\":/g, "\"" + mapping.dependency + "\":");
        else
            mapping.dependency = "Predecessor";

        if (ganttChart.resourceNameMapping)
            val = val.replace(/\"ResourceName\":/g, "\"" + ganttChart.resourceNameMapping + "\":");
        else
            ganttChart.resourceNameMapping = "ResourceName";

        if (ganttChart.resourceIDMapping)
            val = val.replace(/\"ResourceId\":/g, "\"" + ganttChart.resourceIDMapping + "\":");
        else
            ganttChart.resourceIDMapping = "ResourceId";

        if (mapping.baselineEndDate)
            val = val.replace(/\"BaselineEndDate\":/g, "\"" + mapping.baselineEndDate + "\":");
        else
            mapping.baselineEndDate = "BaselineEndDate";

        if (mapping.baselineStartDate)
            val = val.replace(/\"BaselineStartDate\":/g, "\"" + mapping.baselineStartDate + "\":");
        else
            mapping.baselineStartDate = "BaselineStartDate";

        if (mapping.notes)
            val = val.replace(/\"Notes\":/g, "\"" + mapping.notes + "\":");
        else
            mapping.notes = "Notes";

        data = JSON.parse(val);
        ganttChart.dataSource = data["dataSource"];
        ganttChart.resources = data["resources"];
        ganttChart.holidays = importHolidays(data["holidays"]);
        ej.popups.hideSpinner(document.getElementById('Default'));
    }
}
/*Export as XML action for Gantt*/
function xmlExport(action) {
    var checkRecord = constructExportDataSource(),
        ganttObjFinal = {}, form, input;
    for (var data in ganttChart) {
        try {
            if (JSON.stringify(ganttChart[data])) {
                ganttObjFinal[data] = ganttChart[data];
            }
        }
        catch (e) {
            continue;
        }
    }
    delete ganttObjFinal.dayWorkingTime;
    delete ganttObjFinal.editSettings;
    delete ganttObjFinal.dayWorkingTime;
    delete ganttObjFinal.editSettings;
    delete ganttObjFinal.filterSettings;
    delete ganttObjFinal.searchSettings;
    delete ganttObjFinal.selectionSettings;
    delete ganttObjFinal.sortSettings;
    delete ganttObjFinal.labelSettings;
    delete ganttObjFinal.toolbarSettings;
    delete ganttObjFinal.splitterSettings;
    delete ganttObjFinal.tooltipSettings;
    delete ganttObjFinal.taskFields;
    delete ganttObjFinal.timelineSettings;
    delete ganttObjFinal.enableRtl;
    delete ganttObjFinal.autoFocusTasks;
    delete ganttObjFinal.collapseAllParentTasks;
    delete ganttObjFinal.allowKeyboard;
    delete ganttObjFinal.taskbarHeight;
    ganttObjFinal.dataSource = checkRecord;
    ganttObjFinal.holidays = getHolidays(holidays);
    ganttObjFinal.scheduleStartDate = ganttChart.projectStartDate;
    ganttObjFinal.scheduleEndDate = ganttChart.projectEndDate;
    var val = JSON.stringify(ganttObjFinal);

    if (ganttObjFinal.columnMapping.id)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.id + "\":", 'g'), "\"TaskId\":");

    if (ganttObjFinal.columnMapping.name)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.name + "\":", 'g'), "\"TaskName\":");

    if (ganttObjFinal.columnMapping.endDate)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.endDate + "\":", 'g'), "\"StartDateObj\":");

    if (ganttObjFinal.columnMapping.startDate)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.startDate + "\":", 'g'), "\"EndDateObj\":");

    if (ganttObjFinal.columnMapping.progress)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.progress + "\":", 'g'), "\"PercentDone\":");

    if (ganttObjFinal.columnMapping.duration)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.duration + "\":", 'g'), "\"Duration\":");

    if (ganttObjFinal.columnMapping.child)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.child + "\":", 'g'), "\"Children\":");

    if (ganttObjFinal.columnMapping.resourceInfo)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.resourceInfo + "\":", 'g'), "\"ResourceIdCollection\":");

    if (ganttObjFinal.columnMapping.dependency)
        val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.dependency + "\":", 'g'), "\"Predecessor\":");

    if (ganttObjFinal.resourceNameMapping)
        val = val.replace(new RegExp("\"" + ganttObjFinal.resourceNameMapping + "\":", 'g'), "\"ResourceName\":");

    if (ganttObjFinal.resourceIDMapping) {
        val = val.replace(new RegExp("\"" + ganttObjFinal.resourceIDMapping + "\":", 'g'), "\"ResourceId\":");
        val = val.replace(new RegExp("\"resourceEmail\":", 'g'), "\"ResourceEmail\":");
    }
    if (ganttObjFinal.renderBaseline) {
        if (ganttObjFinal.columnMapping.baselineEndDate)
            val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.baselineEndDateMapping + "\":", 'g'), "\"BaselineEndDate\":");

        if (ganttObjFinal.columnMapping.baselineStartDate)
            val = val.replace(new RegExp("\"" + ganttObjFinal.columnMapping.baselineStartDateMapping + "\":", 'g'), "\"BaselineStartDate\":");
    }
    var form = document.createElement("FORM");
    form.setAttribute("id", "form_id");
    form.setAttribute("action", action);
    form.setAttribute("method", "post");
    document.body.appendChild(form);
    var input = document.createElement("INPUT");
    input.setAttribute("name", "GanttData");
    input.setAttribute("type", "hidden");
    input.setAttribute("id", "example");
    document.getElementById("form_id").appendChild(input);
    document.getElementById("example").value = val;
    document.getElementById("form_id").submit();
}
function getDates(startDate, stopDate) {
    var dateArray = new Array();
    var currentDate = new Date(startDate);
    while (currentDate <= new Date(stopDate)) {
        dateArray.push(new Date(currentDate));
        currentDate.setDate(currentDate.getDate() + 1);
    }
    return dateArray;
}
function importHolidays(holiday) {
    var holiExport = [];
    for (var i = 0; i < holiday.length; i++) {
        curHoliday = {};
        curHoliday.from = holiday[i].day;
        curHoliday.to = holiday[i].day;
        curHoliday.label = holiday[i].label;
        holiExport.push(curHoliday);
    }
    return holiExport;
}
function getHolidays(holiday) {
    var holiExport = [];
    for (var i = 0; i < holiday.length; i++) {
        var dateRange = getDates(holiday[i].from, holiday[i].to);
        for (var j = 0; j < dateRange.length; j++) {
            curHoliday = {};
            curHoliday.Day = dateRange[j];
            curHoliday.Background = "#e82869";
            curHoliday.Label = holiday[i].label;
            curHoliday.From = holiday[i].from;
            curHoliday.To = holiday[i].to;
            holiExport.push(curHoliday);
        }
    }
    return holiExport;
}
//get all parent items
function getAllParentRecord(records) {
    var resultRecord = records.filter(function (record) {
        if (record.hasChildRecords)
            return record;
    });
    return resultRecord;
}
/*Construct data source to export action*/
function constructExportDataSource() {
    var controlObj = ganttChart,
        parentRecords = getAllParentRecord(controlObj.flatData),
        currentRecord = {},
        dataSource = [];
    for (var count = 0; count < parentRecords.length; count++) {
        currentRecord = getExportRecord(parentRecords[count]);
        dataSource.push(currentRecord);
    }
    return dataSource;
}

/*Construct child record for export operation*/
function getExportRecord(record) {
    var curRecord = {};
    curRecord.TaskId = record[ganttChart.columnMapping.id];
    curRecord.TaskName = record[ganttChart.columnMapping.name];
    curRecord.StartDateObj = new Date(record[ganttChart.columnMapping.startDate]);
    curRecord.EndDateObj = new Date(record[ganttChart.columnMapping.endDate]);
    curRecord.PercentDone = record[ganttChart.columnMapping.progress];
    curRecord.Duration = record[ganttChart.columnMapping.duration];
    curRecord.Predecessor = record[ganttChart.columnMapping.dependency];
    if (!isNaN(Date.parse(record[ganttChart.columnMapping.baselineStartDate])))
        curRecord.BaselineStartDate = record[ganttChart.columnMapping.baselineStartDate];
    if (!isNaN(Date.parse(record[ganttChart.columnMapping.baselineEndDate])))
        curRecord.BaselineEndDate = record[ganttChart.columnMapping.baselineEndDate];
    curRecord.Predecessor = record[ganttChart.columnMapping.dependency];
    curRecord.ResourceIdCollection = record.taskData[ganttChart.columnMapping.resourceInfo];
    curRecord.Notes = record[ganttChart.columnMapping.notes];
    if (record.hasChildRecords) {
        curRecord.Children = [];
        for (var rec = 0; rec < record.childRecords.length; rec++) {
            curRecord.Children.push(getExportRecord(record.childRecords[rec]));
        }
    }
    return curRecord;
}