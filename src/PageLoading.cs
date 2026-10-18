using System;
using System.Collections.Generic;
using System.Linq;

namespace MP1SkinStudio
{
    public sealed partial class SkinProject
    {
        static void RestorePageMap<T>(Dictionary<string,T> target,KeyValuePair<string,T>[] values)
        {target.Clear();foreach(var pair in values)target.Add(pair.Key,pair.Value);}
        public void OpenPage(string page)
        {
            // Page loading reads sources but must not discard a working session on failure.
            string oldPage=Page;int oldWidth=Width,oldHeight=Height,oldWindow=WindowId,oldFocus=DefaultControlId;
            var oldControls=Controls.ToArray();var oldWarnings=Warnings.ToArray();var oldSources=Sources.ToArray();
            var oldDefaults=defaults.ToArray();var oldStyles=styles.ToArray();var oldDefines=globalDefines.ToArray();
            var oldFontSizes=FontSizes.ToArray();var oldFontNames=FontNames.ToArray();var oldFontSpecs=FontSpecs.ToArray();
            try{OpenPageCore(page);}
            catch
            {
                Page=oldPage;Width=oldWidth;Height=oldHeight;WindowId=oldWindow;DefaultControlId=oldFocus;
                Controls.Clear();Controls.AddRange(oldControls);Warnings.Clear();Warnings.AddRange(oldWarnings);
                RestorePageMap(Sources,oldSources);RestorePageMap(defaults,oldDefaults);RestorePageMap(styles,oldStyles);RestorePageMap(globalDefines,oldDefines);
                RestorePageMap(FontSizes,oldFontSizes);FontNames.Clear();FontNames.AddRange(oldFontNames);RestorePageMap(FontSpecs,oldFontSpecs);
                throw;
            }
        }
    }
}
