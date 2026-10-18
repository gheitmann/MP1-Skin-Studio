using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed partial class StudioForm
    {
        Button alignButton,duplicateButton;
        CheckBox navigation;
        ChoiceBox navDirection=new ChoiceBox();
        readonly string[] directionKeys={"onleft","onright","onup","ondown"};
        SkinControl[] SelectedControls()
        {return project==null?new SkinControl[0]:project.Controls.Where(c=>canvas.SelectedKeys.Contains(c.Key)).GroupBy(c=>c.Key).Select(g=>g.First()).ToArray();}
        Panel BuildDesignerBar()
        {
            var host=new Panel{Dock=DockStyle.Top,Height=40};
            var commands=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false,Padding=new Padding(8,1,0,0)};
            Button insert=null;insert=ButtonFor("Insert ▾",()=>ShowInsertMenu(insert),true);insert.Width=102;commands.Controls.Add(insert);
            duplicateButton=ButtonFor("Duplicate",DuplicateElement);duplicateButton.Width=102;commands.Controls.Add(duplicateButton);
            alignButton=ButtonFor("Align ▾",()=>ShowAlignMenu(alignButton));alignButton.Width=108;commands.Controls.Add(alignButton);
            var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=40,WrapContents=false,Visible=false,Padding=new Padding(8,4,0,0)};
            navigation=Check("Navigation",false,value=>
            {
                canvas.NavigationMode=value;host.Height=value?80:40;row.Visible=value;
                if(value&&selected!=null)SelectControl(selected);
                canvas.Focus();canvas.Invalidate();
                if(value)ShowNotice("The selection is the source. Choose a direction and click a target; use arrow keys to test connections.");
            });commands.Controls.Add(navigation);
            row.Controls.Add(new Label{Text="Direction",ForeColor=muted,Width=64,Height=27,TextAlign=ContentAlignment.MiddleLeft});
            navDirection.Width=100;navDirection.Height=27;navDirection.BackColor=Color.FromArgb(20,26,35);navDirection.ForeColor=ink;
            navDirection.Items.AddRange(new object[]{"Left","Right","Up","Down"});navDirection.SelectedIndex=1;row.Controls.Add(navDirection);
            row.Controls.Add(new Label{Text="Click target · Test with arrows",ForeColor=muted,Width=230,Height=27,TextAlign=ContentAlignment.MiddleLeft});
            host.Controls.Add(row);host.Controls.Add(commands);
            tips.SetToolTip(alignButton,"Use CTRL to select multiple elements; the entire action is one undo step.");
            tips.SetToolTip(duplicateButton,"Copy one control with a new available ID and an offset position.");
            return host;
        }
        void UpdateSelectionTools()
        {
            UpdateProtectionTools();
            if(alignButton!=null)alignButton.Enabled=canvas.SelectedKeys.Count>=2;
            if(duplicateButton!=null)duplicateButton.Enabled=canvas.SelectedKeys.Count==1;
            foreach(TreeNode leaf in TreeControlNodes())
            {var c=leaf.Tag as SkinControl;leaf.BackColor=c!=null&&canvas.SelectedKeys.Contains(c.Key)?Color.FromArgb(33,72,102):panel;}
        }
        void ShowInsertMenu(Control anchor)
        {
            if(project==null)return;
            var menu=StudioMenus.For(this,"insert");string source=selected==null?project.Relative(project.Resolve(project.Page)):selected.Source.RelativePath;
            menu.Items.Add("Target file: "+source).Enabled=false;
            foreach(var pair in new[]{new[]{"label","Text"},new[]{"button","Button"},new[]{"image","Image"}})
            {string kind=pair[0];menu.Items.Add(pair[1],null,(s,e)=>Attempt(()=>InsertElement(kind)));}
            menu.Show(anchor,new Point(0,anchor.Height));
        }
        void InsertElement(string kind)
        {
            if(project==null)return;
            string source=selected==null?project.Relative(project.Resolve(project.Page)):selected.Source.RelativePath;
            string key=project.InsertControl(source,kind);canvas.SelectedKeys.Clear();RefreshProject(key);
            ShowNotice("New element in "+source+" inserted. Shared includes affect every page using them.");
        }
        void DuplicateElement()
        {
            if(selected==null||SelectedControls().Length!=1)throw new InvalidOperationException("Select exactly one element to duplicate.");
            string key=project.DuplicateControl(selected,unlock.Checked);canvas.SelectedKeys.Clear();RefreshProject(key);
            ShowNotice("Copy inserted with a new ID; existing IDs and original text are preserved.");
        }
        void ShowAlignMenu(Control anchor)
        {
            var menu=StudioMenus.For(this,"align");
            string[] modes={"left","hcenter","right","top","vcenter","bottom","hspace","vspace"};
            string[] names={"Left edges","Center horizontally","Right edges","Top edges","Center vertically","Bottom edges","Equal horizontal spacing","Equal vertical spacing"};
            for(int i=0;i<modes.Length;i++){string mode=modes[i];menu.Items.Add(names[i],null,(s,e)=>Attempt(()=>AlignSelection(mode)));}
            menu.Show(anchor,new Point(0,anchor.Height));
        }
        void AlignSelection(string mode)
        {
            if(selected==null)return;string key=selected.Key;
            if(SelectedControls().Any(c=>project.IsLocked(c)))throw new InvalidOperationException("The selection contains a locked element. Unlock it in the element tree first.");
            var edits=LayoutTools.Align(SelectedControls(),mode,unlock.Checked);project.EditMany(edits,"Align / distribute selection");RefreshProject(key);
        }
        void ConnectTo(SkinControl target)
        {
            if(selected==null)throw new InvalidOperationException("Select a source in the element tree first.");
            string key=selected.Key,direction=directionKeys[Math.Max(0,navDirection.SelectedIndex)];
            project.ConnectNavigation(selected,target,direction,unlock.Checked);RefreshProject(key);canvas.Focus();
            ShowNotice("Connected: "+selected.Name+" · "+direction+" → "+target.Name+" (ID "+target.Get("id")+").");
        }
        void FollowNavigation(string direction)
        {
            if(selected==null)return;var target=project.NavigationTarget(selected,direction);
            SelectControl(target);canvas.Focus();ShowNotice("Focus test: "+direction+" → "+target.Name+" (ID "+target.Get("id")+").");
        }
        public void PrepareNavigationPreview()
        {var source=project.Controls.FirstOrDefault(c=>c.Get("description")=="Movies");if(source!=null)SelectControl(source);navigation.Checked=true;}
        public void PrepareMultiPreview()
        {SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));SelectControl(project.Controls.First(c=>c.Get("description")=="Series"),true,false);SelectControl(project.Controls.First(c=>c.Get("description")=="Music"),true,false);}
        void RunDesignerUiSmoke(Action<bool,string> verify)
        {
            var film=project.Controls.First(c=>c.Get("description")=="Movies");var series=project.Controls.First(c=>c.Get("description")=="Series");
            SelectControl(film);SelectControl(series,true,false);
            verify(canvas.SelectedKeys.Count==2&&fields["posX"].Text==""&&alignButton.Enabled,"Multiple selection shows mixed values and enables alignment");
            canvas.Update();canvas.ExerciseDrag(selected,60,40,false);
            verify(project.Controls.First(c=>c.Get("description")=="Movies").Get("posX")=="200"&&project.Controls.First(c=>c.Get("description")=="Series").Get("posX")=="780"&&project.Undo.Count==1,"Canvas mouse events move the entire selection in one transaction");
            undo.PerformClick();verify(!project.Dirty&&canvas.SelectedKeys.Count==2,"One undo restores the entire multiple selection");
            fields["width"].Text="500";Apply();verify(SelectedControls().All(c=>c.Get("width")=="500"),"Shared properties apply to every selected control");undo.PerformClick();
            fields["label"].Text="temporary";fields["label"].Text="";Apply();verify(SelectedControls().All(c=>c.Get("label")==""),"Deliberately clearing a mixed text field clears all selected labels");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Series"));fields["posY"].Text="650";Apply();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));SelectControl(project.Controls.First(c=>c.Get("description")=="Series"),true,false);
            AlignSelection("top");verify(SelectedControls().All(c=>c.Get("posY")=="580"),"Alignment corrects mixed positions");undo.PerformClick();undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));int count=project.Controls.Count;
            InsertElement("button");verify(project.Controls.Count==count+1&&selected.Get("type")=="button"&&selected.Get("id")!="10","Insertion selects the new control with its own ID");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));DuplicateElement();
            verify(project.Controls.Count==count+1&&selected.Name=="Movies (Copy)"&&selected.Node.Child("editorUnknown")!=null,"Duplication preserves extensions and selects the copy");undo.PerformClick();
            PrepareNavigationPreview();navDirection.SelectedIndex=1;Application.DoEvents();canvas.Update();
            canvas.ExerciseConnection(project.Controls.First(c=>c.Get("description")=="Music"));
            verify(selected.Get("description")=="Movies"&&selected.Get("onright")=="12","Navigation mode connects to targets through canvas mouse events");undo.PerformClick();
            canvas.ExerciseFollow(Keys.Right);verify(selected.Get("id")=="11","Arrow keys test the remote-control path to the right");
            canvas.ExerciseFollow(Keys.Left);verify(selected.Get("id")=="10","Arrow keys test the return remote-control path");navigation.Checked=false;
            project.Edit(selected,new Dictionary<string,string>{{"posX","#test.binding"}},"Binding for validation");RefreshProject(selected.Key);
            SelectControl(project.Controls.First(c=>c.Get("description")=="Movies"));SelectControl(project.Controls.First(c=>c.Get("description")=="Series"),true,false);
            int steps=project.Undo.Count;canvas.Update();canvas.ExerciseDrag(selected,60,40,false);
            verify(project.Undo.Count==steps&&project.Controls.First(c=>c.Get("description")=="Series").Get("posX")=="720","A protected binding blocks the entire shared drag");undo.PerformClick();
            verify(!project.Dirty,"New designer actions can be fully undone to the original");
        }
    }
}
