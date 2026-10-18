using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace MP1SkinStudio
{
    public static partial class ProjectFile
    {
        static int[] SnapshotControlOrder(SkinSource original,int editedCount,XmlElement file)
        {
            int count=original.Elements.Count(n=>n.Name=="control");int[] order;
            if(!file.HasAttribute("controlOrder"))
            {
                if(file.OwnerDocument.DocumentElement.GetAttribute("version")!="1")throw new InvalidDataException("Elementzuordnung fehlt im Projektformat 2/3/4.");
                // Older editor versions only appended controls; they did not
                // reorder or delete them. Preserve that known compatibility.
                if(editedCount<count)throw new InvalidDataException("Strukturelle Änderung ohne Elementzuordnung im Projekt.");
                order=Enumerable.Range(0,count).Concat(Enumerable.Repeat(-1,editedCount-count)).ToArray();
            }
            else
            {
                string value=file.GetAttribute("controlOrder");var parts=value.Length==0?new string[0]:value.Split(',');order=new int[parts.Length];
                for(int index=0;index<parts.Length;index++)if(!Int32.TryParse(parts[index],out order[index]))throw new InvalidDataException("Ungültige Elementzuordnung im Projekt.");
            }
            if(order.Length!=editedCount||order.Any(i=>i< -1||i>=count)||order.Where(i=>i>=0).Distinct().Count()!=order.Count(i=>i>=0))throw new InvalidDataException("Elementzuordnung passt nicht zum XML-Schnappschuss.");return order;
        }
    }
}
