using System;
using System.Collections.Generic;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        public SkinControl LogicalControl(SkinControl c)
        {
            for(var owner=c.RuntimeParent;owner!=null;owner=owner.RuntimeParent)
                if(owner.Get("type")=="facadeview"&&ControlId(owner)==ControlId(c)&&ControlId(c).HasValue)return owner;
            return c;
        }
        public SkinControl[] LogicalControls(){return Controls.Select(LogicalControl).Distinct().ToArray();}
    }
}
