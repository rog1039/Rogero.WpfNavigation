using System.Threading.Tasks;

namespace Rogero.WpfNavigation
{
    public interface CanActivate
    {
        Task<bool> CanActivate(string uri, object initData);
    }
}