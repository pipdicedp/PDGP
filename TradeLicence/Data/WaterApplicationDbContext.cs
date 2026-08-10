using Microsoft.EntityFrameworkCore;
using WaterConnection.Models;
using WaterConnection.Models.Masters;

namespace WaterConnection.Data
{
    public class WaterApplicationDbContext: DbContext
    {
        public WaterApplicationDbContext(DbContextOptions<WaterApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WaterConnectionApplication> WaterConnectionApplications { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }

        public DbSet<DepartmentMaster> Departments { get; set; }
        public DbSet<SectionMaster> Sections { get; set; }
        public DbSet<ContractorMaster> Contractors { get; set; }
        public DbSet<AreaMaster> Areas { get; set; }
        public DbSet<PurposeMaster> Purposes { get; set; }
        public DbSet<NameAddressVerificationMaster> NameAddressVerifications { get; set; }
        public DbSet<OwnershipVerificationMaster> OwnershipVerifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------------- Department_Master ----------------
            modelBuilder.Entity<DepartmentMaster>(entity =>
            {
                entity.ToTable("Department_Master");
                entity.Property(e => e.DeptCode).HasColumnName("Dept_Code");
                entity.Property(e => e.DepartmentName).HasColumnName("Department_Name");
            });

            // ---------------- Section_Master ----------------
            modelBuilder.Entity<SectionMaster>(entity =>
            {
                entity.ToTable("Section_Master");
                entity.Property(e => e.SectionCode).HasColumnName("Section_Code");
                entity.Property(e => e.SectionName).HasColumnName("Section_Name");
                entity.Property(e => e.DeptCode).HasColumnName("Dept_Code");

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Sections)
                    .HasForeignKey(e => e.DeptCode)
                    .HasConstraintName("FK_Section_Master_Department_Master")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- Contractor_Master ----------------
            modelBuilder.Entity<ContractorMaster>(entity =>
            {
                entity.ToTable("Contractor_Master");
                entity.Property(e => e.ContractorCode).HasColumnName("Contractor_Code");
                entity.Property(e => e.ContractorName).HasColumnName("Contractor_Name");
                entity.Property(e => e.ContractorAddress).HasColumnName("Contractor_Address");
                entity.Property(e => e.SectionCode).HasColumnName("Section_Code");

                entity.HasOne(e => e.Section)
                    .WithMany(s => s.Contractors)
                    .HasForeignKey(e => e.SectionCode)
                    .HasConstraintName("FK_Contractor_Master_Section_Master")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- Area_Master ----------------
            modelBuilder.Entity<AreaMaster>(entity =>
            {
                entity.ToTable("Area_Master");
                entity.Property(e => e.AreaCode).HasColumnName("Area_Code");
                entity.Property(e => e.AreaName).HasColumnName("Area_Name");
                entity.Property(e => e.ContractorCode).HasColumnName("Contractor_Code");

                entity.HasOne(e => e.Contractor)
                    .WithMany(c => c.Areas)
                    .HasForeignKey(e => e.ContractorCode)
                    .HasConstraintName("FK_Area_Master_Contractor_Master")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- Purpose_Master ----------------
            modelBuilder.Entity<PurposeMaster>(entity =>
            {
                entity.ToTable("Purpose_Master");
                entity.Property(e => e.PurposeCode).HasColumnName("Purpose_Code");
                entity.Property(e => e.PurposeName).HasColumnName("Purpose_Name");
            });

            // ---------------- NameAddressVerification_Master ----------------
            modelBuilder.Entity<NameAddressVerificationMaster>(entity =>
            {
                entity.ToTable("NameAddressVerification_Master");
                entity.Property(e => e.NaVerifyCode).HasColumnName("NA_Verify_Code");
                entity.Property(e => e.DocumentName).HasColumnName("Document_Name");
            });

            // ---------------- OwnershipVerification_Master ----------------
            modelBuilder.Entity<OwnershipVerificationMaster>(entity =>
            {
                entity.ToTable("OwnershipVerification_Master");
                entity.Property(e => e.OwnFileCode).HasColumnName("OwnFile_Code");
                entity.Property(e => e.DocumentName).HasColumnName("Document_Name");
            });

            // ---------------- WaterConnectionApplication ----------------
            modelBuilder.Entity<WaterConnectionApplication>(entity =>
            {
                entity.ToTable("WaterConnectionApplication");
                entity.Property(e => e.ApplicationId).HasColumnName("Application_Id");
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.PowerOfAttorney).HasColumnName("Power_Of_Attorney");
                entity.Property(e => e.FatherName).HasColumnName("Father_Name");
                entity.Property(e => e.PhoneNumber).HasColumnName("Phone_Number");
                entity.Property(e => e.Email).HasColumnName("Email_Id");
                entity.Property(e => e.SpouseName).HasColumnName("Spouse_Name");

                entity.Property(e => e.CommDoorNo).HasColumnName("Communication_Door_Number");
                entity.Property(e => e.CommAddress1).HasColumnName("Communication_Address_Line1");
                entity.Property(e => e.CommAddress2).HasColumnName("Communication_Address_Line2");
                entity.Property(e => e.CommCity).HasColumnName("Communication_Town_City");

                entity.Property(e => e.ConnDoorNo).HasColumnName("Connection_Address_Door_Number");
                entity.Property(e => e.ConnAddress1).HasColumnName("Connection_Address_Line1");
                entity.Property(e => e.ConnAddress2).HasColumnName("Connection_Address_Line2");
                entity.Property(e => e.ConnCity).HasColumnName("Connection_Address_Town_City");

                entity.Property(e => e.PurposeCode).HasColumnName("Purpose_Code");
                entity.Property(e => e.DeptCode).HasColumnName("Dept_Code");
                entity.Property(e => e.SectionCode).HasColumnName("Section_Code");
                entity.Property(e => e.ContractorCode).HasColumnName("Contractor_Code");
                entity.Property(e => e.AreaCode).HasColumnName("Area_Code");
                entity.Property(e => e.NaVerifyCode).HasColumnName("NA_Verify_Code");
                entity.Property(e => e.OwnFileCode).HasColumnName("OwnFile_Code");

                entity.Property(e => e.ApplicationDate).HasColumnName("Application_Date");
                entity.Property(e => e.Status).HasColumnName("Status");

                entity.HasOne(e => e.Purpose)
                    .WithMany()
                    .HasForeignKey(e => e.PurposeCode)
                    .HasConstraintName("FK_App_Purpose")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Department)
                    .WithMany()
                    .HasForeignKey(e => e.DeptCode)
                    .HasConstraintName("FK_App_Dept")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Section)
                    .WithMany()
                    .HasForeignKey(e => e.SectionCode)
                    .HasConstraintName("FK_App_Section")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Contractor)
                    .WithMany()
                    .HasForeignKey(e => e.ContractorCode)
                    .HasConstraintName("FK_App_Contractor")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Area)
                    .WithMany()
                    .HasForeignKey(e => e.AreaCode)
                    .HasConstraintName("FK_App_Area")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.NameAddressVerification)
                    .WithMany()
                    .HasForeignKey(e => e.NaVerifyCode)
                    .HasConstraintName("FK_App_NAVerify")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OwnershipVerification)
                    .WithMany()
                    .HasForeignKey(e => e.OwnFileCode)
                    .HasConstraintName("FK_App_OwnVerify")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------------- Application_Documents ----------------
            modelBuilder.Entity<ApplicationDocument>(entity =>
            {
                entity.ToTable("Application_Documents");
                entity.Property(e => e.DocumentId).HasColumnName("Document_Id");
                entity.Property(e => e.ApplicationId).HasColumnName("Application_Id");
                entity.Property(e => e.DocumentPurpose).HasColumnName("Document_Purpose");
                entity.Property(e => e.DocumentOption).HasColumnName("Document_Option");
                entity.Property(e => e.IsRequired).HasColumnName("Is_Required");
                entity.Property(e => e.FileContent).HasColumnName("File_Path").HasColumnType("varbinary(max)");
                entity.Property(e => e.UploadedOn).HasColumnName("Uploaded_On");

                entity.HasOne(e => e.Application)
                    .WithMany(a => a.Documents)
                    .HasForeignKey(e => e.ApplicationId)
                    .HasConstraintName("FK_Doc_Application")
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
