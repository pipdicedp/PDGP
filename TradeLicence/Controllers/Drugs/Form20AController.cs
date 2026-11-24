using TradeLicence.Data;
using TradeLicence.Models.Drugs;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>/Form20A/... - Application for Fresh/Retention of Restricted Drug License in Form 20A.</summary>
    public class Form20AController : DrugLicenceControllerBase<Form20AApplication>
    {
        public Form20AController(DrugsApplicationDbContext ctx, ApplicationDbContext shared, IFileEncryptionService encryption)
            : base(DrugLicenceConfig.Form20A, ctx, shared, encryption) { }
    }
}
