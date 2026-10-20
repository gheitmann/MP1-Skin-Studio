using System;
using System.Collections.Generic;

namespace MP1SkinStudio
{
    public sealed partial class PreviewState
    {
        Dictionary<SkinControl,bool?> visibilityPass;
        // The caller must inspect a coherent state without firing mutation callbacks.
        // Only completed top-level results are reused. Recursive evaluation keeps
        // its own cycle detection, work budget and occurrence-specific defines.
        internal IDisposable BeginVisibilityPass(){return new VisibilityPass(this);}
        sealed class VisibilityPass : IDisposable
        {
            PreviewState owner;readonly Dictionary<SkinControl,bool?> previous;
            public VisibilityPass(PreviewState state)
            {owner=state;previous=state.visibilityPass;state.visibilityPass=new Dictionary<SkinControl,bool?>();}
            public void Dispose(){if(owner==null)return;owner.visibilityPass=previous;owner=null;}
        }
    }
}
