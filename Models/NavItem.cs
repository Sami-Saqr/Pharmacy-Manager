namespace CarPartManager.Models
{
    public class NavItem
    {
        public string Tag { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IconPathData { get; set; } = string.Empty;

        public NavItem(string tag, string title, string iconPathData)
        {
            Tag = tag;
            Title = title;
            IconPathData = iconPathData;
        }
    }
}
