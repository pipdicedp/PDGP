using System;
using System.Collections.Generic;

namespace TradeLicence.Models
{
    public class SaveEmployersRequest
    {
        public int ApplicationId { get; set; }
        public List<EmployerRowDto> Employers { get; set; } = new();
    }

    public class EmployerRowDto
    {
        public string? EmployerType { get; set; }
        public string? EmployerName { get; set; }
        public bool IsMinor { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? ResidentialAddress { get; set; }
        public string? District { get; set; }
        public string? PinCode { get; set; }
        public bool RcToBeIssued { get; set; }
    }

    public class SaveFormIXPartARequest
    {
        public int ApplicationId { get; set; }
        public List<FormIXPartARowDto> Rows { get; set; } = new();
    }

    public class FormIXPartARowDto
    {
        public string? EmployeeName { get; set; }
        public string? Sex { get; set; }
        public string? FatherHusbandName { get; set; }
        public string? Designation { get; set; }
        public string? EmployeeNumber { get; set; }
        public DateTime? DateOfEntryIntoService { get; set; }
        public string? PersonCategory { get; set; }
        public string? Shift { get; set; }
        public string? TimeOfCommencementOfWork { get; set; }
        public string? RestIntervalHours { get; set; }
        public string? TimeWorkEnds { get; set; }
        public string? WeeklyHoliday { get; set; }
    }

    public class SaveFormIXPartBRequest
    {
        public int ApplicationId { get; set; }
        public List<FormIXPartBRowDto> Rows { get; set; } = new();
    }

    public class FormIXPartBRowDto
    {
        public string? ClassOfWorkers { get; set; }
        public decimal? MaxRateOfWage { get; set; }
        public decimal? MinRateOfWage { get; set; }
    }
}
