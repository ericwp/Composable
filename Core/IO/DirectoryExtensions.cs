using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Void.Hierarchies;
using Void.Linq;

namespace Void.IO
{
    /// <summary/>
    public static class DirectoryExtensions
    {
        /// <summary>
        /// Called on <paramref name="path"/> return a DirectoryInfo instance 
        /// pointed at that path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DirectoryInfo AsDirectory(this string path)
        {
            Contract.Requires(!string.IsNullOrEmpty(path));
            Contract.Requires(path.Union(Path.GetInvalidPathChars()).None());
            Contract.Ensures(Contract.Result<DirectoryInfo>() != null);
            return new DirectoryInfo(path);
        }


        /// <summary>
        /// Returns the size of the directory.
        /// </summary>
        public static long Size(this DirectoryInfo me)
        {
            Contract.Requires(me != null && me.FullName != null);
            return me.FullName
                .AsHierarchy(Directory.GetDirectories).Flatten().Unwrap()
                .SelectMany(dir => Directory.GetFiles(dir))
                .Sum(file => new FileInfo(file).Length);
        }

        /// <summary>
        /// Recursively deletes everything in a airectory and the directory itself.
        /// 
        /// A more intuitive alias for <see cref="DirectoryInfo.Delete(bool)"/>
        /// called with <paramref name="me"/> and true.
        /// </summary>
        /// <param name="me"></param>
        public static void DeleteRecursive(this DirectoryInfo me)
        {
            Contract.Requires(me != null);
            me.Delete(true);
        }
    }
}