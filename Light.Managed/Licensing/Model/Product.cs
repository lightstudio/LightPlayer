using Windows.Globalization;

namespace Light.Managed.Licensing.Model
{
    internal sealed class Product : IProduct
    {
        internal Product()
        {
            
        }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public Language Language { get; private set; }
        public string Identifier { get; private set; }
        public string ServerEndpoint { get; private set; }
        public double Price { get; private set; }
        public string Culture { get; }
    }
}
