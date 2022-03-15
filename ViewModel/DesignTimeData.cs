using System.Linq;

namespace ItalicPig.Bootstrap.ViewModel
{
    public static class DesignTimeData
    {
        public static ProjectCollection ProjectCollection { get; } = new ProjectCollection();
        public static Project Project => ProjectCollection.Projects.FirstOrDefault() ?? new Project("C:\\Example\\");
    }
}
