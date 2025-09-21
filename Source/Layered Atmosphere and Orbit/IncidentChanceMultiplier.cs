using RimWorld;
using System.Xml;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class IncidentChanceMultiplier
    {
        public IncidentDef incident;

        public float multiplier;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "incident", xmlRoot);
            multiplier = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }
}

