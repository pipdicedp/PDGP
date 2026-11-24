using TradeLicence.Data;
using TradeLicence.Models.Drugs;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    public class Form20AOfficerController : DrugLicenceOfficerControllerBase<Form20AApplication>
    {
        public Form20AOfficerController(DrugsApplicationDbContext ctx, ApplicationDbContext shared, IFileEncryptionService encryption)
            : base(DrugLicenceConfig.Form20A, ctx, shared, encryption) { }
    }
}
