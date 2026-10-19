using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MP1SkinStudio
{
    public static partial class SelfTests
    {
        static void RunCollectionInteractionTests(string root)
        {
            Test("Collection values are refreshed after preparing a page from an empty session",()=>
            {
                var p=new SkinProject(StudioForm.DemoRoot());var session=PreparedSkinSession.Create(p,"demo.collections.xml");
                Check(session.Preview.Values["#itemcount"]=="6"&&session.Preview.Values["#selectedindex"]=="1"&&session.Preview.Values["#selectedthumb"]==p.Editor.Collection.Items[0].Image,"Prepared collection values describe the previous empty page.");
                p.OpenPage("demo.xml");session.Preview.RestoreFocus();Check(!session.Preview.Values.ContainsKey("#itemcount")&&!session.Preview.Values.ContainsKey("#selectedthumb")&&session.Preview.Values["#selecteditem"]==PreviewSettings.BaseValues()["#selecteditem"],"Collection-derived values leaked onto another page.");
                p.OpenPage("demo.collections.xml");session.Preview.RestoreFocus();Check(session.Preview.Values["#itemcount"]=="6","Returning to a collection retained stale values.");
            });
            Test("Selected thumbnails and explicit sample overrides follow collection updates",()=>
            {
                var p=CollectionDemo();var state=new PreviewState(p);p.Editor.Collection.Focus=2;state.UpdateCollectionValues();Check(state.Values["#selectedthumb"]==p.Editor.Collection.Items[2].Image,"Selected image did not follow item.");
                p.Editor.Preview.Values["#selectedthumb"]="custom.png";p.Editor.Preview.Values["#selecteditem"]="Custom";state.UpdateCollectionValues();Check(state.Values["#selectedthumb"]=="custom.png"&&state.Values["#selecteditem"]=="Custom","Explicit samples were overwritten.");
                p.Editor.Preview.Values.Clear();p.Editor.Collection.Items.Clear();state.UpdateCollectionValues();Check(state.Values["#selectedthumb"]==""&&state.Values["#selectedindex"]=="0"&&state.Values["#itemcount"]=="0"&&state.Values["#selecteditem"]=="","Empty collection retained previous content.");
            });
            Test("List sample navigation updates metadata and persisted selection without editing XML",()=>
            {
                var p=CollectionDemo();var state=new PreviewState(p);string xml=ProjectFile.XmlSignature(p),initial=ProjectFile.ContentSignature(p);
                Check(p.TryMoveCollectionSelection(state,"ondown")&&p.Editor.Collection.SelectedIndex==1&&state.FocusId==50&&state.Values["#selecteditem"]==p.Editor.Collection.Items[1].Title,"Down did not move within the list.");
                Check(ProjectFile.ContentSignature(p)!=initial&&ProjectFile.XmlSignature(p)==xml&&!p.Dirty&&p.Undo.Count==0,"Sample movement was not separated from XML edits.");
                string file=Path.Combine(root,"collection-navigation.mp1skinproj");ProjectFile.Save(p,file);var loaded=ProjectFile.Load(file);var reopened=PreparedSkinSession.Create(loaded,loaded.Page);
                Check(loaded.Editor.Collection.SelectedIndex==1&&reopened.Preview.Values["#selecteditem"]==state.Values["#selecteditem"],"Saved selection/metadata was not restored.");
                Check(p.TryMoveCollectionSelection(state,"onup")&&ProjectFile.ContentSignature(p)==initial,"Returning to the original item leaves a spurious change.");
            });
            Test("Collection boundaries preserve selection and hand off to explicit control navigation",()=>
            {
                var p=CollectionDemo();string key=p.InsertControl("demo.collections.xml","button");var outside=p.Controls.First(c=>c.Key==key);p.ConnectNavigation(CollectionControl(p,"facadeview"),outside,"onup",false);var state=new PreviewState(p){FocusId=50};string signature=ProjectFile.ContentSignature(p);
                Check(!p.TryMoveCollectionSelection(state,"onup")&&p.PreviewNavigationTarget(state,"onup").Key==key&&ProjectFile.ContentSignature(p)==signature,"List boundary was consumed or selection changed.");
                p.Editor.Collection.Focus=p.Editor.Collection.Items.Count-1;signature=ProjectFile.ContentSignature(p);Check(!p.TryMoveCollectionSelection(state,"ondown")&&!p.TryMoveCollectionSelection(state,"onright")&&ProjectFile.ContentSignature(p)==signature,"List crossed its boundary or horizontal axis.");
            });
            Test("Filmstrip and coverflow arrows move horizontally and preserve focused control",()=>
            {
                var p=CollectionDemo();foreach(string mode in new[]{"filmstrip","coverflow"})
                {
                    p.Editor.Collection.Mode=mode;p.Editor.Collection.Focus=0;var state=new PreviewState(p){FocusId=50};
                    Check(p.TryMoveCollectionSelection(state,"onright")&&p.Editor.Collection.Focus==1&&state.FocusId==50,"Horizontal collection movement failed.");
                    string signature=ProjectFile.ContentSignature(p);Check(!p.TryMoveCollectionSelection(state,"ondown")&&signature==ProjectFile.ContentSignature(p),"Vertical movement changed horizontal view selection.");
                    Check(p.TryMoveCollectionSelection(state,"onleft")&&p.Editor.Collection.Focus==0&&!p.TryMoveCollectionSelection(state,"onleft"),"Horizontal boundary failed.");
                }
            });
            Test("Grid sample navigation follows actual small and large column geometry",()=>
            {
                var p=CollectionDemo();foreach(var entry in new[]{new{Mode="smallicons",Columns=4},new{Mode="largeicons",Columns=3}})
                {
                    p.Editor.Collection.Mode=entry.Mode;p.Editor.Collection.Focus=0;var state=new PreviewState(p){FocusId=50};
                    Check(p.TryMoveCollectionSelection(state,"ondown")&&p.Editor.Collection.Focus==entry.Columns,"Grid down ignored item width.");
                    Check(p.TryMoveCollectionSelection(state,"onup")&&p.Editor.Collection.Focus==0&&!p.TryMoveCollectionSelection(state,"onleft"),"Grid up/left edge failed.");
                    p.Editor.Collection.Focus=entry.Columns-1;Check(!p.TryMoveCollectionSelection(state,"onright"),"Grid wrapped horizontally into another row.");
                    p.Editor.Collection.Focus=5;Check(!p.TryMoveCollectionSelection(state,"ondown"),"Grid invented missing cells below the last item.");
                }
            });
            Test("Hidden, unfocused, empty and ambiguous collections reject sample movement without mutation",()=>
            {
                var p=CollectionDemo();var state=new PreviewState(p){FocusId=777};string signature=ProjectFile.ContentSignature(p);Check(!p.TryMoveCollectionSelection(state,"ondown")&&signature==ProjectFile.ContentSignature(p),"Unfocused collection moved.");
                state.FocusId=50;p.Editor.Hidden.Add(CollectionControl(p,"facadeview").Key);signature=ProjectFile.ContentSignature(p);Check(!p.TryMoveCollectionSelection(state,"ondown")&&signature==ProjectFile.ContentSignature(p),"Editor-hidden collection moved.");p.Editor.Hidden.Clear();
                p.Editor.Collection.Items.Clear();signature=ProjectFile.ContentSignature(p);Check(!p.TryMoveCollectionSelection(state,"ondown")&&signature==ProjectFile.ContentSignature(p),"Empty collection moved.");
                var ambiguous=AuditFixture(root,"collection-ambiguous","<control><type>listcontrol</type><id>50</id></control><control><type>listcontrol</type><id>50</id></control>");state=new PreviewState(ambiguous){FocusId=50};signature=ProjectFile.ContentSignature(ambiguous);Check(!ambiguous.TryMoveCollectionSelection(state,"ondown")&&signature==ProjectFile.ContentSignature(ambiguous),"Ambiguous collection moved.");
                Reject(()=>p.TryMoveCollectionSelection(state,"ondown"));Reject(()=>p.TryMoveCollectionSelection(new PreviewState(p),"diagonal"));
            });
            Test("Standalone icon and playlist controls can receive preview focus and navigate samples",()=>
            {
                foreach(string type in new[]{"thumbnailpanel","playlistcontrol"})
                {
                    var p=AuditFixture(root,"collection-focus-"+type,"<control><type>"+type+"</type><id>50</id><width>500</width><height>500</height></control>");Check(SkinProject.Focusable(p.Controls[0]),"Collection type is not focusable.");
                    var state=new PreviewState(p){FocusId=50};Check(p.TryMoveCollectionSelection(state,type=="thumbnailpanel"?"onright":"ondown")&&p.Editor.Collection.Focus==1,"Standalone collection does not navigate.");
                }
            });
            Test("List geometry includes the last fitting row without a trailing gap",()=>
            {
                var p=CollectionDemo();var list=CollectionControl(p,"listcontrol");var tiles=CollectionGeometry.Tiles(list,new RectangleF(0,0,1100,180),p.Editor.Collection);
                Check(tiles.Length==2&&tiles[1].Bounds.Bottom==180,"A complete final row was discarded because of a trailing gap.");Check(tiles[0].Detail.Bottom==tiles[0].Bounds.Bottom,"Secondary text used the primary text offset height.");
            });
            Test("Every selected coverflow item remains visible in narrow viewports",()=>
            {
                var p=CollectionDemo();var flow=CollectionControl(p,"coverflow");foreach(float width in new[]{50f,290f,580f,1100f})for(int index=0;index<p.Editor.Collection.Items.Count;index++)
                {
                    p.Editor.Collection.Focus=index;var viewport=new RectangleF(0,0,width,610);var selected=CollectionGeometry.Tiles(flow,viewport,p.Editor.Collection).Single(t=>t.Selected);Check(selected.Index==index&&selected.Bounds.IntersectsWith(viewport),"Selected coverflow card disappeared.");
                }
            });
            Test("Filmstrip scrolling keeps successive selections in view without page jumps",()=>
            {
                var p=CollectionDemo();var film=CollectionControl(p,"filmstrip");p.Editor.Collection.Focus=4;var tiles=CollectionGeometry.Tiles(film,new RectangleF(0,0,810,510),p.Editor.Collection);
                Check(tiles.Select(t=>t.Index).SequenceEqual(new[]{2,3,4})&&tiles.Last().Selected,"Filmstrip jumped to a new page instead of scrolling.");
            });
            Test("Collection geometry rejects nonfinite bounds and bounds imported nonfinite offsets",()=>
            {
                var p=CollectionDemo();var list=CollectionControl(p,"listcontrol");foreach(float invalid in new[]{Single.NaN,Single.PositiveInfinity,Single.NegativeInfinity})
                {
                    Check(CollectionGeometry.Tiles(list,new RectangleF(0,0,invalid,100),p.Editor.Collection).Length==0,"Nonfinite viewport produced tiles.");list.Properties["spaceBetweenItems"]=new PropertyValue(invalid.ToString(System.Globalization.CultureInfo.InvariantCulture),"",true);list.Properties["textXOff"]=list.Properties["spaceBetweenItems"];
                    var tile=CollectionGeometry.Tiles(list,new RectangleF(0,0,1100,610),p.Editor.Collection).First();Check(!Single.IsNaN(tile.Title.X)&&!Single.IsInfinity(tile.Title.X),"Nonfinite source offsets reached rendering.");
                }
                Check(CollectionGeometry.Tiles(CollectionControl(p,"thumbnailpanel"),new RectangleF(0,0,Single.MaxValue,Single.MaxValue),p.Editor.Collection).Length<=50,"Extreme grid overflowed capacity.");
            });
            Test("Collection selection focus textures follow runtime focus rather than designer selection",()=>
            {
                var p=AuditFixture(root,"collection-render-focus","<control><type>listcontrol</type><id>50</id><textureHeight>30</textureHeight><textureFocus>green.png</textureFocus><textureNoFocus>red.png</textureNoFocus></control><control><type>button</type><id>2</id></control>");Directory.CreateDirectory(Path.Combine(p.Root,"Media"));
                foreach(var pair in new[]{new{File="green.png",Color=Color.Lime},new{File="red.png",Color=Color.Red}})using(var image=new Bitmap(4,4))using(var g=Graphics.FromImage(image)){g.Clear(pair.Color);image.Save(Path.Combine(p.Root,"Media",pair.File));}
                p.Editor.Collection.Items.Clear();p.Editor.Collection.Items.Add(new PreviewItem());var state=new PreviewState(p){FocusId=50};string signature=ProjectFile.ContentSignature(p);
                using(var canvas=new SkinCanvas{Project=p,State=state,Selected=p.Controls[1]})using(var output=new Bitmap(100,40))using(var g=Graphics.FromImage(output))
                {
                    canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,100,30));Check(output.GetPixel(10,10).G>240,"Focused list did not use selected texture.");state.FocusId=2;canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,100,30));Check(output.GetPixel(10,10).R>240,"List retained focus after moving to a button.");
                }
                Check(ProjectFile.ContentSignature(p)==signature,"Rendering changed preview state.");
            });
            Test("Clean collection previews suppress missing image placeholders and empty-state text",()=>
            {
                var p=AuditFixture(root,"collection-render-clean","<control><type>thumbnailpanel</type><id>50</id><itemWidth>100</itemWidth><itemHeight>100</itemHeight><thumbWidth>80</thumbWidth><thumbHeight>80</thumbHeight><imageFolder>-</imageFolder><imageFolderFocus>-</imageFolderFocus></control>");p.Editor.Collection.Items.Clear();p.Editor.Collection.Items.Add(new PreviewItem{Image="absent.png"});var state=new PreviewState(p){FocusId=50};
                using(var canvas=new SkinCanvas{Project=p,State=state})using(var output=new Bitmap(100,100))using(var g=Graphics.FromImage(output))
                {
                    p.Editor.ShowUnresolved=false;canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,100,100));Check(output.GetPixel(30,30).A==0,"Missing thumbnail leaked into clean preview.");
                    p.Editor.ShowUnresolved=true;canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,100,100));Check(output.GetPixel(30,30).A>0,"Debug thumbnail placeholder disappeared.");
                    g.Clear(Color.Transparent);p.Editor.Collection.Items[0].Image="";canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,100,100));Check(output.GetPixel(30,30).A==0,"Explicit empty image became a placeholder.");
                    p.Editor.ShowUnresolved=false;p.Editor.Collection.Items.Clear();canvas.ControlSurfaceForTest(g,p.Controls[0],new RectangleF(0,0,100,100));Check(Enumerable.Range(0,100).All(y=>Enumerable.Range(0,100).All(x=>output.GetPixel(x,y).A==0)),"Empty clean collection displayed invented content.");
                }
            });
        }
    }
}
