using System;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        // Preview sample navigation only; XML neighbor links are consulted at boundaries.
        public bool TryMoveCollectionSelection(PreviewState state,string direction)
        {
            if(state==null||state.Project!=this)throw new InvalidOperationException("Preview state belongs to another project.");
            if(!new[]{"onleft","onright","onup","ondown"}.Contains(direction))throw new InvalidOperationException("Invalid navigation direction.");
            var focused=LogicalControls().Where(c=>ControlId(c)==state.FocusId&&Focusable(c)).ToArray();
            if(focused.Length!=1||!CollectionSettings.IsCollection(focused[0]))return false;
            var control=focused[0];
            if(control.Get("type")=="facadeview")control=CollectionSettings.Template(this,control,Editor.Collection.Mode);
            if(control==null||IsEditorHidden(control)||state.Visible(control)!=true)return false;
            var settings=Editor.Collection;settings.Validate();int index=settings.SelectedIndex;if(index<0)return false;
            string kind=CollectionSettings.Kind(control,settings.Mode);int next=index;
            if(kind=="list"||kind=="album"||kind=="playlist")
            {if(direction=="onup")next--;else if(direction=="ondown")next++;else return false;}
            else if(kind=="filmstrip"||kind=="coverflow")
            {if(direction=="onleft")next--;else if(direction=="onright")next++;else return false;}
            else if(kind=="smallicons"||kind=="largeicons")
            {
                int columns=CollectionGeometry.Columns(control,new StackPreview(this,state).Bounds(control),settings);if(columns==0)return false;
                if(direction=="onup")next-=columns;else if(direction=="ondown")next+=columns;
                else if(direction=="onleft"){if(index%columns==0)return false;next--;}
                else{if(index%columns==columns-1)return false;next++;}
            }
            else return false;
            if(next<0||next>=settings.Items.Count)return false;
            settings.Focus=next;state.UpdateCollectionValues();return true;
        }
    }
}
