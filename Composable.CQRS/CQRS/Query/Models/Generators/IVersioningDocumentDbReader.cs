using Composable.KeyValueStorage;

namespace Composable.CQRS.Query.Models.Generators
{
    public interface IVersioningDocumentDbReader : IDocumentDbReader
    {
        bool TryGetVersion<TDocument>(object key, out TDocument document, int version);
        TValue GetVersion<TValue>(object key, int version);
    }
}