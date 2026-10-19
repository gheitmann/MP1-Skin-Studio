using System;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        // A bounded subset of MP1's geometric fallback: direct linear stack buttons.
        SkinControl StackNavigationTarget(PreviewState state,SkinControl source,string direction)
        {
            var parent=source.RuntimeParent;StackPreview.Rule rule;
            if(parent==null||!source.Get("type").Contains("button")||!StackPreview.TryRead(parent.Resolved("layout"),out rule))throw new InvalidOperationException("Automatic navigation is supported only for direct buttons in a StackLayout.");
            bool forward=direction==(rule.Horizontal?"onright":"ondown"),backward=direction==(rule.Horizontal?"onleft":"onup");
            if(!forward&&!backward)throw new InvalidOperationException("No explicit navigation target across the stack axis.");
            var siblings=Controls.Where(c=>c.RuntimeParent==parent).ToArray();int index=Array.IndexOf(siblings,source),step=forward?1:-1;
            if(index<0)throw new InvalidOperationException("The stack source is missing.");
            for(int i=index+step;i>=0&&i<siblings.Length;i+=step)
            {
                var target=siblings[i];if(LayoutTools.IsContainer(target))throw new InvalidOperationException("Automatic navigation through nested groups is not supported.");
                if(!Focusable(target))continue;
                if(!target.Get("type").Contains("button")||ControlId(target)<=0||!ControlId(target).HasValue||LogicalControls().Count(c=>ControlId(c)==ControlId(target))!=1)throw new InvalidOperationException("The automatic navigation target is unsupported or ambiguous.");
                int before=state.FocusId;bool? visible;
                try{state.FocusId=ControlId(target).Value;visible=state.Visible(target);}finally{state.FocusId=before;}
                if(visible==null)throw new InvalidOperationException("Automatic navigation encounters unverified visibility. Apply suitable sample data first.");
                if(visible==false||IsEditorHidden(target))continue;return target;
            }
            return source;
        }
    }
}
