using System.Windows.Media;

namespace CarPartManager.Models
{
    public sealed class HomeTile
    {
        public NavItem NavigationItem { get; }
        public string Description { get; }
        public bool IsProminent { get; }
        public double Width => IsProminent ? 484 : 236;
        public double Height => IsProminent ? 200 : 152;
        public Brush AccentBrush { get; }
        public Brush AccentLightBrush { get; }

        public HomeTile(NavItem navigationItem, string description, bool isProminent, Brush accentBrush, Brush accentLightBrush)
        {
            NavigationItem = navigationItem;
            Description = description;
            IsProminent = isProminent;
            AccentBrush = accentBrush;
            AccentLightBrush = accentLightBrush;
        }
    }

    public sealed class HomeTileSection
    {
        public string Title { get; }
        public Brush AccentBrush { get; }
        public Brush AccentLightBrush { get; }
        public IReadOnlyList<HomeTile> Tiles { get; }

        public HomeTileSection(string title, Brush accentBrush, Brush accentLightBrush, IReadOnlyList<HomeTile> tiles)
        {
            Title = title;
            AccentBrush = accentBrush;
            AccentLightBrush = accentLightBrush;
            Tiles = tiles;
        }
    }
}
