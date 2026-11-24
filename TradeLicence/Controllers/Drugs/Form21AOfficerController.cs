using TradeLicence.Data;
using TradeLicence.Models.Drugs;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    public class Form21AOfficerController : DrugLicenceOfficerControllerBase<Form21AApplication>
    {
        public Form21AOfficerController(DrugsApplicationDbContext ctx, ApplicationDbContext shared, IFileEncryptionService encryption)
            : base(DrugLicenceConfig.Form21A, ctx, shared, encryption) { }
    }
}
