using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakScale
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class TechUpdater : MonoBehaviour
    {
        public void Start()
        {
            Tech.Reload();
        }
    }

    public static class Tech
    {
        private static HashSet<string> _unlockedTechs = new HashSet<string>();

        public static void Reload()
        {
            if (HighLogic.CurrentGame == null)
                return;
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return;

            var persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
            var config = ConfigNode.Load(persistentfile);
            var gameconf = config.GetNode("GAME");
            var scenarios = gameconf.GetNodes("SCENARIO");
            var thisScenario = scenarios.FirstOrDefault(a => a.GetValue("name") == "ResearchAndDevelopment");
            if (thisScenario == null)
                return;
            var techs = thisScenario.GetNodes("Tech");

            _unlockedTechs = techs.Select(a => a.GetValue("id")).ToHashSet();
            _unlockedTechs.Add("");
        }

        public static bool IsUnlocked(string techId)
        {
            if (HighLogic.CurrentGame == null)
                return true;
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return true;
            return techId == "" || _unlockedTechs.Contains(techId);
        }
    }

    /// <summary>
    /// Configuration values for TweakScale.
    /// </summary>
    public class ScaleType
    {
        /// <summary>
        /// Fetches the scale ScaleType with the specified name.
        /// </summary>
        /// <param name="name">The name of the ScaleType to fetch.</param>
        /// <returns>The specified ScaleType or the default ScaleType if none exists by that name.</returns>
        /*private static ScaleType GetScaleConfig(string name)
        {
            var config = GameDatabase.Instance.GetConfigs("SCALETYPE").FirstOrDefault(a => a.name == name);
            if (config == null)
            {
                Tools.LogWf("No SCALETYPE with name {0}", name);
            }
            return config; // == null ? DefaultScaleType : new ScaleType(config.config);
        }*/

        public class NodeInfo
        {
            public readonly string Family;
            public readonly float Scale;

            private NodeInfo()
            {
            }

            public NodeInfo(string family, float scale) : this()
            {

                Family = family;
                Scale = scale;
                if (Mathf.Abs(Scale) < 0.01)
                {
                    Tools.LogWf("Invalid scale for family {0}: {1}", family, scale);
                }
            }

            public NodeInfo(string s) : this()
            {
                var parts = s.Split(':');
                if (parts.Length == 1)
                {
                    if (!float.TryParse(parts[0], out Scale))
                        Tools.LogWf("Invalid attachment node string \"{0}\"", s);
                    return;
                }
                if (parts.Length == 0)
                {
                    return;
                }
                if (!float.TryParse(parts[1], out Scale))
                {
                    Tools.LogWf("Invalid attachment node string \"{0}\"", s);
                    return;
                }
                Family = parts[0];
                if (Mathf.Abs(Scale) < 0.01)
                {
                    Tools.LogWf("Invalid scale for family {0}: {1}", Family, Scale);
                }
            }

            public override string ToString()
            {
                return string.Format("({0}, {1})", Family, Scale);
            }
        }

        private static List<ScaleType> _scaleTypes;
        public static List<ScaleType> AllScaleTypes
        {
            get {
                return _scaleTypes = _scaleTypes ??
                        (GameDatabase.Instance.GetConfigs("SCALETYPE")
                            .Select(a => new ScaleType(a.config))
                            .ToList<ScaleType>());
            }
        }

        //private static readonly ScaleType DefaultScaleType = new ScaleType();

        private float[] _scaleFactors = {};
        private readonly string[] _scaleNames = {};
        public readonly Dictionary<string, ScaleExponents> Exponents = new Dictionary<string, ScaleExponents>();

        public readonly bool IsFreeScale = true;
        public readonly string[] TechRequired = {};
        //public readonly Dictionary<string, NodeInfo> AttachNodes = new Dictionary<string, NodeInfo>();
        //public readonly float MinValue = 0f;
        //public readonly float MaxValue = 0f;
        public float DefaultScale = -1;
        public float[] IncrementSlide = {};
        public string Suffix = null;
        public readonly string Name = null;
        public readonly string Family;
        /*public float BaseScale {
            get { return AttachNodes["base"].Scale; }
        }*/

        public float[] AllScaleFactors
        {
            get
            {
                return _scaleFactors;
            }
        }

        public float[] ScaleFactors
        {
            get
            {
                if (TechRequired.Length == 0)
                    return _scaleFactors;
                var result = _scaleFactors.ZipFilter(TechRequired, Tech.IsUnlocked).ToArray();
                return result;
            }
        }

        public string[] ScaleNames
        {
            get
            {
                if (TechRequired.Length == 0)
                    return _scaleNames;

                var result = _scaleNames.ZipFilter(TechRequired, Tech.IsUnlocked).ToArray();
                return result;
            }
        }

        public int[] ScaleNodes { get; private set; }

        private ScaleType()
        {
            ScaleNodes = new int[] {};
            //AttachNodes = new Dictionary<string, NodeInfo>();
            //AttachNodes["base"] = new NodeInfo("", 1);
        }

        // config is a part config
        public ScaleType(ConfigNode partConfig)
        {
            ConfigNode scaleConfig = null;
            if ((object)partConfig != null )
            {
                Name = Tools.ConfigValue(partConfig, "type", Name);
                ScaleExponents.LoadGlobalExponents();

                if (Name != null)
                {
                    var tmp = GameDatabase.Instance.GetConfigs("SCALETYPE").FirstOrDefault(a => a.name == Name);
                    if (tmp != null) scaleConfig = tmp.config;
                    if (scaleConfig != null)
                    {
                        // search scaletype for values
                        IsFreeScale = Tools.ConfigValue(scaleConfig, "freeScale", IsFreeScale);
                        DefaultScale = Tools.ConfigValue(scaleConfig, "defaultScale", DefaultScale);
                        Suffix = Tools.ConfigValue(scaleConfig, "suffix", Suffix);
                        _scaleFactors = Tools.ConfigValue(scaleConfig, "scaleFactors", _scaleFactors);
                        ScaleNodes = Tools.ConfigValue(scaleConfig, "scaleNodes", ScaleNodes);         // currently not used!
                        _scaleNames = Tools.ConfigValue(scaleConfig, "scaleNames", _scaleNames).Select(a => a.Trim()).ToArray();
                        TechRequired = Tools.ConfigValue(scaleConfig, "techRequired", TechRequired).Select(a => a.Trim()).ToArray();
                        Family = Tools.ConfigValue(scaleConfig, "family", "default");
                        //AttachNodes   = GetNodeFactors(scaleConfig.GetNode("ATTACHNODES"), AttachNodes);  // currently not used!
                        IncrementSlide = Tools.ConfigValue(scaleConfig, "incrementSlide", IncrementSlide); // deprecated!

                        Exponents = ScaleExponents.CreateExponentsForModule(scaleConfig, Exponents);
                        //Debug.Log("[TweakScale] scaleConfig:" + scaleConfig.ToString());
                        //Debug.Log("[TweakScale] scaleConfig:" + this.ToString());
                        //Debug.Log("[TweakScale]" + Exponents.ToString());
                    }
                }
                else
                    Name = "";

                // search part config for overrides
                IsFreeScale   = Tools.ConfigValue(partConfig, "freeScale",    IsFreeScale);
                DefaultScale  = Tools.ConfigValue(partConfig, "defaultScale", DefaultScale);
                Suffix        = Tools.ConfigValue(partConfig, "suffix",       Suffix);
                _scaleFactors = Tools.ConfigValue(partConfig, "scaleFactors", _scaleFactors);
                ScaleNodes    = Tools.ConfigValue(partConfig, "scaleNodes",   ScaleNodes);
                _scaleNames   = Tools.ConfigValue(partConfig, "scaleNames",   _scaleNames).Select(a => a.Trim()).ToArray();
                TechRequired  = Tools.ConfigValue(partConfig, "techRequired", TechRequired).Select(a=>a.Trim()).ToArray();
                Family        = Tools.ConfigValue(partConfig, "family",       "default");
                //AttachNodes   = GetNodeFactors(partConfig.GetNode("ATTACHNODES"), AttachNodes);
                IncrementSlide= Tools.ConfigValue(partConfig, "incrementSlide", IncrementSlide);

                Exponents = ScaleExponents.CreateExponentsForModule(partConfig, Exponents);
                ScaleExponents.treatMassAndCost(Exponents);

//                string log = "finished ScaleExponents: ";
//                foreach(var e in Exponents) { log += e.ToString() + ", \n"; }
//                Debug.Log(log);

                //Debug.Log("[TweakScale] partConfig:" + partConfig.ToString());
                //Debug.Log("[TweakScale] partConfig:" + this.ToString());
                //Debug.Log("[TweakScale]" + Exponents.ToString());
            }

            if (IsFreeScale && (_scaleFactors.Length > 1))
            {
                bool error = false;
                for (int i=0; i<_scaleFactors.Length-1; i++)
                    if (_scaleFactors[i + 1] <= _scaleFactors[i])
                        error = true;

                if (error)
                {
                    Tools.LogWf("scaleFactors must be in ascending order! \n{0}", this.ToString());
                    _scaleFactors = new float[0];
                }
            }

            // fill in missing values
            if ((DefaultScale <= 0) || (_scaleFactors.Length == 0))
                RepairScaletype(scaleConfig, partConfig);

            if (!IsFreeScale && (_scaleFactors.Length != _scaleNames.Length))
            {
                if(_scaleNames.Length != 0)
                    Tools.LogWf("Wrong number of scaleFactors compared to scaleNames in scaleType \"{0}\": {1} scaleFactors vs {2} scaleNames\n{3}", Name, _scaleFactors.Length, _scaleNames.Length, this.ToString());

                _scaleNames = new string[_scaleFactors.Length];
                for (int i=0; i<_scaleFactors.Length; i++)
                    _scaleNames[i] = _scaleFactors[i].ToString();
            }

            if (!IsFreeScale)
            {
                DefaultScale = Tools.Closest(DefaultScale, AllScaleFactors);
            }
            DefaultScale = Tools.Clamp(DefaultScale, _scaleFactors.Min(), _scaleFactors.Max());

            if (IncrementSlide.Length == 0)
            {
                IncrementSlide = new float[_scaleFactors.Length-1];
                for (var i=0; i<_scaleFactors.Length-1; i++)
                    IncrementSlide[i] = (_scaleFactors[i+1]-_scaleFactors[i])/50f;
            }

            if (IsFreeScale)
            {
                // workaround for stock bug in tweakable UI_ScaleEdit:
                // add a tiny dummy interval to the range because the highest one is bugged
                var tmp = _scaleFactors;
                _scaleFactors = new float[tmp.Length + 1];
                for (int i = 0; i < tmp.Length; i++)
                    _scaleFactors[i] = tmp[i];

                _scaleFactors[tmp.Length] = _scaleFactors[tmp.Length - 1] + 0.1f * IncrementSlide.Max();
            }

            var numTechs = TechRequired.Length;
            if ((numTechs > 0) && (numTechs != _scaleFactors.Length))
            {
                //Tools.LogWf("Wrong number of techRequired compared to scaleFactors in scaleType \"{0}\": {1} scaleFactors vs {2} techRequired", Name, _scaleFactors.Length, TechRequired.Length);
                if (numTechs < _scaleFactors.Length)
                {
                    var lastTech = TechRequired[TechRequired.Length - 1];
                    TechRequired = TechRequired.Concat(lastTech.Repeat()).Take(_scaleFactors.Length).ToArray();
                }
            }

            //Debug.Log("[TweakScale] finished config:" + this.ToString());
            //Debug.Log("[TweakScale]" + Exponents.ToString());
        }

        private void RepairScaletype(ConfigNode scaleConfig, ConfigNode partConfig)
        {
            if ((DefaultScale <= 0) && (_scaleFactors.Length == 0))
            {
                DefaultScale = 100;
                if (Suffix == null)
                    Suffix = "%";
                if (IncrementSlide.Length == 0)
                    IncrementSlide = new float[] {1f, 1f, 1f, 2f, 5f}; 
            }
            if ((DefaultScale > 0) && (_scaleFactors.Length == 0))
            {
                _scaleFactors = new float[] { DefaultScale/10f, DefaultScale/4f, DefaultScale/2f, DefaultScale, DefaultScale*2f, DefaultScale*4f };
            }
            else if ((DefaultScale <= 0) && (_scaleFactors.Length > 0))
            {
                DefaultScale = _scaleFactors[0];
            }
            else
            {
                // Legacy support: min/maxValue
                float minScale = -1;
                float maxScale = -1;
                if (scaleConfig != null)
                {
                    minScale = Tools.ConfigValue(scaleConfig, "minScale", minScale);    // deprecated!
                    maxScale = Tools.ConfigValue(scaleConfig, "maxScale", maxScale);    // deprecated!
                }
                if (partConfig != null)
                {
                    minScale = Tools.ConfigValue(partConfig, "minScale", minScale);
                    maxScale = Tools.ConfigValue(partConfig, "maxScale", maxScale);
                }
                if ((minScale > 0) && (maxScale > 0))
                {
                    if (minScale > 0 && maxScale > 0)
                    {
                        if (DefaultScale > minScale && DefaultScale < maxScale)
                            _scaleFactors = new float[] { minScale, DefaultScale, maxScale };
                        else
                            _scaleFactors = new float[] { minScale, maxScale };
                    }
                }
            }
        }

        private Dictionary<string, NodeInfo> GetNodeFactors(ConfigNode node, Dictionary<string, NodeInfo> source)
        {
            var result = source.Clone();

            if (node != null)
            {
                foreach (var v in node.values.Cast<ConfigNode.Value>())
                {
                    result[v.name] = new NodeInfo(v.value);
                }
            }

            if (!result.ContainsKey("base"))
            {
                result["base"] = new NodeInfo(Family, 1.0f);
            }

            return result;
        }

        public override string ToString()
        {
            var result = "ScaleType {";
            result += "\n name = " + Name;
            result += "\n isFreeScale = " + IsFreeScale;
            result += "\n " + _scaleFactors.Length  + " scaleFactors = ";
            foreach (var s in _scaleFactors)
                result += s + "  ";
            result += "\n " + _scaleNames.Length  + " scaleNames = ";
            foreach (var s in _scaleNames)
                result += s + "  ";
            result += "\n " + IncrementSlide.Length + " incrementSlide = ";
            foreach (var s in IncrementSlide)
                result += s + "  ";
            result += "\n " + TechRequired.Length + " TechRequired = ";
            foreach (var s in TechRequired)
                result += s + "  ";
            result += "\n defaultScale = " + DefaultScale;
            //result += " scaleNodes = " + ScaleNodes + "\n";
            //result += "	minValue = " + MinValue + "\n";
            //result += "	maxValue = " + MaxValue + "\n";
            return result + "\n}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ScaleType) obj);
        }

        public static bool operator ==(ScaleType a, ScaleType b)
        {
            if ((object)a == null)
                return (object)b == null;
            if ((object)b == null)
                return false;
            return a.Name == b.Name;
        }

        public static bool operator !=(ScaleType a, ScaleType b)
        {
            return !(a == b);
        }

        protected bool Equals(ScaleType other)
        {
            return string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}
