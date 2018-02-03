using System.IO;

namespace Composable.System.Threading
{
    interface IBinarySerializeMySelf
    {
        void Deserialize(BinaryReader reader);
        void Serialize(BinaryWriter writer);
    }

    interface IBinarySerializeMySelf<TInheritor> : IBinarySerializeMySelf where TInheritor : new()
    {

    }
}