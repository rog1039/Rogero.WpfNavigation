using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Rogero.WpfNavigation.EnumerableTrees;

/// <summary>
/// Adapts a DependencyObject to provide methods required for generate
/// a Linq To Tree API
/// </summary>
public class VisualTreeAdapter : ILinqTree<DependencyObject>
{
    private DependencyObject _item;

    public VisualTreeAdapter(DependencyObject item)
    {
        _item = item;
    }

    public IEnumerable<DependencyObject> Children()
    {
        if (_item is Visual || _item is Visual3D)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(_item);
            for (int i = 0; i < childrenCount; i++)
            {
                yield return VisualTreeHelper.GetChild(_item, i);
            }
        }
    }

    public DependencyObject Parent
    {
        get { return VisualTreeHelper.GetParent(_item); }
    }
}

//Started working on this, not sure worth the effort.
// public class WpfTreeAdapter : ILinqTree<object>
// {
//     private object _item;
//
//     public WpfTreeAdapter(object item)
//     {
//         _item = item;
//     }
//
//     public IEnumerable<object> Children()
//     {
//         switch (_item)
//         {
//             case Visual:
//             case Visual3D:
//             {
//                 var visualLike    = _item as DependencyObject;
//                 var childrenCount = VisualTreeHelper.GetChildrenCount(visualLike);
//                 for (int i = 0; i < childrenCount; i++)
//                 {
//                     yield return VisualTreeHelper.GetChild(visualLike, i);
//                 }
//
//                 break;
//                 _:
//                 {
//                     if (_item is DependencyObject dependencyObject)
//                     {
//                         
//                     }  
//                 break;
//                 }
//             }
//         }
//
//         if (_item is Visual || _item is Visual3D)
//         {
//             int childrenCount = VisualTreeHelper.GetChildrenCount(_item);
//             for (int i = 0; i < childrenCount; i++)
//             {
//                 yield return VisualTreeHelper.GetChild(_item, i);
//             }
//         }
//     }
//
//     public object Parent { get; }
// }

public static class WpfTreeHelpers
{
    public static IEnumerable<object> WalkDownLogicalTree(this object item, bool includeItem = false)
    {
        if(includeItem) yield return item;
        if (item is DependencyObject dependencyObject)
        {
            foreach (var logicalChild in LogicalTreeHelper.GetChildren(dependencyObject))
            {
                yield return logicalChild;
                yield return WalkDownLogicalTree(logicalChild, false);
            }
        }
    }
}