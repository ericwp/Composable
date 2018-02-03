﻿using System;
using System.IO;
using System.Linq;

// ReSharper disable ForCanBeConvertedToForeach optimization is important in this file. It is really the whole purpose of it :)

namespace Composable.Serialization
{
    abstract partial class BinarySerializedObject<TInheritor> : IBinarySerializeMySelf<TInheritor>
        where TInheritor : BinarySerializedObject<TInheritor>, new()
    {
        static MemberGetterSetter[] _memberGetterSetters;
        static MemberGetterSetter[] _memberGetterSettersReversed;

        readonly TInheritor _this;

        protected BinarySerializedObject() => _this = (TInheritor)this;

        protected static void InitGetterSetters(params MemberGetterSetter[] getterSetters)
        {
            if(_memberGetterSetters != null)
            {
                throw new InvalidOperationException($"You can only call {nameof(InitGetterSetters)} once");
            }

            _memberGetterSetters = getterSetters.ToArray();
            _memberGetterSettersReversed = getterSetters.Reverse().ToArray();
        }

        public void Deserialize(BinaryReader reader)
        {
            for(var index = 0; index < _memberGetterSetters.Length; index++)
            {
                _memberGetterSettersReversed[index].Deserialize(_this, reader);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            for(int index = 0; index < _memberGetterSettersReversed.Length; index++)
            {
                _memberGetterSettersReversed[index].Serialize(_this, writer);
            }
        }

        internal byte[] Serialize()
        {
            //Optimization to know the size of the buffer to use from the start so we don't need to reallocate or to use ToArray. We can just return the existing buffer we created.
            using(var stream = new MemoryStream())
            {
                using(var binaryWriter = new BinaryWriter(stream))
                {
                    Serialize(binaryWriter);
                }

                return stream.ToArray();
            }
        }

        internal static TInheritor Deserialize(byte[] data)
        {
            using(var reader = new BinaryReader(new MemoryStream(data)))
            {
                var instance = new TInheritor();
                instance.Deserialize(reader);
                return instance;
            }
        }

        protected abstract class MemberGetterSetter
        {
            internal abstract void Serialize(TInheritor inheritor, BinaryWriter writer);
            internal abstract void Deserialize(TInheritor inheritor, BinaryReader reader);
        }

        protected abstract class MemberGetterSetter<TValue> : MemberGetterSetter
        {
            protected readonly Func<TInheritor, TValue> Getter;
            protected readonly Action<TInheritor, TValue> Setter;
            protected MemberGetterSetter(Func<TInheritor, TValue> getter, Action<TInheritor, TValue> setter)
            {
                Getter = getter;
                Setter = setter;
            }
        }
    }
}
