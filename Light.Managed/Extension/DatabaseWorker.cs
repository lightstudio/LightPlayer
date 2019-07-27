#if ENABLE_STAGING
using System.Linq;
using Light.Managed.Database;
using Light.Managed.Extension.Model;

namespace Light.Managed.Extension
{
    public class ExtensionDatabaseWorker
    {
        private readonly ExtensionDbContext _context;

        /// <summary>
        /// Class constructor that creates instance of <see cref="ExtensionDatabaseWorker"/>.
        /// </summary>
        /// <param name="context">Instance of <see cref="ExtensionDbContext"/>.</param>
        public ExtensionDatabaseWorker(ExtensionDbContext context)
        {
            _context = context;
        }

        public void RegisterPackage(ExtPackage pkg)
        {
            _context.Packages.Add(pkg);
            _context.SaveChanges();
        }

        public void RegisterPackageAndFormat(ExtPackage pkg)
        {
            var formats = pkg.SupportedFormats.Split(';');

            foreach (var f in formats)
            {
                _context.Formats.Add(new FormatTable
                {
                    Format = f,
                    PluginId = pkg.PackageId
                });
            }

            _context.SaveChanges();
            RegisterPackage(pkg);
        }

        public void UnregisterPackage(int id)
        {
            var query = _context.Packages.Where(i => i.Id == id);

            var e = query.Any() ? query.FirstOrDefault() : null;

            if (e != null)
            {
                if (e.Type == PackageType.Decoder)
                {
                    var fquery = from c in _context.Formats
                                where c.PluginId == e.PackageId
                                select c;
                    _context.Formats.RemoveRange(fquery);
                }
                // TODO: Post-recovery default formats

                _context.Packages.Remove(e);
                _context.SaveChanges();
            }
        }

        public void UnregisterPackage(string packageId)
        {
            var query = from c in _context.Packages where c.PackageId == packageId select c;
            if (query.Any()) UnregisterPackage(query.FirstOrDefault().Id);
        }

        public string FindPreferredDecoder(string ext)
        {
            var query = from c in _context.Formats where c.Format == ext select c;
            if (query.Any()) return query.FirstOrDefault().PluginId;
            return "Default.Null";
        }

        public string FindDecoderFileById(string id)
        {
            var query = from c in _context.Packages where c.PackageId == id select c;
            if (query.Any()) return query.FirstOrDefault().EntryPoint;
            return "Default.Null";
        }
    }
}
#endif
