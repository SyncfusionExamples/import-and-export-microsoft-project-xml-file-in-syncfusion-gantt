# Syncfusion EJ2 Gantt Microsoft Project XML Import/Export Sample

A demo repository for importing and exporting Microsoft Project XML files with Syncfusion EJ2 Gantt and a .NET 6 Web API.

## Overview
Browser sample and ASP.NET Core service convert Project XML to Gantt JSON and export Gantt data to Microsoft Project XML.

## Features

- Import Microsoft Project XML (`.xml`) into Syncfusion EJ2 Gantt
- Export Gantt data to Microsoft Project XML
- Supports hierarchy, dependencies, baselines, resources, and holidays

## Requirements

- .NET 6 SDK
- Visual Studio 2022 or later
- Syncfusion packages: `Syncfusion.EJ2.AspNet.Core`, `Syncfusion.ProjIO.Base`
- Internet access for browser CDN scripts

## Setup

1. Open `GanttXMLService/GanttXMLService.sln`
2. Restore packages
3. Run the Web API project
4. Confirm the API at `https://localhost:7281`

## Usage

Open `Gantt_Import_Sample/GanttImport.html`

- `Import` to upload XML
- `Export` to download XML

## API

- `POST api/XMLImport/Import` — upload XML or URL, returns Gantt JSON
- `POST api/XMLImport/ExportToXML` — accepts `GanttData`, returns XML

## Notes

Update the sample page URL if the API port changes. Syncfusion licensing may apply.
