# Run guide - Trade Licence portal with Common Application Form (CAF)

## 1. Database (SQL Server, database NewEODB)
Your NewEODB already has all dbo.caf_* tables, including caf_doc_table.
Run this once in SSMS (safe to run again):

    Database/CAF_SchemaExtras.sql

It gives the five sub-tables their Id key and adds one-row-per-user safety
constraints on the four main CAF tables. (The app also adds the Id keys by
itself on first use if the login in the connection string may ALTER tables.)

## 2. Connection string
Edit TradeLicence/appsettings.json -> ConnectionStrings:DefaultConnection
so it points at your SQL Server.

## 3. Run
Needs the .NET SDK that matches TradeLicence/TradeLicence.csproj.

    cd TradeLicence
    dotnet run

or open TradeLicence.slnx in Visual Studio and press F5.

## 4. Use the CAF
Log in as a citizen (not an officer) -> menu "Common Application Form"
(/common_application_form/1). Steps:
 1 Basic Details, 2 Town & Country, 3 Factory & Boiler, 4 Electricity,
 5 Science & Technology, 6 Upload Documents (9 files: PDF/JPG/PNG, 5 MB each).
Step 6 button "Save & Submit Application" marks the application submitted.

## Notes
- To make a document mandatory, add Required = true to its line in
  TradeLicence/Helpers/CafFormMetadata.cs (Step 6).
- Hosting on IIS: raise maxAllowedContentLength (web.config) above ~50 MB,
  otherwise IIS blocks large uploads before the app sees them.
