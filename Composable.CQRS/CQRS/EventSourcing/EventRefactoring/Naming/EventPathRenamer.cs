using System;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Naming
{
    public class EventPathRenamer : IRenameEvents
    {
        private string OldPath { get; }
        private string NewPath { get;  }

        public EventPathRenamer(string oldPath, Type eventAtNewPath)
        {
            OldPath = oldPath;
            NewPath = eventAtNewPath.FullName.Substring(0 , eventAtNewPath.FullName.Length - eventAtNewPath.Name.Length);
        }

        public void Rename(EventNameMapping mapping)
        {
            if(mapping.FullName.StartsWith(NewPath))
            {
                mapping.FullName = OldPath + mapping.FullName.Substring(NewPath.Length);
            }
        }
    }
}