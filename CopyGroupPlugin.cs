using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroupPlugin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                //Экземпляр класса фильтра выбора
                GroupPickFilter groupPickFilter = new GroupPickFilter();
                //Получение ссылки на объект
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите копируемую группу элементов");
                //Обращение к объекту по ссылке
                Element element = doc.GetElement(reference);
                Group group = element as Group;

                //Определение центра группы
                XYZ groupCenter = GetElementCenter(group);
                //Определение центра комнаты, где находится исходная группа объектов
                Room baseRoom = GetRoomByPoint(doc, groupCenter);
                XYZ baseRoomCenter = GetElementCenter(baseRoom);

                //Определение смещения центра группы относительно центра комнаты
                XYZ offset = groupCenter - baseRoomCenter;

                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                //Определение целевой комнаты
                Room targetRoom = GetRoomByPoint(doc, point);
                XYZ targetRoomCenter = GetElementCenter(targetRoom);

                //Точка вставки
                XYZ insertionPoint = targetRoomCenter + offset;

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(insertionPoint, group.GroupType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                                                        .OfCategory(BuiltInCategory.OST_Rooms);

            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (e != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }

            return null;
        }
    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
