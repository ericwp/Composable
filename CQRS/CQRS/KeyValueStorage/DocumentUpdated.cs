using System;
using System.Reactive.Linq;

namespace Composable.KeyValueStorage
{

    public class DocumentUpdated<TDocument> : IDocumentUpdated<TDocument>
    {
        public TDocument Document { get; private set; }
        public string Key { get; private set; }

        public DocumentUpdated(string key, TDocument document)
        {
            Document = document;
            Key = key;
        }
    }

    public class DocumentUpdated : DocumentUpdated<object>, IDocumentUpdated
    {
        public DocumentUpdated(string key, object document) : base(key, document) {}
    }

    public static class DocumentUpdatedObservableExtensions
    {
        public static IObservable<IDocumentUpdated<TDocument>>  OfType<TDocument>(IObservable<IDocumentUpdated> me)
        {
            return me.Where(documentUpdated => documentUpdated.Document is TDocument)
                .Select(documentUpdated => new DocumentUpdated<TDocument>(documentUpdated.Key, (TDocument)documentUpdated.Document) );
        } 
    }
}