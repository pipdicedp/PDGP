using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TradeLicence.Models
{
    /// <summary>
    /// Society - Request for Name / Business Place / Members (Name Change of Society).
    /// Maps 1:1 to dbo.SocietyNameChangeRequest — the table columns are fixed by
    /// whatever is defined in the database (see Database/SocietyNameChange_Tables.sql).
    /// The uploaded Change Request Form is stored on disk under
    /// wwwroot\uploads\societynamechange\ and its path/name/type/size are saved here.
    /// </summary>
    [Table("SocietyNameChangeRequest")]
    public class SocietyNameChangeRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Application No. is required")]
        [StringLength(50)]
        [Display(Name = "Application No.")]
        public string ApplicationNo { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Serial No.")]
        public string? SerialNo { get; set; }

        [Display(Name = "Name Change")]
        public bool IsNameChange { get; set; }

        [Display(Name = "Address Change")]
        public bool IsAddressChange { get; set; }

        [Display(Name = "Member")]
        public bool IsMemberChange { get; set; }

        [Display(Name = "Others")]
        public bool IsOthers { get; set; }

        /// <summary>Original file name of the uploaded Change Request Form.</summary>
        [StringLength(520)]
        public string FormFileName { get; set; } = string.Empty;

        /// <summary>Saved location, e.g. /uploads/societynamechange/&lt;guid&gt;.png</summary>
        [StringLength(1000)]
        public string FormFilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FormContentType { get; set; }

        public int? FormFileSizeKB { get; set; }

        [StringLength(2000)]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>Workflow status — "Submitted" when the citizen saves the request.</summary>
        [StringLength(20)]
        public string RequestStatus { get; set; } = "Submitted";

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public bool IsActive { get; set; } = true;

        // ---------- form-only members (not columns in the table) ----------

        /// <summary>The "Upload Change Request Form" image/PDF upload.</summary>
        [NotMapped]
        [Required(ErrorMessage = "Change Request Form is required")]
        public IFormFile? ChangeRequestFormFile { get; set; }

        /// <summary>Read-only helper for the list page.</summary>
        [NotMapped]
        public string RequestedChangeFor
        {
            get
            {
                var parts = new List<string>();
                if (IsNameChange) parts.Add("Name Change");
                if (IsAddressChange) parts.Add("Address Change");
                if (IsMemberChange) parts.Add("Member");
                if (IsOthers) parts.Add("Others");
                return parts.Count == 0 ? "-" : string.Join(", ", parts);
            }
        }
    }
}
