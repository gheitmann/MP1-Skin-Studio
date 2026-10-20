using System;
namespace MP1SkinStudio
{
    // Navigation links remain editable XML. Arrow preview must first respect
    // controls that can consume that direction before base navigation runs.
    public static class PreviewControlActions
    {
        public static bool Limited(SkinControl control)
        {return control.Get("type")=="slider"||control.Get("type")=="spincontrol";}
        public static void RequireNavigation(SkinControl control,string direction)
        {
            string type=control.Get("type");
            if(type=="slider"&&(direction=="onleft"||direction=="onright"))
                throw new InvalidOperationException("Slider Left/Right adjusts a runtime value. Value interaction is not simulated; focus and XML remain unchanged.");
            if(type!="spincontrol")return;
            PropertyValue property;string orientation=control.Resolved("orientation","horizontal").Trim().ToLowerInvariant();
            bool bound=control.Properties.TryGetValue("orientation",out property)&&property.Expression.Length>0;
            bool horizontal=orientation=="horizontal"||orientation=="0",vertical=orientation=="vertical"||orientation=="1";
            if(bound||(!horizontal&&!vertical))throw new InvalidOperationException("Spin-control orientation is unverified. Arrow routing cannot be determined; focus and XML remain unchanged.");
            if((horizontal&&(direction=="onleft"||direction=="onright"))||(vertical&&(direction=="onup"||direction=="ondown")))
                throw new InvalidOperationException("Spin-control arrows on this axis depend on its runtime inner-button and value state. That interaction is not simulated; focus and XML remain unchanged.");
        }
    }
}
