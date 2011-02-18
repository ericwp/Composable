using Composable.DDD;

namespace Composable.DomainEvents
{
    public class BeforeAndAfter<TObject> : ValueObject<BeforeAndAfter<TObject>>
    {
        public TObject Before { get; set; }
        public TObject After { get; set; }
    }
}