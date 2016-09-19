using System.Threading.Tasks;

namespace Rogero.WpfNavigation
{
    public interface ICanDeactivate
    {
        Task<bool> CanDeactivate(string uri, object initData);
    }
}