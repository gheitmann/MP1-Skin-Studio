using System.Xml;
namespace MP1SkinStudio.LiveBridge
{
    public interface ILiveSession
    {XmlDocument Handle(XmlDocument request);void Reset();}
}
