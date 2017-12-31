using System;

namespace Composable.Persistence.EventStore.Refactoring.Naming
{
    //todo:remove
    [AttributeUsage(AttributeTargets.Class)] class EventRenamedFromAttribute : Attribute
    {
        public string FullName { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }
}