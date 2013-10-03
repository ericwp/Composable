﻿using System;
using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public partial class DocumentDbSession
    {        
        internal class DocumentItem
        {
            private readonly IDocumentDb _backingStore;
            private DocumentKey Key { get; set; }

            public DocumentItem(DocumentKey key, IDocumentDb backingStore)
            {
                _backingStore = backingStore;
                Key = key;
            }

            private Object Document { get; set; }
            public bool IsDeleted { get; private set; }
            private bool IsInBackingStore { get; set; }

            private bool ScheduledForAdding { get { return !IsInBackingStore && !IsDeleted && Document != null; } }
            private bool ScheduledForRemoval { get { return IsInBackingStore && IsDeleted; } }
            private bool ScheduledForUpdate { get { return IsInBackingStore && !IsDeleted; } }

            public void Delete()
            {
                IsDeleted = true;
            }

            public void Save(object document)
            {
                if(document == null)
                {
                    throw new ArgumentNullException("document");
                }
                Document = document;
                IsDeleted = false;
            }

            public void DocumentLoadedFromBackingStore(object document)
            {
                if (document == null)
                {
                    throw new ArgumentNullException("document");
                }
                Document = document;
                IsInBackingStore = true;
            }

            private bool IsCommitting { get; set; }
            public void CommitChangesToBackingStore()
            {
                //Avoid reentrancy issues.
                if(IsCommitting)
                {
                    return;
                }
                IsCommitting = true;
                using(new DisposeAction(() => IsCommitting = false))//Reset IsCommitting to false once we are done committing.
                {
                    if(ScheduledForAdding)
                    {
                        IsInBackingStore = true;
                        _backingStore.Add(Key.Id, Document);
                    }
                    else if(ScheduledForRemoval)
                    {
                        var docType = Document.GetType();
                        Document = null;
                        IsInBackingStore = false;
                        _backingStore.Remove(Key.Id, docType);

                    }
                    else if(ScheduledForUpdate)
                    {
                        _backingStore.Update(Seq.Create(new KeyValuePair<string, object>(Key.Id, Document)));
                    }
                }
            }
        }

    }
}
