using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        static int CaptionColor(Color color){return color.R|(color.G<<8)|(color.B<<16);}
        void ApplyWindowAppearance()
        {
            if(!IsHandleCreated)return;
            try
            {
                // Documented Windows 11 attributes; older systems reject them
                // without affecting native move/resize/snap or caption buttons.
                int dark=1,background=CaptionColor(panel),text=CaptionColor(ink),border=CaptionColor(panel);
                if(SystemInformation.HighContrast){dark=0;background=text=border=-1;}
                DwmSetWindowAttribute(Handle,20,ref dark,4);
                DwmSetWindowAttribute(Handle,35,ref background,4);
                DwmSetWindowAttribute(Handle,36,ref text,4);
                DwmSetWindowAttribute(Handle,34,ref border,4);
            }
            catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
        }
        protected override void OnHandleCreated(EventArgs e)
        {base.OnHandleCreated(e);ApplyWindowAppearance();}
        protected override void OnSystemColorsChanged(EventArgs e)
        {base.OnSystemColorsChanged(e);ApplyWindowAppearance();}
    }
}
