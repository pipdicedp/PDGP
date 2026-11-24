using TradeLicence.Data;
using TradeLicence.Models.Drugs;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>/Form21A/... - Application for Fresh/Retention of Restricted Drug License in Form 21A.</summary>
    public class Form21AController : DrugLicenceControllerBase<Form21AApplication>
    {
        public Form21AController(DrugsApplicationDbContext ctx, ApplicationDbContext shared, IFileEncryptionService encryption)
            : base(DrugLicenceConfig.Form21A, ctx, shared, encryption) { }
    }
}
