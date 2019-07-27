using System.Threading.Tasks;
using Light.Managed.Licensing.Model;

namespace Light.Managed.Licensing
{
    internal interface IProductService
    {
        bool ConfirmPackageAsync(IProduct package);
        Task<bool> DoPurchaseAsync(IProduct package);
    }
}
