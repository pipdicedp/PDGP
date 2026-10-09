using System.Collections.Generic;
using System.Linq;
using TradeLicence.Models.Caf;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// Single source of truth for the Common Application Form: which database
    /// table backs each step, which columns appear on it, and which child
    /// ("sub") tables show up as an Add-row mini-table inside that step.
    ///
    /// This mirrors, column for column, the schema in
    /// Database/CAF_SchemaExtras.sql's companion script (the CREATE TABLE
    /// script supplied for dbo.caf_basic_details and its children). A few
    /// columns on caf_basic_details are deliberately left out of the citizen
    /// form because they are workflow/admin fields the application sets
    /// itself, never the applicant: deptcode, ServiceID, ServiceDes, statuss,
    /// apptype, distcode, ForwardTo, Remarks, appliedon, Forwardeddate, and
    /// the three uploadfinalcert* columns (an officer's final-certificate
    /// upload, not part of the applicant's submission).
    ///
    /// Labels for the dbo.caf_sciencetech_table columns are a best-effort
    /// reading of fairly cryptic legacy column names (wprocess, wname,
    /// hquantity, etc.) — relabel in this file if your team already has the
    /// "real" field names for that sheet.
    /// </summary>
    public static class CafFormMetadata
    {
        private static readonly string[] YesNo = { "Yes", "No" };

        public static readonly List<CafStepDef> Steps = new()
        {
            // ============================================================
            // STEP 1 — Basic Details  (dbo.caf_basic_details)
            // ============================================================
            new CafStepDef
            {
                Number = 1,
                Key = "basic",
                Title = "Basic Details",
                TableName = "caf_basic_details",
                Fields = new List<CafField>
                {
                    // ---- Unit / Applicant Details ----
                    new() { Name = "indname", Label = "Name of Industry / Unit", Type = CafFieldType.Text, MaxLength = 50, Section = "Unit / Applicant Details" },
                    new() { Name = "promotername", Label = "Name of Promoter", Type = CafFieldType.Text, MaxLength = 50, Section = "Unit / Applicant Details" },
                    new() { Name = "unitcategory", Label = "Unit Category", Type = CafFieldType.Select, Options = new[] { "M|Micro", "S|Small", "D|Medium", "L|Large" }, Section = "Unit / Applicant Details" },
                    new() { Name = "norganisationtype", Label = "Nature of Organisation", Type = CafFieldType.Select, MaxLength = 50, Options = new[] { "Proprietorship", "Partnership", "Private Limited", "Public Limited", "LLP", "Co-operative Society", "Others" }, Section = "Unit / Applicant Details" },
                    new() { Name = "natureofapp", Label = "Nature of Application", Type = CafFieldType.Select, MaxLength = 50, Options = new[] { "New", "Expansion", "Diversification", "Modernisation" }, Section = "Unit / Applicant Details" },
                    new() { Name = "nactivityname", Label = "Nature of Activity", Type = CafFieldType.Text, MaxLength = 50, Section = "Unit / Applicant Details" },
                    new() { Name = "catofapp", Label = "Category of Applicant", Type = CafFieldType.Text, MaxLength = 50, Section = "Unit / Applicant Details" },
                    new() { Name = "category", Label = "Category", Type = CafFieldType.Text, MaxLength = 50, Section = "Unit / Applicant Details" },

                    // ---- Communication Address ----
                    new() { Name = "CommunicationAddress1", Label = "Address Line 1", Type = CafFieldType.Text, MaxLength = 400, Section = "Communication Address" },
                    new() { Name = "CommunicationAddress2", Label = "Address Line 2", Type = CafFieldType.Text, MaxLength = 50, Section = "Communication Address" },
                    new() { Name = "CommunicationAddress3", Label = "Address Line 3", Type = CafFieldType.Text, MaxLength = 50, Section = "Communication Address" },
                    new() { Name = "CommunicationState", Label = "State", Type = CafFieldType.Text, MaxLength = 50, Section = "Communication Address" },
                    new() { Name = "CommunicationDistrict", Label = "District", Type = CafFieldType.Text, MaxLength = 50, Section = "Communication Address" },
                    new() { Name = "pincode", Label = "Pincode", Type = CafFieldType.Number, Section = "Communication Address" },

                    // ---- Promoter Address ----
                    new() { Name = "PromotorAddress1", Label = "Address Line 1", Type = CafFieldType.Text, MaxLength = 400, Section = "Promoter Address" },
                    new() { Name = "PromotorAddress2", Label = "Address Line 2", Type = CafFieldType.Text, MaxLength = 50, Section = "Promoter Address" },
                    new() { Name = "PromotorAddress3", Label = "Address Line 3", Type = CafFieldType.Text, MaxLength = 50, Section = "Promoter Address" },
                    new() { Name = "PromotorState", Label = "State", Type = CafFieldType.Text, MaxLength = 50, Section = "Promoter Address" },
                    new() { Name = "PromotorDistrict", Label = "District", Type = CafFieldType.Text, MaxLength = 50, Section = "Promoter Address" },
                    new() { Name = "PromotorPinCode", Label = "Pincode", Type = CafFieldType.Number, Section = "Promoter Address" },

                    // ---- Identification & Contact ----
                    new() { Name = "aadharno", Label = "Aadhar Number", Type = CafFieldType.Text, MaxLength = 12, Section = "Identification & Contact" },
                    new() { Name = "uam", Label = "Udyam / UAM Registration No.", Type = CafFieldType.Text, MaxLength = 25, Section = "Identification & Contact" },
                    new() { Name = "authpersonname", Label = "Authorized Person Name", Type = CafFieldType.Text, MaxLength = 50, Section = "Identification & Contact" },
                    new() { Name = "designation", Label = "Designation", Type = CafFieldType.Text, MaxLength = 50, Section = "Identification & Contact" },
                    new() { Name = "officeno", Label = "Office Phone No.", Type = CafFieldType.Text, MaxLength = 50, Section = "Identification & Contact" },
                    new() { Name = "residenceno", Label = "Residence Phone No.", Type = CafFieldType.Number, Section = "Identification & Contact" },
                    new() { Name = "mobileno", Label = "Mobile Number", Type = CafFieldType.Number, MaxLength = 10, Section = "Identification & Contact" },
                    new() { Name = "altmobileno", Label = "Alternate Mobile Number", Type = CafFieldType.Number, MaxLength = 10, Section = "Identification & Contact" },
                    new() { Name = "pannumber", Label = "PAN Number", Type = CafFieldType.Text, MaxLength = 50, Section = "Identification & Contact" },
                    new() { Name = "fax", Label = "Fax", Type = CafFieldType.Text, MaxLength = 50, Section = "Identification & Contact" },
                    new() { Name = "email", Label = "Email", Type = CafFieldType.Email, MaxLength = 50, Section = "Identification & Contact" },

                    // ---- Site Details ----
                    new() { Name = "sitestatus", Label = "Site Status", Type = CafFieldType.Select, MaxLength = 50, Options = new[] { "Identified", "Not Identified" }, Section = "Site Details" },
                    new() { Name = "reason", Label = "Reason", Type = CafFieldType.Text, MaxLength = 250, Section = "Site Details" },
                    new() { Name = "resurveyno", Label = "Resurvey No.", Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details" },
                    new() { Name = "ucountry", Label = "Country", Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details" },
                    new() { Name = "ustate", Label = "State", Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details" },
                    new() { Name = "udistrict", Label = "District", Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details" },
                    new() { Name = "sitecommune", Label = "Commune", Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details" },
                    new() { Name = "sitevillagename", Label = "Village Name", Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details" },
                    new() { Name = "sitepincode", Label = "Pincode", Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details" },
                    new() { Name = "siteaddress", Label = "Site Address", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Site Details" },

                    // ---- Building & Land Details ----
                    new() { Name = "ownedorleased", Label = "Site Owned / Leased", Type = CafFieldType.Select, MaxLength = 50, Options = new[] { "Owned", "Leased" }, Section = "Building & Land Details" },
                    new() { Name = "ExistingBuilting", Label = "Existing Building Available", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Building & Land Details" },
                    new() { Name = "BuildingType", Label = "Building Type", Type = CafFieldType.Text, MaxLength = 50, Section = "Building & Land Details" },
                    new() { Name = "ApprovalOfPPA", Label = "Approval of PPA Obtained", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Building & Land Details" },
                    new() { Name = "OldBuilding", Label = "Old Building", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Building & Land Details" },
                    new() { Name = "extbuilding", Label = "Extent of Building (Sq.Mtr)", Type = CafFieldType.Decimal, Section = "Building & Land Details" },
                    new() { Name = "landarea", Label = "Land Area (Sq.Mtr)", Type = CafFieldType.Decimal, Section = "Building & Land Details" },
                }
            },

            // ============================================================
            // STEP 2 — Town & Country Planning (dbo.caf_towncountry_table)
            //           + dbo.caf_towncountry_sub_table1 (Land Requirement)
            // ============================================================
            new CafStepDef
            {
                Number = 2,
                Key = "towncountry",
                Title = "Town & Country Planning",
                TableName = "caf_towncountry_table",
                Fields = new List<CafField>
                {
                    new() { Name = "landcostex", Label = "Land Cost - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "landcostpro", Label = "Land Cost - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "buildingcostex", Label = "Building Cost - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "buildingcostpro", Label = "Building Cost - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "indigenousex", Label = "Indigenous Machinery Cost - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "indigenouspro", Label = "Indigenous Machinery Cost - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "miscex", Label = "Misc. Fixed Assets - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "miscpro", Label = "Misc. Fixed Assets - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "others", Label = "Other Assets - Description", Type = CafFieldType.Text, MaxLength = 50, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "othersex", Label = "Other Assets Cost - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "otherspro", Label = "Other Assets Cost - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "workingothers", Label = "Working Capital (Others) - Description", Type = CafFieldType.Text, MaxLength = 50, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "workingothersex", Label = "Working Capital (Others) - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "workingotherspro", Label = "Working Capital (Others) - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "workingcapex", Label = "Working Capital - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "workingcappro", Label = "Working Capital - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "totalexprojectcost", Label = "Total Project Cost - Existing", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                    new() { Name = "totalproprojectcost", Label = "Total Project Cost - Proposed", Type = CafFieldType.Decimal, Section = "Project Cost Details (₹ in Lakhs)" },
                },
                SubTables = new List<CafSubTableDef>
                {
                    new()
                    {
                        Key = "land",
                        TableName = "caf_towncountry_sub_table1",
                        DisplayName = "Land Requirement Details",
                        Fields = new List<CafField>
                        {
                            new() { Name = "landfor", Label = "Land Required For", Type = CafFieldType.Text, MaxLength = 50 },
                            new() { Name = "areaofland", Label = "Area of Land (Sq.Mtr)", Type = CafFieldType.Decimal },
                        }
                    }
                }
            },

            // ============================================================
            // STEP 3 — Factory & Boiler (dbo.caf_factoryboiler_table)
            //           + sub_table1 (Products-Existing), sub_table2
            //           (Products-Proposed), sub_table3 (Plant & Machinery)
            // ============================================================
            new CafStepDef
            {
                Number = 3,
                Key = "factoryboiler",
                Title = "Factory & Boiler",
                TableName = "caf_factoryboiler_table",
                Fields = new List<CafField>
                {
                    new() { Name = "skilled", Label = "Skilled Workers", Type = CafFieldType.Number, Section = "Manpower / Employment Details" },
                    new() { Name = "unskilled", Label = "Unskilled Workers", Type = CafFieldType.Number, Section = "Manpower / Employment Details" },
                    new() { Name = "supervisory", Label = "Supervisory Staff", Type = CafFieldType.Number, Section = "Manpower / Employment Details" },
                    new() { Name = "management", Label = "Management Staff", Type = CafFieldType.Number, Section = "Manpower / Employment Details" },
                    new() { Name = "contract", Label = "Contract Workers", Type = CafFieldType.Number, Section = "Manpower / Employment Details" },
                    new() { Name = "Indirect", Label = "Indirect Employment", Type = CafFieldType.Number, Section = "Manpower / Employment Details" },
                    new() { Name = "total", Label = "Total Employment", Type = CafFieldType.Number, Section = "Manpower / Employment Details" },

                    new() { Name = "shift", Label = "Shift Pattern", Type = CafFieldType.Text, MaxLength = 50, Section = "Shift Details" },
                    new() { Name = "shifti", Label = "Shift I - Workers", Type = CafFieldType.Number, Section = "Shift Details" },
                    new() { Name = "shiftii", Label = "Shift II - Workers", Type = CafFieldType.Number, Section = "Shift Details" },
                    new() { Name = "shiftiii", Label = "Shift III - Workers", Type = CafFieldType.Number, Section = "Shift Details" },

                    new() { Name = "vessels", Label = "Pressure Vessels Installed", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Boiler / Plant Safety" },
                    new() { Name = "hazardous", Label = "Hazardous Process Involved", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Boiler / Plant Safety" },
                },
                SubTables = new List<CafSubTableDef>
                {
                    new()
                    {
                        Key = "productsexisting",
                        TableName = "caf_factoryboiler_sub_table1",
                        DisplayName = "Products / Raw Materials - Existing",
                        Fields = new List<CafField>
                        {
                            new() { Name = "product", Label = "Product / Material", Type = CafFieldType.Text, MaxLength = 50 },
                            new() { Name = "quantity", Label = "Quantity", Type = CafFieldType.Number },
                            new() { Name = "value", Label = "Value (₹)", Type = CafFieldType.Number },
                        }
                    },
                    new()
                    {
                        Key = "productsproposed",
                        TableName = "caf_factoryboiler_sub_table2",
                        DisplayName = "Products / Raw Materials - Proposed",
                        Fields = new List<CafField>
                        {
                            new() { Name = "product", Label = "Product / Material", Type = CafFieldType.Text, MaxLength = 50 },
                            new() { Name = "quantity", Label = "Quantity", Type = CafFieldType.Number },
                            new() { Name = "value", Label = "Value (₹)", Type = CafFieldType.Number },
                        }
                    },
                    new()
                    {
                        Key = "plantmachinery",
                        TableName = "caf_factoryboiler_sub_table3",
                        DisplayName = "Plant & Machinery - Proposed",
                        Fields = new List<CafField>
                        {
                            new() { Name = "proposed", Label = "Machinery Description", Type = CafFieldType.Text, MaxLength = 50 },
                            new() { Name = "plantquantity", Label = "Quantity", Type = CafFieldType.Number },
                            new() { Name = "totalhp", Label = "Total HP", Type = CafFieldType.Decimal },
                            new() { Name = "plantvalue", Label = "Value (₹)", Type = CafFieldType.Decimal },
                        }
                    }
                }
            },

            // ============================================================
            // STEP 4 — Electricity (dbo.caf_electricity_table) — no sub-table
            // ============================================================
            new CafStepDef
            {
                Number = 4,
                Key = "electricity",
                Title = "Electricity",
                TableName = "caf_electricity_table",
                Fields = new List<CafField>
                {
                    new() { Name = "powerloadhp", Label = "Power Load (HP)", Type = CafFieldType.Decimal, Section = "Load Details (HP / KVA)" },
                    new() { Name = "powerloadkv", Label = "Power Load (KVA)", Type = CafFieldType.Number, Section = "Load Details (HP / KVA)" },
                    new() { Name = "lightloadhp", Label = "Light Load (HP)", Type = CafFieldType.Decimal, Section = "Load Details (HP / KVA)" },
                    new() { Name = "lightloadkv", Label = "Light Load (KVA)", Type = CafFieldType.Number, Section = "Load Details (HP / KVA)" },
                    new() { Name = "powerreqhp", Label = "Power Required (HP)", Type = CafFieldType.Decimal, Section = "Load Details (HP / KVA)" },
                    new() { Name = "powerreqkv", Label = "Power Required (KVA)", Type = CafFieldType.Number, Section = "Load Details (HP / KVA)" },
                    new() { Name = "connectedhp", Label = "Connected Load (HP)", Type = CafFieldType.Decimal, Section = "Load Details (HP / KVA)" },
                    new() { Name = "connectedkv", Label = "Connected Load (KVA)", Type = CafFieldType.Number, Section = "Load Details (HP / KVA)" },
                    new() { Name = "maxdemandhp", Label = "Max. Demand (HP)", Type = CafFieldType.Decimal, Section = "Load Details (HP / KVA)" },
                    new() { Name = "maxdemandkv", Label = "Max. Demand (KVA)", Type = CafFieldType.Number, Section = "Load Details (HP / KVA)" },

                    new() { Name = "existingconnection", Label = "Existing Electricity Connection", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Connection Details" },
                    new() { Name = "existingdiesel", Label = "Existing Diesel Generator", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Connection Details" },
                    new() { Name = "physeg", Label = "Physical Segregation of Load", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Connection Details" },
                }
            },

            // ============================================================
            // STEP 5 — Science & Technology / Pollution Control
            //           (dbo.caf_sciencetech_table) + sub_table1 (Raw Material)
            // ============================================================
            new CafStepDef
            {
                Number = 5,
                Key = "sciencetech",
                Title = "Science & Technology",
                TableName = "caf_sciencetech_table",
                Fields = new List<CafField>
                {
                    new() { Name = "sourceofsupply", Label = "Source of Water Supply", Type = CafFieldType.Text, MaxLength = 50, Section = "Water Supply" },
                    new() { Name = "watersupply", Label = "Mode of Water Supply", Type = CafFieldType.Text, MaxLength = 50, Section = "Water Supply" },
                    new() { Name = "waterprocess", Label = "Water for Process (KLD)", Type = CafFieldType.Decimal, Section = "Water Supply" },
                    new() { Name = "vesselwash", Label = "Water for Vessel Wash (KLD)", Type = CafFieldType.Decimal, Section = "Water Supply" },
                    new() { Name = "cooling", Label = "Water for Cooling (KLD)", Type = CafFieldType.Number, Section = "Water Supply" },
                    new() { Name = "domestic", Label = "Water for Domestic Use (KLD)", Type = CafFieldType.Decimal, Section = "Water Supply" },
                    new() { Name = "gardening", Label = "Water for Gardening (KLD)", Type = CafFieldType.Decimal, Section = "Water Supply" },
                    new() { Name = "watertotal", Label = "Total Water Requirement (KLD)", Type = CafFieldType.Decimal, Section = "Water Supply" },

                    new() { Name = "borewell", Label = "Borewell Existing", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Borewell Details" },
                    new() { Name = "permission", Label = "Permission Obtained", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Borewell Details" },
                    new() { Name = "exborewell", Label = "Existing Borewell No.", Type = CafFieldType.Text, MaxLength = 50, Section = "Borewell Details" },
                    new() { Name = "ppno", Label = "PWD Permit No.", Type = CafFieldType.Number, Section = "Borewell Details" },
                    new() { Name = "permissionno", Label = "Permission No.", Type = CafFieldType.Number, Section = "Borewell Details" },

                    new() { Name = "recyclewater", Label = "Water Recycled", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Water Recycling" },
                    new() { Name = "treatedwater", Label = "Treated Water Used", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Water Recycling" },
                    new() { Name = "documentuploaded", Label = "Supporting Document Uploaded", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Water Recycling" },
                    new() { Name = "recycledmaterialused", Label = "Recycled Material Used", Type = CafFieldType.YesNo, MaxLength = 50, Options = YesNo, Section = "Water Recycling" },
                    new() { Name = "name", Label = "Recycled Material Name", Type = CafFieldType.Text, MaxLength = 50, Section = "Water Recycling" },
                    new() { Name = "fquantity", Label = "Quantity (per day)", Type = CafFieldType.Number, Section = "Water Recycling" },
                    new() { Name = "source", Label = "Source", Type = CafFieldType.Text, MaxLength = 50, Section = "Water Recycling" },

                    new() { Name = "salvaged", Label = "Material Salvaged", Type = CafFieldType.TextArea, MaxLength = 250, Section = "Raw Material / Process" },
                    new() { Name = "sourceofprocess", Label = "Source of Process", Type = CafFieldType.TextArea, MaxLength = 250, Section = "Raw Material / Process" },
                    new() { Name = "lesspolluting", Label = "Less Polluting Process Adopted", Type = CafFieldType.TextArea, MaxLength = 250, Section = "Raw Material / Process" },

                    new() { Name = "fuelsource", Label = "Fuel Source", Type = CafFieldType.Text, MaxLength = 50, Section = "Fuel Details" },
                    new() { Name = "fueltype", Label = "Fuel Type", Type = CafFieldType.Text, MaxLength = 50, Section = "Fuel Details" },
                    new() { Name = "fuelquantity", Label = "Fuel Quantity / Method of Disposal", Type = CafFieldType.Text, MaxLength = 50, Section = "Fuel Details" },
                    new() { Name = "fuelunit", Label = "Fuel Unit", Type = CafFieldType.Text, MaxLength = 50, Section = "Fuel Details" },

                    new() { Name = "airpollution", Label = "Air Pollution Control Measures", Type = CafFieldType.Text, MaxLength = 50, Section = "Air Pollution Control" },
                    new() { Name = "wprocess", Label = "Wastewater - Process (KLD)", Type = CafFieldType.Text, MaxLength = 250, Section = "Air Pollution Control" },
                    new() { Name = "wvessel", Label = "Wastewater - Vessel Wash (KLD)", Type = CafFieldType.Text, MaxLength = 250, Section = "Air Pollution Control" },
                    new() { Name = "wcooling", Label = "Wastewater - Cooling (KLD)", Type = CafFieldType.Text, MaxLength = 250, Section = "Air Pollution Control" },
                    new() { Name = "wboiler", Label = "Wastewater - Boiler (KLD)", Type = CafFieldType.Decimal, Section = "Air Pollution Control" },
                    new() { Name = "wdomestic", Label = "Wastewater - Domestic (KLD)", Type = CafFieldType.Text, MaxLength = 250, Section = "Air Pollution Control" },
                    new() { Name = "wdischarge", Label = "Wastewater Discharge Point", Type = CafFieldType.Text, MaxLength = 100, Section = "Air Pollution Control" },
                    new() { Name = "wtotal", Label = "Total Wastewater (KLD)", Type = CafFieldType.Decimal, Section = "Air Pollution Control" },
                    new() { Name = "wname", Label = "Treatment Method Name", Type = CafFieldType.Text, MaxLength = 250, Section = "Air Pollution Control" },
                    new() { Name = "wquantity", Label = "Treatment Capacity", Type = CafFieldType.Text, MaxLength = 250, Section = "Air Pollution Control" },
                    new() { Name = "wmethod", Label = "Method of Disposal", Type = CafFieldType.Text, MaxLength = 50, Section = "Air Pollution Control" },

                    new() { Name = "swairPollution", Label = "Solid Waste - Air Pollution Impact", Type = CafFieldType.Text, MaxLength = 250, Section = "Solid / Hazardous Waste" },
                    new() { Name = "hname", Label = "Hazardous Waste Name", Type = CafFieldType.Text, MaxLength = 50, Section = "Solid / Hazardous Waste" },
                    new() { Name = "hquantity", Label = "Hazardous Waste Quantity", Type = CafFieldType.Text, MaxLength = 250, Section = "Solid / Hazardous Waste" },
                    new() { Name = "hmethod", Label = "Hazardous Waste Disposal Method", Type = CafFieldType.Text, MaxLength = 50, Section = "Solid / Hazardous Waste" },
                    new() { Name = "hunit", Label = "Hazardous Waste Unit", Type = CafFieldType.Text, MaxLength = 50, Section = "Solid / Hazardous Waste" },

                    new() { Name = "nodg", Label = "Number of DG Sets", Type = CafFieldType.Number, Section = "DG Set & Noise Control" },
                    new() { Name = "dgcapacity", Label = "DG Set Capacity (KVA)", Type = CafFieldType.Number, Section = "DG Set & Noise Control" },
                    new() { Name = "ventht", Label = "Stack / Vent Height (Mtr)", Type = CafFieldType.Decimal, Section = "DG Set & Noise Control" },
                    new() { Name = "buildingroof", Label = "Building Roof Type", Type = CafFieldType.Text, MaxLength = 50, Section = "DG Set & Noise Control" },
                    new() { Name = "noisecontrol", Label = "Noise Control Measures", Type = CafFieldType.Text, MaxLength = 50, Section = "DG Set & Noise Control" },
                    new() { Name = "otherremarks", Label = "Other Remarks", Type = CafFieldType.Text, MaxLength = 50, Section = "DG Set & Noise Control" },
                },
                SubTables = new List<CafSubTableDef>
                {
                    new()
                    {
                        Key = "rawmaterial",
                        TableName = "caf_sciencetech_sub_table1",
                        DisplayName = "Raw Material Requirement",
                        Fields = new List<CafField>
                        {
                            new() { Name = "rawmaterial", Label = "Raw Material", Type = CafFieldType.Text, MaxLength = 50 },
                            new() { Name = "reqperday", Label = "Required / Day", Type = CafFieldType.Number },
                            new() { Name = "waterunit", Label = "Unit", Type = CafFieldType.Text, MaxLength = 50 },
                            new() { Name = "watervalue", Label = "Value", Type = CafFieldType.Number },
                            new() { Name = "waterimported", Label = "Imported", Type = CafFieldType.YesNo, MaxLength = 15, Options = YesNo },
                        }
                    }
                }
            },

            // ============================================================
            // STEP 6 — Upload Documents  (dbo.caf_doc_table)
            // One varbinary(max) column per document; no Fields — the view
            // renders a file input for each entry of Documents instead.
            // Set Required = true on any document that must be uploaded
            // before the application can be submitted.
            // ============================================================
            new CafStepDef
            {
                Number = 6,
                Key = "documents",
                Title = "Upload Documents",
                TableName = "caf_doc_table",
                Documents = new List<CafDocumentDef>
                {
                    new() { Column = "common_app",       Label = "Common Application" },
                    new() { Column = "land_doc",         Label = "Land Document" },
                    new() { Column = "building_plan",    Label = "Building Plan" },
                    new() { Column = "process_details",  Label = "Process Details" },
                    new() { Column = "partnership_deed", Label = "Partnership Deed" },
                    new() { Column = "resolution",       Label = "Resolution" },
                    new() { Column = "demand_bill",      Label = "Demand Bill" },
                    new() { Column = "form1c_challan",   Label = "Form 1C Challan" },
                    new() { Column = "fmb_sketch",       Label = "FMB Sketch" },
                }
            },
        };

        /// <summary>Largest single document accepted, in bytes (5 MB).</summary>
        public const int MaxDocumentBytes = 5 * 1024 * 1024;

        /// <summary>Every document column of dbo.caf_doc_table — also the whitelist used to build SQL.</summary>
        public static IReadOnlyList<CafDocumentDef> AllDocuments =>
            Steps.SelectMany(x => x.Documents).ToList();

        public static CafDocumentDef? DocumentByColumn(string column) =>
            AllDocuments.FirstOrDefault(d => string.Equals(d.Column, column, System.StringComparison.OrdinalIgnoreCase));

        public static CafStepDef? ByNumber(int number) => Steps.FirstOrDefault(s => s.Number == number);
        public static CafStepDef? ByKey(string key) => Steps.FirstOrDefault(s => string.Equals(s.Key, key, System.StringComparison.OrdinalIgnoreCase));
        public static int MinStep => Steps.Min(s => s.Number);
        public static int MaxStep => Steps.Max(s => s.Number);
    }
}
