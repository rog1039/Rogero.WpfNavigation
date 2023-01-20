using System.Collections.Concurrent;
using System.Windows;
using Optional;
using Optional.Collections;

namespace Rogero.WpfNavigation;

public interface IRouteEntry
{
   string Name          { get; }
   string Uri           { get; }
   Type   ViewModelType { get; }
   Type   ViewType      { get; }

   UIElement CreateView();
   object    CreateViewModel();
}

public interface IRouteEntryRegistry
{
   Guid                Id { get; }
   void                RegisterRouteEntry(IRouteEntry routeEntry);
   Option<IRouteEntry> GetRouteEntry(string           uri);
   IList<IRouteEntry>  GetRouteEntries();
}

public class RouteEntryRegistry : IRouteEntryRegistry
{
   public Guid Id { get; } = Guid.NewGuid();

   private readonly IDictionary<string, IRouteEntry> _routeEntries = new ConcurrentDictionary<string, IRouteEntry>();

   public RouteEntryRegistry() { }

   public Option<IRouteEntry> GetRouteEntry(string uri) => _routeEntries.GetValueOrNone(uri);

   public IList<IRouteEntry> GetRouteEntries()
   {
      return _routeEntries.Values.ToList();
   }

   public void RegisterRouteEntry(IRouteEntry routeEntry)
   {
      try
      {
         var existingRouteEntryOption = _routeEntries.GetValueOrNone(routeEntry.Uri);
         existingRouteEntryOption
            .Match(some =>
                   {
                      if (some.Name != routeEntry.Name
                       || some.ViewType != routeEntry.ViewType
                       || some.ViewModelType != routeEntry.ViewModelType)
                      {
                         var entries = new List<IRouteEntry> {some, routeEntry};
                         throw new Exception(@$"Duplicate route entry detected. First is existing, second is new route:
{entries.ToStringTable()}
");
                      }
                   },
                   () =>
                   {
                      //
                      _routeEntries.Add(routeEntry.Uri, routeEntry);
                   });
      }
      catch (Exception e)
      {
         Console.WriteLine(e);
         throw;
      }
   }
}