using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rogero.WpfNavigation
{
    public interface IViewAware
    {
        Task LoadView(object view);
    }
}
