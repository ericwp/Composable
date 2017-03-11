using Composable.Persistence.KeyValueStorage;

namespace Composable.CQRS.CQRS.Query.Models.Generators
{
    interface IVersioningDocumentDbReader : IDocumentDbReader
    {
        bool TryGetVersion<TDocument>(object key, out TDocument document, int version);
        TValue GetVersion<TValue>(object key, int version);
    }
}