using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using Light.Managed.Licensing.Model;

namespace Light.Managed.Licensing
{
    internal sealed class ProductService : IProductService
    {
        public bool ConfirmPackageAsync(IProduct package)
        {
#if DEBUG
            var licInfo = CurrentAppSimulator.LicenseInformation.ProductLicenses[package.Identifier];
#else
            var licInfo = CurrentApp.LicenseInformation.ProductLicenses[package.Identifier];
#endif
            return licInfo.IsActive;
        }

        public async Task<bool> DoPurchaseAsync(IProduct package)
        {
            var isPurchased = ConfirmPackageAsync(package);
            if (isPurchased) return true;
#if DEBUG
            await CurrentAppSimulator.RequestProductPurchaseAsync(package.Identifier);
#else
            await CurrentApp.RequestProductPurchaseAsync(package.Identifier);
#endif
            return ConfirmPackageAsync(package);
        }
    }
}