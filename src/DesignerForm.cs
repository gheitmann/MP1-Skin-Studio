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
            Button insert=null;insert=ButtonFor("Einfügen ▾",()=>ShowInsertMenu(insert),true);insert.Width=102;commands.Controls.Add(insert);
            duplicateButton=ButtonFor("Duplizieren",DuplicateElement);duplicateButton.Width=102;commands.Controls.Add(duplicateButton);
            alignButton=ButtonFor("Ausrichten ▾",()=>ShowAlignMenu(alignButton));alignButton.Width=108;commands.Controls.Add(alignButton);
            var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=40,WrapContents=false,Visible=false,Padding=new Padding(8,4,0,0)};
            navigation=Check("Navigation",false,value=>
            {
                canvas.NavigationMode=value;host.Height=value?80:40;row.Visible=value;
                if(value&&selected!=null)SelectControl(selected);
                canvas.Focus();canvas.Invalidate();
                if(value)ShowNotice("Quelle ist die Auswahl. Richtung wählen und Ziel anklicken; Pfeiltasten testen die Verbindungen.");
            });commands.Controls.Add(navigation);
            row.Controls.Add(new Label{Text="Richtung",ForeColor=muted,Width=64,Height=27,TextAlign=ContentAlignment.MiddleLeft});
            navDirection.Width=100;navDirection.Height=27;navDirection.BackColor=Color.FromArgb(20,26,35);navDirection.ForeColor=ink;
            navDirection.Items.AddRange(new object[]{"Links","Rechts","Oben","Unten"});navDirection.SelectedIndex=1;row.Controls.Add(navDirection);
            row.Controls.Add(new Label{Text="Ziel anklicken · Pfeile testen",ForeColor=muted,Width=230,Height=27,TextAlign=ContentAlignment.MiddleLeft});
            host.Controls.Add(row);host.Controls.Add(commands);
            tips.SetToolTip(alignButton,"Mit STRG mehrere Elemente auswählen; die gesamte Aktion ist ein Rückgängig-Schritt.");
            tips.SetToolTip(duplicateButton,"Ein einzelnes Control mit neuer freier ID und versetzter Position kopieren.");
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
            menu.Items.Add("Zieldatei: "+source).Enabled=false;
            foreach(var pair in new[]{new[]{"label","Text"},new[]{"button","Schaltfläche"},new[]{"image","Bild"}})
            {string kind=pair[0];menu.Items.Add(pair[1],null,(s,e)=>Attempt(()=>InsertElement(kind)));}
            menu.Show(anchor,new Point(0,anchor.Height));
        }
        void InsertElement(string kind)
        {
            if(project==null)return;
            string source=selected==null?project.Relative(project.Resolve(project.Page)):selected.Source.RelativePath;
            string key=project.InsertControl(source,kind);canvas.SelectedKeys.Clear();RefreshProject(key);
            ShowNotice("Neues Element in "+source+" eingefügt. Bei gemeinsamen Includes wirkt es auf alle verwendenden Seiten.");
        }
        void DuplicateElement()
        {
            if(selected==null||SelectedControls().Length!=1)throw new InvalidOperationException("Zum Duplizieren genau ein Element auswählen.");
            string key=project.DuplicateControl(selected,unlock.Checked);canvas.SelectedKeys.Clear();RefreshProject(key);
            ShowNotice("Kopie mit neuer ID eingefügt; bestehende IDs und der Originaltext bleiben erhalten.");
        }
        void ShowAlignMenu(Control anchor)
        {
            var menu=StudioMenus.For(this,"align");
            string[] modes={"left","hcenter","right","top","vcenter","bottom","hspace","vspace"};
            string[] names={"Linke Kanten","Horizontal zentrieren","Rechte Kanten","Obere Kanten","Vertikal zentrieren","Untere Kanten","Gleiche horizontale Abstände","Gleiche vertikale Abstände"};
            for(int i=0;i<modes.Length;i++){string mode=modes[i];menu.Items.Add(names[i],null,(s,e)=>Attempt(()=>AlignSelection(mode)));}
            menu.Show(anchor,new Point(0,anchor.Height));
        }
        void AlignSelection(string mode)
        {
            if(selected==null)return;string key=selected.Key;
            if(SelectedControls().Any(c=>project.IsLocked(c)))throw new InvalidOperationException("Die Auswahl enthält ein gesperrtes Element. Zuerst im Elementbaum entsperren.");
            var edits=LayoutTools.Align(SelectedControls(),mode,unlock.Checked);project.EditMany(edits,"Auswahl ausrichten / verteilen");RefreshProject(key);
        }
        void ConnectTo(SkinControl target)
        {
            if(selected==null)throw new InvalidOperationException("Zuerst im Elementbaum eine Quelle auswählen.");
            string key=selected.Key,direction=directionKeys[Math.Max(0,navDirection.SelectedIndex)];
            project.ConnectNavigation(selected,target,direction,unlock.Checked);RefreshProject(key);canvas.Focus();
            ShowNotice("Verbunden: "+selected.Name+" · "+direction+" → "+target.Name+" (ID "+target.Get("id")+").");
        }
        void FollowNavigation(string direction)
        {
            if(selected==null)return;var target=project.NavigationTarget(selected,direction);
            SelectControl(target);canvas.Focus();ShowNotice("Fokus-Test: "+direction+" → "+target.Name+" (ID "+target.Get("id")+").");
        }
        public void PrepareNavigationPreview()
        {var source=project.Controls.FirstOrDefault(c=>c.Get("description")=="Filme");if(source!=null)SelectControl(source);navigation.Checked=true;}
        public void PrepareMultiPreview()
        {SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));SelectControl(project.Controls.First(c=>c.Get("description")=="Serien"),true,false);SelectControl(project.Controls.First(c=>c.Get("description")=="Musik"),true,false);}
        void RunDesignerUiSmoke(Action<bool,string> verify)
        {
            var film=project.Controls.First(c=>c.Get("description")=="Filme");var series=project.Controls.First(c=>c.Get("description")=="Serien");
            SelectControl(film);SelectControl(series,true,false);
            verify(canvas.SelectedKeys.Count==2&&fields["posX"].Text==""&&alignButton.Enabled,"Mehrfachauswahl zeigt gemischte Werte und aktiviert Ausrichten");
            canvas.Update();canvas.ExerciseDrag(selected,60,40,false);
            verify(project.Controls.First(c=>c.Get("description")=="Filme").Get("posX")=="200"&&project.Controls.First(c=>c.Get("description")=="Serien").Get("posX")=="780"&&project.Undo.Count==1,"Canvas-Mausereignisse verschieben gesamte Auswahl in einer Transaktion");
            undo.PerformClick();verify(!project.Dirty&&canvas.SelectedKeys.Count==2,"Ein Undo stellt gesamte Mehrfachauswahl wieder her");
            fields["width"].Text="500";Apply();verify(SelectedControls().All(c=>c.Get("width")=="500"),"Gemeinsame Eigenschaften gelten für alle ausgewählten Controls");undo.PerformClick();
            fields["label"].Text="vorübergehend";fields["label"].Text="";Apply();verify(SelectedControls().All(c=>c.Get("label")==""),"Bewusst geleertes gemischtes Textfeld leert alle gewählten Beschriftungen");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Serien"));fields["posY"].Text="650";Apply();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));SelectControl(project.Controls.First(c=>c.Get("description")=="Serien"),true,false);
            AlignSelection("top");verify(SelectedControls().All(c=>c.Get("posY")=="580"),"Ausrichten-Aktion korrigiert gemischte Positionen");undo.PerformClick();undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));int count=project.Controls.Count;
            InsertElement("button");verify(project.Controls.Count==count+1&&selected.Get("type")=="button"&&selected.Get("id")!="10","Einfügen wählt neues Control mit eigener ID aus");undo.PerformClick();
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));DuplicateElement();
            verify(project.Controls.Count==count+1&&selected.Name=="Filme (Kopie)"&&selected.Node.Child("editorUnknown")!=null,"Duplizieren erhält Erweiterungen und wählt Kopie aus");undo.PerformClick();
            PrepareNavigationPreview();navDirection.SelectedIndex=1;Application.DoEvents();canvas.Update();
            canvas.ExerciseConnection(project.Controls.First(c=>c.Get("description")=="Musik"));
            verify(selected.Get("description")=="Filme"&&selected.Get("onright")=="12","Navigationsmodus verbindet per Canvas-Mausereignis zum Ziel");undo.PerformClick();
            canvas.ExerciseFollow(Keys.Right);verify(selected.Get("id")=="11","Pfeiltaste testet Fernbedienungsweg nach rechts");
            canvas.ExerciseFollow(Keys.Left);verify(selected.Get("id")=="10","Pfeiltaste testet Fernbedienungsweg zurück");navigation.Checked=false;
            project.Edit(selected,new Dictionary<string,string>{{"posX","#test.binding"}},"Bindung für Prüfung");RefreshProject(selected.Key);
            SelectControl(project.Controls.First(c=>c.Get("description")=="Filme"));SelectControl(project.Controls.First(c=>c.Get("description")=="Serien"),true,false);
            int steps=project.Undo.Count;canvas.Update();canvas.ExerciseDrag(selected,60,40,false);
            verify(project.Undo.Count==steps&&project.Controls.First(c=>c.Get("description")=="Serien").Get("posX")=="720","Eine geschützte Bindung blockiert den gesamten gemeinsamen Drag");undo.PerformClick();
            verify(!project.Dirty,"Neue Designer-Aktionen lassen sich vollständig auf Original zurücksetzen");
        }
    }
}
