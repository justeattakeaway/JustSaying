using System.ComponentModel;

namespace System.Threading.Tasks
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TaskExtensions
    {
        public static void ResultSync(this Task task)
            => task.GetAwaiter().GetResult();

        public static T ResultSync<T>(this Task<T> task)
            => task.GetAwaiter().GetResult();
    }
}
