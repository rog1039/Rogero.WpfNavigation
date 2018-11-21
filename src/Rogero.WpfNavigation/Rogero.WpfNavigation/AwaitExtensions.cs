using System.Threading.Tasks;

namespace Rogero.WpfNavigation
{
    public static class AwaitExtensions
    {
        public static async Task AwaitIfNecessary(this object item)
        {
            if (item is Task task)
            {
                await task;
            }
        }
    }
}