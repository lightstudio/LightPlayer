using Windows.Globalization;

namespace Light.Managed.Licensing.Model
{
    public interface IProduct
    {
        string Name { get; }
        string Description { get; }
        Language Language { get; }
        string Identifier { get; }
        string ServerEndpoint { get; }
        double Price { get; }
        string Culture { get; }
    }
}