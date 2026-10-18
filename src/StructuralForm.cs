using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MP1SkinStudio
{
    public sealed class DeleteControlsDialog : Form
    {
        readonly CheckBox references=new CheckBox(),shared=new CheckBox();readonly Button remove=new Button();
        readonly DeletionPlan plan;
        public bool AllowReferences{get{return references.Checked;}}
        public bool AllowShared{get{return shared.Checked;}}
        public DeleteControlsDialog(DeletionPlan value)
        {
            plan=value;Text="Elemente entfernen — MP1 Skin Studio";Size=new Size(1100,650);MinimumSize=new Size(900,570);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(26,33,44);ForeColor=Color.FromArgb(224,235,245);
            var top=new TextBox{Dock=DockStyle.Top,Height=130,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=BackColor,ForeColor=ForeColor,BorderStyle=BorderStyle.None};
            top.Text="Entfernen: "+plan.Roots.Length+" ausgewählte Elemente / Gruppen, insgesamt "+plan.RemovedKeys.Count+" Controls.\r\n"+String.Join("\r\n",plan.Roots.Select(c=>c.Name+" — "+c.Source.RelativePath))+"\r\n\r\n"+plan.BoundControls+" Controls enthalten Bindungen; sie werden mit dem Element entfernt. Originaldateien bleiben unverändert. Die Aktion ist vollständig rückgängig zu machen.";
            var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,HideSelection=false,BackColor=Color.FromArgb(18,24,33),ForeColor=ForeColor};list.Columns.Add("Quelle",210);list.Columns.Add("Verweisendes Element",180);list.Columns.Add("Eigenschaft",140);list.Columns.Add("ID",70);list.Columns.Add("Vorhandener Verweis",430);
            foreach(var item in plan.References)list.Items.Add(new ListViewItem(new[]{item.Source,item.Name,item.Property,item.TargetId.ToString(),item.Value}));
            var approvals=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=142,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(10,6,6,0)};
            approvals.Controls.Add(new Label{Text=plan.References.Count+" mögliche Verweise in der geöffneten Seite samt geladenen Includes. Andere Seiten und dynamische Verweise werden nicht vollständig geprüft.",Width=1020,Height=39});
            references.Text="Fundstellen geprüft: vorhandene Verweise beim Entfernen unverändert stehen lassen";references.AutoSize=true;references.Visible=plan.References.Count>0;references.CheckedChanged+=(s,e)=>UpdateRemove();approvals.Controls.Add(references);
            shared.Text="Aus eingebundenen Quelldateien entfernen; die Änderung gilt für alle Verwendungen";shared.AutoSize=true;shared.Visible=plan.SharedFiles.Length>0;shared.CheckedChanged+=(s,e)=>UpdateRemove();approvals.Controls.Add(shared);
            approvals.Controls.Add(new Label{Text=plan.SharedFiles.Length==0?"Die Quelldatei kann auch von anderen Seiten verwendet werden. Vor Einsatz im vollständigen Skin prüfen.":"Eingebundene Quellen: "+String.Join(", ",plan.SharedFiles),Width=1020,Height=38});
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=53,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,8,12,8)};remove.Text="Auswahl entfernen";remove.Width=160;remove.Height=33;remove.FlatStyle=FlatStyle.Flat;remove.BackColor=Color.FromArgb(145,62,57);remove.ForeColor=ForeColor;remove.Click+=(s,e)=>AcceptRemoval();bottom.Controls.Add(remove);var cancel=new Button{Text="Abbrechen",Width=112,Height=33,FlatStyle=FlatStyle.Flat,ForeColor=ForeColor,DialogResult=DialogResult.Cancel};bottom.Controls.Add(cancel);CancelButton=cancel;
            Controls.Add(list);Controls.Add(approvals);Controls.Add(bottom);Controls.Add(top);UpdateRemove();
        }
        void UpdateRemove(){remove.Enabled=(plan.References.Count==0||references.Checked)&&(plan.SharedFiles.Length==0||shared.Checked);}
        void AcceptRemoval(){if(!remove.Enabled)return;DialogResult=DialogResult.OK;Close();}
        internal bool RemovalEnabled{get{return remove.Enabled;}}
        internal void ApproveForTest(){references.Checked=true;shared.Checked=true;}
        internal void AcceptForTest(){AcceptRemoval();}
        public void RenderTo(string path)
        {Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
    }
    public sealed partial class StudioForm
    {
        bool PendingProperties(){return fields.Any(pair=>originals.ContainsKey(pair.Key)&&(pair.Value.Text!=originals[pair.Key]||(mixedFields.Contains(pair.Key)&&touchedFields.Contains(pair.Key))));}
        EditorSelection CaptureSelection(){return new EditorSelection{Primary=selected==null?null:selected.Key,Keys=canvas.SelectedKeys.ToArray()};}
        EditorSelection TranslateSelection(EditorSelection before)
        {
            Func<string,string> map=key=>{string next;return key!=null&&project.LastKeyTranslation.TryGetValue(key,out next)?next:key;};
            var keys=before.Keys.Select(map).Where(k=>k!=null&&project.Controls.Any(c=>c.Key==k)).Distinct().ToArray();string primary=map(before.Primary);
            if(primary==null||!project.Controls.Any(c=>c.Key==primary))primary=keys.FirstOrDefault();
            if(primary==null&&before.Primary!=null)
            {
                int split=before.Primary.LastIndexOf('|'),ordinal;if(split>0&&Int32.TryParse(before.Primary.Substring(split+1),out ordinal))
                {var same=project.Controls.Where(c=>String.Equals(c.Source.RelativePath,before.Primary.Substring(0,split),StringComparison.OrdinalIgnoreCase)).ToArray();var next=same.FirstOrDefault(c=>c.Ordinal>=ordinal)??same.LastOrDefault();if(next!=null)primary=next.Key;}
            }
            if(keys.Length==0&&primary!=null)keys=new[]{primary};return new EditorSelection{Primary=primary,Keys=keys};
        }
        void RestoreSelection(EditorSelection snapshot)
        {canvas.SelectedKeys.Clear();foreach(string key in snapshot.Keys)if(project.Controls.Any(c=>c.Key==key))canvas.SelectedKeys.Add(key);RefreshProject(snapshot.Primary??snapshot.Keys.FirstOrDefault());}
        void UndoEditor(bool repeat)
        {
            var stack=repeat?project.Redo:project.Undo;if(stack.Count==0)return;var change=stack.Peek();var before=CaptureSelection();
            if(repeat)project.RedoChange();else project.UndoChange();
            RestoreSelection((repeat?change.SelectionAfter:change.SelectionBefore)??TranslateSelection(before));
        }
        bool StructuralAction(Action action)
        {
            if(PendingProperties())throw new InvalidOperationException("Andere Eigenschaften haben noch nicht übernommene Eingaben. Diese zuerst übernehmen.");
            var before=CaptureSelection();int steps=project.Undo.Count;action();if(project.Undo.Count==steps){ShowNotice("Die Ebene liegt bereits an dieser Grenze ihres XML-Bereichs.");return false;}
            var change=project.Undo.Peek();change.SelectionBefore=before;RestoreSelection(TranslateSelection(before));change.SelectionAfter=CaptureSelection();
            return true;
        }
        void ReorderSelection(string mode)
        {
            if(SelectedControls().Length!=1)throw new InvalidOperationException("Für die Ebenenreihenfolge genau ein Control oder eine Gruppe auswählen.");
            if(StructuralAction(()=>project.ReorderControl(selected,mode)))ShowNotice("Ebenenreihenfolge geändert. Die Auswahl sowie Sperren und Ausblendungen folgen dem Control.");
        }
        void DeleteSelection()
        {
            if(PendingProperties())throw new InvalidOperationException("Andere Eigenschaften haben noch nicht übernommene Eingaben. Diese zuerst übernehmen.");
            var controls=SelectedControls();var plan=project.InspectDeletion(controls);
            using(var dialog=new DeleteControlsDialog(plan))if(dialog.ShowDialog(this)==DialogResult.OK)StructuralAction(()=>project.DeleteControls(controls,dialog.AllowReferences,dialog.AllowShared));
        }
        void AddStructureCommands(ContextMenuStrip menu)
        {
            bool any=SelectedControls().Length>0,single=SelectedControls().Length==1,locked=SelectedControls().Any(c=>project.IsLocked(c));menu.Items.Add(new ToolStripSeparator());
            string[] modes={"forward","backward","front","back"},names={"Eine Ebene nach vorn","Eine Ebene nach hinten","Ganz nach vorn","Ganz nach hinten"};
            for(int index=0;index<modes.Length;index++){string mode=modes[index];menu.Items.Add(names[index],null,(s,e)=>Attempt(()=>ReorderSelection(mode))).Enabled=single&&!locked;}
            menu.Items.Add("Auswahl entfernen …",null,(s,e)=>Attempt(DeleteSelection)).Enabled=any&&!locked;
        }
        public void RenderDeletionTo(string path){using(var dialog=new DeleteControlsDialog(project.InspectDeletion(SelectedControls())))dialog.RenderTo(path);}
        public void PrepareLayersPreview(){LoadProject(DemoRoot(),"demo.ebenen.xml");SelectControl(project.Controls.First(c=>c.Get("description")=="Ebene A"));}
    }
}
