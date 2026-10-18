using System;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        public SkinControl[] ContainedLeaves(SkinControl group)
        {
            RequireCurrent(group);
            return Controls.Where(c=>c.Source==group.Source&&c.Node!=group.Node&&c.Node.Start>group.Node.Start&&c.Node.End<group.Node.End&&!LayoutTools.IsContainer(c)).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();
        }
    }
}
