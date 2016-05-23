using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Diagnostics;
using System.Xml;

namespace PPL_Model_Wrapper
{
    //--------------------------------------------------------------------------------------------
    //   Class: PPLX
    //--------------------------------------------------------------------------------------------
    public class PPLX
    {
        private Scene m_Scene = new Scene();
        public Scene Scene
        {
            set{ m_Scene = value; }
            get{ return m_Scene; }
        }

        //Write to PPLX
        public void SavePPLX(String pFullPath)
        {
            using (XmlTextWriter writer = new XmlTextWriter(pFullPath, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("PPL");
                try
                {
                    writer.WriteAttributeString("DATE", DateTime.Now.ToString());
                    writer.WriteAttributeString("USER", Environment.UserName.ToString());
                    writer.WriteAttributeString("WORKSTATION", Environment.MachineName.ToString());
                }
                catch { }
                m_Scene.SaveToXML(writer);
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
            }
        }
    }

   //--------------------------------------------------------------------------------------------
   //   Class: ValTable
   //--------------------------------------------------------------------------------------------

	public class ValTable
	{
		public ValTable()
		{
		}

		public ValTable(String pTokens)
		{
			ParseTokens(pTokens);
		}

		public class ValuePair
		{
			public ValuePair()
			{
				Position = 0;
				ValueAtPosition = 0;
			}
			public double Position { set; get; }
			public double ValueAtPosition { set; get; }
		}

		private List<ValuePair> m_DataValues = new List<ValuePair>();

		public List<ValuePair> DataValues
		{
			set
			{
				m_DataValues = value;
			}
			get
			{
				return m_DataValues;
			}
		}

		public String Label { set; get; }

		public void ParseTokens(String pTokens)
		{
			String[] toks = pTokens.Split(';');
			System.Diagnostics.Debug.Assert(toks.Length >= 2);
			try
			{
				Label = toks[0];
			}
			catch { }
			try
			{
				for (int idx = 1; idx < toks.Length; idx++)
				{
					String s = toks[idx].Trim();
					if (s == String.Empty) continue;
					try
					{
						String[] vp = s.Split(',');
						System.Diagnostics.Debug.Assert(vp.Length == 2);
						ValuePair tvp = new ValuePair();
						tvp.Position = Convert.ToDouble(vp[0]);
						tvp.ValueAtPosition = Convert.ToDouble(vp[1]);
						m_DataValues.Add(tvp);
					}
					catch { }
				}
			}
			catch { }
		}

		public String BuildTokens()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Label);
			sb.Append(';');
			foreach (ValuePair tvp in m_DataValues)
			{
				sb.Append(tvp.Position.ToString());
				sb.Append(',');
				sb.Append(tvp.ValueAtPosition.ToString());
				sb.Append(';');
			}
			return sb.ToString();
		}

		public override string ToString()
		{
			return BuildTokens();
		}
	}


   //--------------------------------------------------------------------------------------------
   //   Class: ElementBase
   // Mirrors: PPLElement
   //--------------------------------------------------------------------------------------------
   public abstract class ElementBase
   {
     string cDescriptionOverride = String.Empty;
     public string DescriptionOverride { set { cDescriptionOverride = value; } get { return cDescriptionOverride; } }
     public System.Guid m_Guid = System.Guid.NewGuid();
     public string Guid
     {
         set{m_Guid = new System.Guid(value);}
         get{return m_Guid.ToString();}
     }

     //Parent
     protected ElementBase m_Parent = null;
     public ElementBase GetParent() { return m_Parent; }

     //Children
     private List<ElementBase> m_Children = new List<ElementBase>();
     public List<ElementBase> Children
     {
         set{ m_Children = value; }
         get{ return m_Children; }
     }
     public IReadOnlyList<ElementBase> GetChildren() { return m_Children.AsReadOnly(); }
     public abstract bool IsLegalChild(ElementBase pChildCandidate);
     public void AddChild(ElementBase pChild)
     {
         if (!IsLegalChild(pChild)) throw new Exception("element is not a legal child of this type");
         m_Children.Add(pChild);
         pChild.m_Parent = this;
     }
     public void RemoveChild(ElementBase pChild)
     {
         if (!m_Children.Contains(pChild)) throw new Exception("element is not a child of this element");
         m_Children.Remove(pChild);
     }

     //Write
     public abstract string XMLkey();
     public virtual void SaveToXML(XmlTextWriter pWriter)
     {
         pWriter.WriteStartElement(XMLkey());
         pWriter.WriteStartElement("ATTRIBUTES");
         foreach (PropertyInfo fi in this.GetType().GetProperties(
             BindingFlags.Instance | BindingFlags.Public |
             BindingFlags.SetProperty | BindingFlags.GetProperty))
         {
             String sname = fi.Name.Replace("_"," ");
             if (sname == "Children") continue;
             String stype = fi.PropertyType.Name.ToString();
             String sval = fi.GetValue(this).ToString();
             if( stype.EndsWith("_val"))
             {
                sval = EnumValsList.EnumToString(sval);
                stype = "String";
             }
             pWriter.WriteStartElement("VALUE");
             pWriter.WriteAttributeString("NAME", sname);
             pWriter.WriteAttributeString("TYPE", stype);
             pWriter.WriteString(sval);
             pWriter.WriteEndElement();
         }
         pWriter.WriteEndElement();
         pWriter.WriteStartElement("PPLChildElements");
         foreach (ElementBase elem in this.m_Children)
         {
             elem.SaveToXML(pWriter);
         }
         pWriter.WriteEndElement();
         pWriter.WriteEndElement();
     }
   }



   //--------------------------------------------------------------------------------------------
   //   Class: Scene
   // Mirrors: PPLGroundLine : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Scene : ElementBase
   {

      public static string gXMLkey = "PPLScene";
      public override string XMLkey() { return gXMLkey; }

      public Scene(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_SelectedLoadCase = 0;
               m_PPLVersion = 502;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is WoodPole) return true;
         if(pChildCandidate is SteelPole) return true;
         if(pChildCandidate is ConcretePole) return true;
         if(pChildCandidate is CompositePole) return true;
         if(pChildCandidate is SegmentedPole) return true;
         if(pChildCandidate is MultiPoleStructure) return true;
         return false;
      }



        //   Attr Name:   SelectedLoadCase
        //   Attr Group:Standard
        //   Alt Display Name:
        //   Description:   SelectedLoadCase
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   INTEGER
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_SelectedLoadCase;
        [Category("Standard")]
        [Description("SelectedLoadCase")]
        public int SelectedLoadCase
        {
           get { return m_SelectedLoadCase; }
           set { m_SelectedLoadCase = value; }
        }



        //   Attr Name:   PPLVersion
        //   Attr Group:Standard
        //   Alt Display Name:
        //   Description:   PPLVersion
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   INTEGER
        //   Default Value:   502
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_PPLVersion;
        [Category("Standard")]
        [Description("PPLVersion")]
        public int PPLVersion
        {
           get { return m_PPLVersion; }
           set { m_PPLVersion = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: LoadCase
   // Mirrors: PPLEnvironment : PPLElement
   //--------------------------------------------------------------------------------------------
   public class LoadCase : ElementBase
   {

      public static string gXMLkey = "LoadCase";
      public override string XMLkey() { return gXMLkey; }

      public LoadCase(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Name = "";
               m_Method = Method_val.NESC;
               m_Deflection = Deflection_val.Linear;
               m_Fixity = Fixity_val.Fixed;
               m_Solver = Solver_val.Advanced;
               m_Algorithm = Algorithm_val.Cholesky_Decomposition;
               m_District = District_val.Medium;
               m_Radial_Ice = 0.25;
               m_Ice_Density = 0.032986;
               m_Wind_Speed = 39.53;
               m_Horiz_Wind_Pres = 4;
               m_Temperature = 65;
               m_TempMin = 32;
               m_TempMax = 212;
               m_WindType = WindType_val.WindType_2007;
               m_Construction_Grade = Construction_Grade_val.B;
               m_Crossing_Conditions = Crossing_Conditions_val.Unknown;
               m_Installation_or_Replacement = Installation_or_Replacement_val.At_Installation;
               m_Override_Wind = false;
               m_NomWindAngle = 0;
               m_Terrian_Exposure = Terrian_Exposure_val.N_A;
               m_Force_Coef = 0;
               m_Apply_FSR = false;
               m_Strength_Reduction_Factor = 1;
               m_Load_Duration_Factor = 1;
               m_Immaturity_Factor = 1;
               m_Shaving_Factor = 1;
               m_Processing_Factor = 1;
               m_Degradation_Factor = 1;
               m_Shear_Area = Shear_Area_val.Tip;
               m_ApplyCrossarmAllowance = ApplyCrossarmAllowance_val.Auto;
               m_CrossarmAllowance = 300;
               m_ApplyCableAllowance = ApplyCableAllowance_val.Auto;
               m_CableAllowance = 300;
               m_Attr_250_Rule = Attr_250_Rule_val.Rule_250B;
               m_Vertical_LF = 1.5;
               m_TransWind_LF = 2.5;
               m_TransTension_LF = 1.65;
               m_GeneralLongitude_LF = 1.1;
               m_DeadendLongitude_LF = 1.65;
               m_Guy_Vertical_LF = 1.5;
               m_Guy_TransWind_LF = 2.5;
               m_Guy_TransTension_LF = 1.65;
               m_Guy_GeneralLongitude_LF = 1.1;
               m_Guy_DeadendLongitude_LF = 1.65;
               m_Anchor_Vertical_LF = 1.5;
               m_Anchor_TransWind_LF = 2.5;
               m_Anchor_TransTension_LF = 1.65;
               m_Anchor_GeneralLongitude_LF = 1.1;
               m_Anchor_DeadendLongitude_LF = 1.65;
               m_Pole_Strength_Factor = 0.85;
               m_Manufactured_Pole_Strength_Factor = 1;
               m_Crossarm_Strength_Factor = 0.5;
               m_Guy_Strength_Factor = 0.9;
               m_Anchor_Strength_Factor = 1;
               m_PoleCapacityThreshhold = 75;
               m_Guy_Inadequate_Thresh = 0.5;
               m_Guy_At_Cap_Thresh = 2;
               m_Guy_Near_Cap_Thresh = 10;
               m_Span_Cap_Thresh = 10;
               m_BucklingConstUnguyed = 2;
               m_BucklingConstGuyed = 0.707106781186548;
               m_BucklingConstGuyedDeadend = 0.707106781186548;
               m_SectionHeightMethod = SectionHeightMethod_val.Standard;
               m_BucklingSectionPercentBCH = 0.33333333;
               m_ReportingAngleMode = ReportingAngleMode_val.Load;
               m_ReportingAngle = 1.5707963267949;
               m_SpanCapSignal = false;
               m_ArmCapSignal = false;
               m_GuyCapSignal = false;
               m_AnchorCapSignal = false;
               m_CarryCapacityUp = false;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   LoadCase name
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   Method
        //   Attr Group:Standard
        //   Alt Display Name:Code
        //   Description:   Calculation Method
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   NESC
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        GO 95  (GO 95)
        //        ASCE  (Americal Society of Civil Engineers)
        //        CSA  (Canadian Standards Association)
        //        AS/NZS 7000  (AS/NZS 7000)
        //        N/A  (No special calculations)
        public enum Method_val
        {
           [Description("NESC")]
           NESC,    //National Electrical Safety Code
           [Description("GO 95")]
           GO_95,    //GO 95
           [Description("ASCE")]
           ASCE,    //Americal Society of Civil Engineers
           [Description("CSA")]
           CSA,    //Canadian Standards Association
           [Description("AS/NZS 7000")]
           AS_NZS_7000,    //AS/NZS 7000
           [Description("N/A")]
           N_A     //No special calculations
        }
        private Method_val m_Method;
        [Category("Standard")]
        [Description("Method")]
        public Method_val Method
        {
           get
           { return m_Method; }
           set
           { m_Method = value; }
        }

        public Method_val String_to_Method_val(string pKey)
        {
           switch (pKey)
           {
                case "NESC":
                   return Method_val.NESC;    //National Electrical Safety Code
                case "GO 95":
                   return Method_val.GO_95;    //GO 95
                case "ASCE":
                   return Method_val.ASCE;    //Americal Society of Civil Engineers
                case "CSA":
                   return Method_val.CSA;    //Canadian Standards Association
                case "AS/NZS 7000":
                   return Method_val.AS_NZS_7000;    //AS/NZS 7000
                case "N/A":
                   return Method_val.N_A;    //No special calculations
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Method_val_to_String(Method_val pKey)
        {
           switch (pKey)
           {
                case Method_val.NESC:
                   return "NESC";    //National Electrical Safety Code
                case Method_val.GO_95:
                   return "GO 95";    //GO 95
                case Method_val.ASCE:
                   return "ASCE";    //Americal Society of Civil Engineers
                case Method_val.CSA:
                   return "CSA";    //Canadian Standards Association
                case Method_val.AS_NZS_7000:
                   return "AS/NZS 7000";    //AS/NZS 7000
                case Method_val.N_A:
                   return "N/A";    //No special calculations
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Deflection
        //   Attr Group:Solver
        //   Alt Display Name:Analysis Method
        //   Description:   Solution method to use
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Linear
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        1 Iteration P-Δ  (Single iteration P-Delta)
        //        2nd Order P-Δ  (Fully Converged P-Delta)
        public enum Deflection_val
        {
           [Description("Linear")]
           Linear,    //Static No P-Delta iterations performed
           [Description("1 Iteration P-Δ")]
           Deflection_1_Iteration_P_Delta,    //Single iteration P-Delta
           [Description("2nd Order P-Δ")]
           Deflection_2nd_Order_P_Delta     //Fully Converged P-Delta
        }
        private Deflection_val m_Deflection;
        [Category("Solver")]
        [Description("Deflection")]
        public Deflection_val Deflection
        {
           get
           { return m_Deflection; }
           set
           { m_Deflection = value; }
        }

        public Deflection_val String_to_Deflection_val(string pKey)
        {
           switch (pKey)
           {
                case "Linear":
                   return Deflection_val.Linear;    //Static No P-Delta iterations performed
                case "1 Iteration P-Δ":
                   return Deflection_val.Deflection_1_Iteration_P_Delta;    //Single iteration P-Delta
                case "2nd Order P-Δ":
                   return Deflection_val.Deflection_2nd_Order_P_Delta;    //Fully Converged P-Delta
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Deflection_val_to_String(Deflection_val pKey)
        {
           switch (pKey)
           {
                case Deflection_val.Linear:
                   return "Linear";    //Static No P-Delta iterations performed
                case Deflection_val.Deflection_1_Iteration_P_Delta:
                   return "1 Iteration P-Δ";    //Single iteration P-Delta
                case Deflection_val.Deflection_2nd_Order_P_Delta:
                   return "2nd Order P-Δ";    //Fully Converged P-Delta
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Fixity
        //   Attr Group:Solver
        //   Alt Display Name:Groundline Fixity
        //   Description:   Groundline fixity mode
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Fixed
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Pinned  (Pole is gimbaled at groundline for guy analysis)
        public enum Fixity_val
        {
           [Description("Fixed")]
           Fixed,    //Pole is fixed at groundline for guy analysis
           [Description("Pinned")]
           Pinned     //Pole is gimbaled at groundline for guy analysis
        }
        private Fixity_val m_Fixity;
        [Category("Solver")]
        [Description("Fixity")]
        public Fixity_val Fixity
        {
           get
           { return m_Fixity; }
           set
           { m_Fixity = value; }
        }

        public Fixity_val String_to_Fixity_val(string pKey)
        {
           switch (pKey)
           {
                case "Fixed":
                   return Fixity_val.Fixed;    //Pole is fixed at groundline for guy analysis
                case "Pinned":
                   return Fixity_val.Pinned;    //Pole is gimbaled at groundline for guy analysis
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Fixity_val_to_String(Fixity_val pKey)
        {
           switch (pKey)
           {
                case Fixity_val.Fixed:
                   return "Fixed";    //Pole is fixed at groundline for guy analysis
                case Fixity_val.Pinned:
                   return "Pinned";    //Pole is gimbaled at groundline for guy analysis
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Solver
        //   Attr Group:Solver
        //   Description:   Solver to use
        //   User Level Required:   Administrative access only
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Advanced
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Advanced  (Advanced)
        public enum Solver_val
        {
           [Description("Legacy")]
           Legacy,    //Legacy
           [Description("Advanced")]
           Advanced     //Advanced
        }
        private Solver_val m_Solver;
        [Category("Solver")]
        [Description("Solver")]
        public Solver_val Solver
        {
           get
           { return m_Solver; }
           set
           { m_Solver = value; }
        }

        public Solver_val String_to_Solver_val(string pKey)
        {
           switch (pKey)
           {
                case "Legacy":
                   return Solver_val.Legacy;    //Legacy
                case "Advanced":
                   return Solver_val.Advanced;    //Advanced
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Solver_val_to_String(Solver_val pKey)
        {
           switch (pKey)
           {
                case Solver_val.Legacy:
                   return "Legacy";    //Legacy
                case Solver_val.Advanced:
                   return "Advanced";    //Advanced
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Algorithm
        //   Attr Group:Solver
        //   Description:   Algorithm to use
        //   User Level Required:   Administrative access only
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Cholesky Decomposition
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Conjugate Gradient  (Conjugate Gradient)
        public enum Algorithm_val
        {
           [Description("Cholesky Decomposition")]
           Cholesky_Decomposition,    //Cholesky Decomposition
           [Description("Conjugate Gradient")]
           Conjugate_Gradient     //Conjugate Gradient
        }
        private Algorithm_val m_Algorithm;
        [Category("Solver")]
        [Description("Algorithm")]
        public Algorithm_val Algorithm
        {
           get
           { return m_Algorithm; }
           set
           { m_Algorithm = value; }
        }

        public Algorithm_val String_to_Algorithm_val(string pKey)
        {
           switch (pKey)
           {
                case "Cholesky Decomposition":
                   return Algorithm_val.Cholesky_Decomposition;    //Cholesky Decomposition
                case "Conjugate Gradient":
                   return Algorithm_val.Conjugate_Gradient;    //Conjugate Gradient
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Algorithm_val_to_String(Algorithm_val pKey)
        {
           switch (pKey)
           {
                case Algorithm_val.Cholesky_Decomposition:
                   return "Cholesky Decomposition";    //Cholesky Decomposition
                case Algorithm_val.Conjugate_Gradient:
                   return "Conjugate Gradient";    //Conjugate Gradient
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   District
        //   Attr Group:Ice
        //   Description:   District
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Medium
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Medium  (Medium)
        //        Medium A  (Medium A)
        //        Medium B  (Medium B)
        //        Heavy  (Heavy)
        //        Severe  (Severe)
        //        Manual  (Manual)
        //        Warm Island  (Warm Island)
        //        Special  (Special)
        //        Unset  (Unset)
        //        N/A  (N/A)
        public enum District_val
        {
           [Description("Light")]
           Light,    //Light
           [Description("Medium")]
           Medium,    //Medium
           [Description("Medium A")]
           Medium_A,    //Medium A
           [Description("Medium B")]
           Medium_B,    //Medium B
           [Description("Heavy")]
           Heavy,    //Heavy
           [Description("Severe")]
           Severe,    //Severe
           [Description("Manual")]
           Manual,    //Manual
           [Description("Warm Island")]
           Warm_Island,    //Warm Island
           [Description("Special")]
           Special,    //Special
           [Description("Unset")]
           Unset,    //Unset
           [Description("N/A")]
           N_A     //N/A
        }
        private District_val m_District;
        [Category("Ice")]
        [Description("District")]
        public District_val District
        {
           get
           { return m_District; }
           set
           { m_District = value; }
        }

        public District_val String_to_District_val(string pKey)
        {
           switch (pKey)
           {
                case "Light":
                   return District_val.Light;    //Light
                case "Medium":
                   return District_val.Medium;    //Medium
                case "Medium A":
                   return District_val.Medium_A;    //Medium A
                case "Medium B":
                   return District_val.Medium_B;    //Medium B
                case "Heavy":
                   return District_val.Heavy;    //Heavy
                case "Severe":
                   return District_val.Severe;    //Severe
                case "Manual":
                   return District_val.Manual;    //Manual
                case "Warm Island":
                   return District_val.Warm_Island;    //Warm Island
                case "Special":
                   return District_val.Special;    //Special
                case "Unset":
                   return District_val.Unset;    //Unset
                case "N/A":
                   return District_val.N_A;    //N/A
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string District_val_to_String(District_val pKey)
        {
           switch (pKey)
           {
                case District_val.Light:
                   return "Light";    //Light
                case District_val.Medium:
                   return "Medium";    //Medium
                case District_val.Medium_A:
                   return "Medium A";    //Medium A
                case District_val.Medium_B:
                   return "Medium B";    //Medium B
                case District_val.Heavy:
                   return "Heavy";    //Heavy
                case District_val.Severe:
                   return "Severe";    //Severe
                case District_val.Manual:
                   return "Manual";    //Manual
                case District_val.Warm_Island:
                   return "Warm Island";    //Warm Island
                case District_val.Special:
                   return "Special";    //Special
                case District_val.Unset:
                   return "Unset";    //Unset
                case District_val.N_A:
                   return "N/A";    //N/A
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Radial Ice
        //   Attr Group:Ice
        //   Alt Display Name:Radial Ice (in)
        //   Description:   Radial Thickness of Ice
        //   Displayed Units:   store as INCHES display as INCHES or MILLIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.25
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Radial_Ice;
        [Category("Ice")]
        [Description("Radial Ice")]
        public double Radial_Ice
        {
           get { return m_Radial_Ice; }
           set { m_Radial_Ice = value; }
        }



        //   Attr Name:   Ice Density
        //   Attr Group:Ice
        //   Alt Display Name:Ice Density (lb/ft^3)
        //   Description:   Ice Density in lbs per cubic inch
        //   Displayed Units:   store as POUNDS PER CUBIC INCH display as POUNDS PER CUBIC FOOT or KILOGRAMS PER CUBIC METER
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.032986
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Ice_Density;
        [Category("Ice")]
        [Description("Ice Density")]
        public double Ice_Density
        {
           get { return m_Ice_Density; }
           set { m_Ice_Density = value; }
        }



        //   Attr Name:   Wind Speed
        //   Attr Group:Wind
        //   Alt Display Name:Wind Speed (mph)
        //   Description:   NESC Basic Wind Speed at 33 Feet / ASCE Fastest Wind
        //   Displayed Units:   store as MILES PER HOUR display as MILES PER HOUR or KILOMETERS PER HOUR
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   39.53
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Wind_Speed;
        [Category("Wind")]
        [Description("Wind Speed")]
        public double Wind_Speed
        {
           get { return m_Wind_Speed; }
           set { m_Wind_Speed = value; }
        }



        //   Attr Name:   Horiz Wind Pres
        //   Attr Group:Wind
        //   Alt Display Name:Wind Pressure (lb/ft^2)
        //   Description:   Horizontal Wind Pressure in lbs per square foot
        //   Displayed Units:   store as PSF display as PSF or PASCAL
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   4
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Horiz_Wind_Pres;
        [Category("Wind")]
        [Description("Horiz Wind Pres")]
        public double Horiz_Wind_Pres
        {
           get { return m_Horiz_Wind_Pres; }
           set { m_Horiz_Wind_Pres = value; }
        }



        //   Attr Name:   Temperature
        //   Attr Group:Temperature
        //   Alt Display Name:Temperature (°f)
        //   Description:   Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Temperature;
        [Category("Temperature")]
        [Description("Temperature")]
        public double Temperature
        {
           get { return m_Temperature; }
           set { m_Temperature = value; }
        }



        //   Attr Name:   TempMin
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Min (°f)
        //   Description:   Minimum Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   32
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TempMin;
        [Category("Temperature")]
        [Description("TempMin")]
        public double TempMin
        {
           get { return m_TempMin; }
           set { m_TempMin = value; }
        }



        //   Attr Name:   TempMax
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Max (°f)
        //   Description:   Maximum Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   212
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TempMax;
        [Category("Temperature")]
        [Description("TempMax")]
        public double TempMax
        {
           get { return m_TempMax; }
           set { m_TempMax = value; }
        }



        //   Attr Name:   WindType
        //   Attr Group:Wind
        //   Alt Display Name:NESC Standard
        //   Description:   Wind calculation type
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   2007
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        2002  (2002)
        //        2007  (2007)
        //        2012  (2012)
        //        N/A  (N/A)
        public enum WindType_val
        {
           [Description("1997")]
           WindType_1997,    //1997
           [Description("2002")]
           WindType_2002,    //2002
           [Description("2007")]
           WindType_2007,    //2007
           [Description("2012")]
           WindType_2012,    //2012
           [Description("N/A")]
           N_A     //N/A
        }
        private WindType_val m_WindType;
        [Category("Wind")]
        [Description("WindType")]
        public WindType_val WindType
        {
           get
           { return m_WindType; }
           set
           { m_WindType = value; }
        }

        public WindType_val String_to_WindType_val(string pKey)
        {
           switch (pKey)
           {
                case "1997":
                   return WindType_val.WindType_1997;    //1997
                case "2002":
                   return WindType_val.WindType_2002;    //2002
                case "2007":
                   return WindType_val.WindType_2007;    //2007
                case "2012":
                   return WindType_val.WindType_2012;    //2012
                case "N/A":
                   return WindType_val.N_A;    //N/A
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string WindType_val_to_String(WindType_val pKey)
        {
           switch (pKey)
           {
                case WindType_val.WindType_1997:
                   return "1997";    //1997
                case WindType_val.WindType_2002:
                   return "2002";    //2002
                case WindType_val.WindType_2007:
                   return "2007";    //2007
                case WindType_val.WindType_2012:
                   return "2012";    //2012
                case WindType_val.N_A:
                   return "N/A";    //N/A
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Construction Grade
        //   Attr Group:Standard
        //   Description:   Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   B
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        F  (F)
        //        B  (B)
        //        C  (C)
        //        1  (1)
        //        2  (2)
        //        3  (3)
        //        N/A  (N/A)
        public enum Construction_Grade_val
        {
           [Description("A")]
           A,    //A
           [Description("F")]
           F,    //F
           [Description("B")]
           B,    //B
           [Description("C")]
           C,    //C
           [Description("1")]
           Construction_Grade_1,    //1
           [Description("2")]
           Construction_Grade_2,    //2
           [Description("3")]
           Construction_Grade_3,    //3
           [Description("N/A")]
           N_A     //N/A
        }
        private Construction_Grade_val m_Construction_Grade;
        [Category("Standard")]
        [Description("Construction Grade")]
        public Construction_Grade_val Construction_Grade
        {
           get
           { return m_Construction_Grade; }
           set
           { m_Construction_Grade = value; }
        }

        public Construction_Grade_val String_to_Construction_Grade_val(string pKey)
        {
           switch (pKey)
           {
                case "A":
                   return Construction_Grade_val.A;    //A
                case "F":
                   return Construction_Grade_val.F;    //F
                case "B":
                   return Construction_Grade_val.B;    //B
                case "C":
                   return Construction_Grade_val.C;    //C
                case "1":
                   return Construction_Grade_val.Construction_Grade_1;    //1
                case "2":
                   return Construction_Grade_val.Construction_Grade_2;    //2
                case "3":
                   return Construction_Grade_val.Construction_Grade_3;    //3
                case "N/A":
                   return Construction_Grade_val.N_A;    //N/A
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Construction_Grade_val_to_String(Construction_Grade_val pKey)
        {
           switch (pKey)
           {
                case Construction_Grade_val.A:
                   return "A";    //A
                case Construction_Grade_val.F:
                   return "F";    //F
                case Construction_Grade_val.B:
                   return "B";    //B
                case Construction_Grade_val.C:
                   return "C";    //C
                case Construction_Grade_val.Construction_Grade_1:
                   return "1";    //1
                case Construction_Grade_val.Construction_Grade_2:
                   return "2";    //2
                case Construction_Grade_val.Construction_Grade_3:
                   return "3";    //3
                case Construction_Grade_val.N_A:
                   return "N/A";    //N/A
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Crossing Conditions
        //   Attr Group:Standard
        //   Description:   Describes the crossings conditions of the site
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Unknown
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        None  (None)
        //        At Crossing  (At Crossing)
        public enum Crossing_Conditions_val
        {
           [Description("Unknown")]
           Unknown,    //Unknown
           [Description("None")]
           None,    //None
           [Description("At Crossing")]
           At_Crossing     //At Crossing
        }
        private Crossing_Conditions_val m_Crossing_Conditions;
        [Category("Standard")]
        [Description("Crossing Conditions")]
        public Crossing_Conditions_val Crossing_Conditions
        {
           get
           { return m_Crossing_Conditions; }
           set
           { m_Crossing_Conditions = value; }
        }

        public Crossing_Conditions_val String_to_Crossing_Conditions_val(string pKey)
        {
           switch (pKey)
           {
                case "Unknown":
                   return Crossing_Conditions_val.Unknown;    //Unknown
                case "None":
                   return Crossing_Conditions_val.None;    //None
                case "At Crossing":
                   return Crossing_Conditions_val.At_Crossing;    //At Crossing
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Crossing_Conditions_val_to_String(Crossing_Conditions_val pKey)
        {
           switch (pKey)
           {
                case Crossing_Conditions_val.Unknown:
                   return "Unknown";    //Unknown
                case Crossing_Conditions_val.None:
                   return "None";    //None
                case Crossing_Conditions_val.At_Crossing:
                   return "At Crossing";    //At Crossing
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Installation or Replacement
        //   Attr Group:Standard
        //   Description:   Use load factors at installation or load factors at replacement
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   At Installation
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        At Replacement  (Existing)
        public enum Installation_or_Replacement_val
        {
           [Description("At Installation")]
           At_Installation,    //New
           [Description("At Replacement")]
           At_Replacement     //Existing
        }
        private Installation_or_Replacement_val m_Installation_or_Replacement;
        [Category("Standard")]
        [Description("Installation or Replacement")]
        public Installation_or_Replacement_val Installation_or_Replacement
        {
           get
           { return m_Installation_or_Replacement; }
           set
           { m_Installation_or_Replacement = value; }
        }

        public Installation_or_Replacement_val String_to_Installation_or_Replacement_val(string pKey)
        {
           switch (pKey)
           {
                case "At Installation":
                   return Installation_or_Replacement_val.At_Installation;    //New
                case "At Replacement":
                   return Installation_or_Replacement_val.At_Replacement;    //Existing
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Installation_or_Replacement_val_to_String(Installation_or_Replacement_val pKey)
        {
           switch (pKey)
           {
                case Installation_or_Replacement_val.At_Installation:
                   return "At Installation";    //New
                case Installation_or_Replacement_val.At_Replacement:
                   return "At Replacement";    //Existing
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Override Wind
        //   Attr Group:Wind
        //   Description:   Apply fixed wind angle
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_Override_Wind;
        [Category("Wind")]
        [Description("Override Wind")]
        public bool Override_Wind
        {
           get { return m_Override_Wind; }
           set { m_Override_Wind = value; }
        }



        //   Attr Name:   NomWindAngle
        //   Attr Group:Wind
        //   Alt Display Name:Override Wind Angle (°)
        //   Description:   Nominal wind angle
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_NomWindAngle;
        [Category("Wind")]
        [Description("NomWindAngle")]
        public double NomWindAngle
        {
           get { return m_NomWindAngle; }
           set { m_NomWindAngle = value; }
        }



        //   Attr Name:   Terrian Exposure
        //   Attr Group:Standard
        //   Alt Display Name:Terrain Exposure
        //   Description:   ASCE Terrian Exposure Code
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   N/A
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        C  (C)
        //        D  (D)
        //        N/A  (N/A)
        public enum Terrian_Exposure_val
        {
           [Description("B")]
           B,    //B
           [Description("C")]
           C,    //C
           [Description("D")]
           D,    //D
           [Description("N/A")]
           N_A     //N/A
        }
        private Terrian_Exposure_val m_Terrian_Exposure;
        [Category("Standard")]
        [Description("Terrian Exposure")]
        public Terrian_Exposure_val Terrian_Exposure
        {
           get
           { return m_Terrian_Exposure; }
           set
           { m_Terrian_Exposure = value; }
        }

        public Terrian_Exposure_val String_to_Terrian_Exposure_val(string pKey)
        {
           switch (pKey)
           {
                case "B":
                   return Terrian_Exposure_val.B;    //B
                case "C":
                   return Terrian_Exposure_val.C;    //C
                case "D":
                   return Terrian_Exposure_val.D;    //D
                case "N/A":
                   return Terrian_Exposure_val.N_A;    //N/A
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Terrian_Exposure_val_to_String(Terrian_Exposure_val pKey)
        {
           switch (pKey)
           {
                case Terrian_Exposure_val.B:
                   return "B";    //B
                case Terrian_Exposure_val.C:
                   return "C";    //C
                case Terrian_Exposure_val.D:
                   return "D";    //D
                case Terrian_Exposure_val.N_A:
                   return "N/A";    //N/A
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Force Coef
        //   Attr Group:Standard
        //   Description:   ASCE Force Coefficient
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        private double m_Force_Coef;
        [Category("Standard")]
        [Description("Force Coef")]
        public double Force_Coef
        {
           get { return m_Force_Coef; }
           set { m_Force_Coef = value; }
        }



        //   Attr Name:   Apply FSR
        //   Attr Group:ANSI
        //   Description:   Apply ANSI O5.1.2008 Fiber Strength Reduction calculations for wood poles >= 60 feet in length
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_Apply_FSR;
        [Category("ANSI")]
        [Description("Apply FSR")]
        public bool Apply_FSR
        {
           get { return m_Apply_FSR; }
           set { m_Apply_FSR = value; }
        }



        //   Attr Name:   Strength Reduction Factor
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Φ Strength Reduction Factor
        //   Description:   Strength Reduction Factor
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Strength_Reduction_Factor;
        [Category("AS/NZS 7000")]
        [Description("Strength Reduction Factor")]
        public double Strength_Reduction_Factor
        {
           get { return m_Strength_Reduction_Factor; }
           set { m_Strength_Reduction_Factor = value; }
        }



        //   Attr Name:   Load Duration Factor
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:K1 Load Duration Factor
        //   Description:   Load Duration Factor
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Load_Duration_Factor;
        [Category("AS/NZS 7000")]
        [Description("Load Duration Factor")]
        public double Load_Duration_Factor
        {
           get { return m_Load_Duration_Factor; }
           set { m_Load_Duration_Factor = value; }
        }



        //   Attr Name:   Immaturity Factor
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:K20 Immaturity Factor
        //   Description:   Immaturity Factor
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Immaturity_Factor;
        [Category("AS/NZS 7000")]
        [Description("Immaturity Factor")]
        public double Immaturity_Factor
        {
           get { return m_Immaturity_Factor; }
           set { m_Immaturity_Factor = value; }
        }



        //   Attr Name:   Shaving Factor
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:K21 Shaving Factor
        //   Description:   Shaving Factor
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Shaving_Factor;
        [Category("AS/NZS 7000")]
        [Description("Shaving Factor")]
        public double Shaving_Factor
        {
           get { return m_Shaving_Factor; }
           set { m_Shaving_Factor = value; }
        }



        //   Attr Name:   Processing Factor
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:K22 Processing Factor
        //   Description:   Processing Factor
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Processing_Factor;
        [Category("AS/NZS 7000")]
        [Description("Processing Factor")]
        public double Processing_Factor
        {
           get { return m_Processing_Factor; }
           set { m_Processing_Factor = value; }
        }



        //   Attr Name:   Degradation Factor
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Kd Degradation Factor
        //   Description:   Degradation Factor
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Degradation_Factor;
        [Category("AS/NZS 7000")]
        [Description("Degradation Factor")]
        public double Degradation_Factor
        {
           get { return m_Degradation_Factor; }
           set { m_Degradation_Factor = value; }
        }



        //   Attr Name:   Shear Area
        //   Attr Group:AS/NZS 7000
        //   Description:   Shear Area
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Tip
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Actual  (Actual)
        public enum Shear_Area_val
        {
           [Description("Tip")]
           Tip,    //Tip
           [Description("Actual")]
           Actual     //Actual
        }
        private Shear_Area_val m_Shear_Area;
        [Category("AS/NZS 7000")]
        [Description("Shear Area")]
        public Shear_Area_val Shear_Area
        {
           get
           { return m_Shear_Area; }
           set
           { m_Shear_Area = value; }
        }

        public Shear_Area_val String_to_Shear_Area_val(string pKey)
        {
           switch (pKey)
           {
                case "Tip":
                   return Shear_Area_val.Tip;    //Tip
                case "Actual":
                   return Shear_Area_val.Actual;    //Actual
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Shear_Area_val_to_String(Shear_Area_val pKey)
        {
           switch (pKey)
           {
                case Shear_Area_val.Tip:
                   return "Tip";    //Tip
                case Shear_Area_val.Actual:
                   return "Actual";    //Actual
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   ApplyCrossarmAllowance
        //   Attr Group:Allowances
        //   Alt Display Name:Apply Crossarm Allowance
        //   Description:   Cable chair allowance on crossarm mode
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Auto
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Yes  (Yes)
        //        No  (No)
        public enum ApplyCrossarmAllowance_val
        {
           [Description("Auto")]
           Auto,    //Auto
           [Description("Yes")]
           Yes,    //Yes
           [Description("No")]
           No     //No
        }
        private ApplyCrossarmAllowance_val m_ApplyCrossarmAllowance;
        [Category("Allowances")]
        [Description("ApplyCrossarmAllowance")]
        public ApplyCrossarmAllowance_val ApplyCrossarmAllowance
        {
           get
           { return m_ApplyCrossarmAllowance; }
           set
           { m_ApplyCrossarmAllowance = value; }
        }

        public ApplyCrossarmAllowance_val String_to_ApplyCrossarmAllowance_val(string pKey)
        {
           switch (pKey)
           {
                case "Auto":
                   return ApplyCrossarmAllowance_val.Auto;    //Auto
                case "Yes":
                   return ApplyCrossarmAllowance_val.Yes;    //Yes
                case "No":
                   return ApplyCrossarmAllowance_val.No;    //No
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string ApplyCrossarmAllowance_val_to_String(ApplyCrossarmAllowance_val pKey)
        {
           switch (pKey)
           {
                case ApplyCrossarmAllowance_val.Auto:
                   return "Auto";    //Auto
                case ApplyCrossarmAllowance_val.Yes:
                   return "Yes";    //Yes
                case ApplyCrossarmAllowance_val.No:
                   return "No";    //No
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CrossarmAllowance
        //   Attr Group:Allowances
        //   Alt Display Name:Cable chair on Arm (lbs)
        //   Description:   Allowance for cable chair on crossarm
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   300
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_CrossarmAllowance;
        [Category("Allowances")]
        [Description("CrossarmAllowance")]
        public double CrossarmAllowance
        {
           get { return m_CrossarmAllowance; }
           set { m_CrossarmAllowance = value; }
        }



        //   Attr Name:   ApplyCableAllowance
        //   Attr Group:Allowances
        //   Alt Display Name:Apply Cable Allowance
        //   Description:   Ladder or Chair
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Auto
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Yes  (Yes)
        //        No  (No)
        public enum ApplyCableAllowance_val
        {
           [Description("Auto")]
           Auto,    //Auto
           [Description("Yes")]
           Yes,    //Yes
           [Description("No")]
           No     //No
        }
        private ApplyCableAllowance_val m_ApplyCableAllowance;
        [Category("Allowances")]
        [Description("ApplyCableAllowance")]
        public ApplyCableAllowance_val ApplyCableAllowance
        {
           get
           { return m_ApplyCableAllowance; }
           set
           { m_ApplyCableAllowance = value; }
        }

        public ApplyCableAllowance_val String_to_ApplyCableAllowance_val(string pKey)
        {
           switch (pKey)
           {
                case "Auto":
                   return ApplyCableAllowance_val.Auto;    //Auto
                case "Yes":
                   return ApplyCableAllowance_val.Yes;    //Yes
                case "No":
                   return ApplyCableAllowance_val.No;    //No
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string ApplyCableAllowance_val_to_String(ApplyCableAllowance_val pKey)
        {
           switch (pKey)
           {
                case ApplyCableAllowance_val.Auto:
                   return "Auto";    //Auto
                case ApplyCableAllowance_val.Yes:
                   return "Yes";    //Yes
                case ApplyCableAllowance_val.No:
                   return "No";    //No
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CableAllowance
        //   Attr Group:Allowances
        //   Alt Display Name:Cable allowance (lbs)
        //   Description:   Cable tension allowance for chair or ladder
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   300
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_CableAllowance;
        [Category("Allowances")]
        [Description("CableAllowance")]
        public double CableAllowance
        {
           get { return m_CableAllowance; }
           set { m_CableAllowance = value; }
        }



        //   Attr Name:   250 Rule
        //   Attr Group:NESC
        //   Description:   Rule applied from NESC 250 when calculating the loads
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Rule 250B
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Rule 250B Alternate  (Rule 250B Alternate)
        //        Rule 250C  (Rule 250C)
        //        Rule 250D  (Rule 250D)
        //        N/A  (N/A)
        public enum Attr_250_Rule_val
        {
           [Description("Rule 250B")]
           Rule_250B,    //Rule 250B
           [Description("Rule 250B Alternate")]
           Rule_250B_Alternate,    //Rule 250B Alternate
           [Description("Rule 250C")]
           Rule_250C,    //Rule 250C
           [Description("Rule 250D")]
           Rule_250D,    //Rule 250D
           [Description("N/A")]
           N_A     //N/A
        }
        private Attr_250_Rule_val m_Attr_250_Rule;
        [Category("NESC")]
        [Description("250 Rule")]
        public Attr_250_Rule_val Attr_250_Rule
        {
           get
           { return m_Attr_250_Rule; }
           set
           { m_Attr_250_Rule = value; }
        }

        public Attr_250_Rule_val String_to_Attr_250_Rule_val(string pKey)
        {
           switch (pKey)
           {
                case "Rule 250B":
                   return Attr_250_Rule_val.Rule_250B;    //Rule 250B
                case "Rule 250B Alternate":
                   return Attr_250_Rule_val.Rule_250B_Alternate;    //Rule 250B Alternate
                case "Rule 250C":
                   return Attr_250_Rule_val.Rule_250C;    //Rule 250C
                case "Rule 250D":
                   return Attr_250_Rule_val.Rule_250D;    //Rule 250D
                case "N/A":
                   return Attr_250_Rule_val.N_A;    //N/A
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Attr_250_Rule_val_to_String(Attr_250_Rule_val pKey)
        {
           switch (pKey)
           {
                case Attr_250_Rule_val.Rule_250B:
                   return "Rule 250B";    //Rule 250B
                case Attr_250_Rule_val.Rule_250B_Alternate:
                   return "Rule 250B Alternate";    //Rule 250B Alternate
                case Attr_250_Rule_val.Rule_250C:
                   return "Rule 250C";    //Rule 250C
                case Attr_250_Rule_val.Rule_250D:
                   return "Rule 250D";    //Rule 250D
                case Attr_250_Rule_val.N_A:
                   return "N/A";    //N/A
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Vertical LF
        //   Attr Group:Pole LFs
        //   Alt Display Name:Vertical Pole LF
        //   Description:   NESC Vertical Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Vertical_LF;
        [Category("Pole LFs")]
        [Description("Vertical LF")]
        public double Vertical_LF
        {
           get { return m_Vertical_LF; }
           set { m_Vertical_LF = value; }
        }



        //   Attr Name:   TransWind LF
        //   Attr Group:Pole LFs
        //   Alt Display Name:Transverse Wind Pole LF
        //   Description:   NESC Transverse Wind Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   2.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TransWind_LF;
        [Category("Pole LFs")]
        [Description("TransWind LF")]
        public double TransWind_LF
        {
           get { return m_TransWind_LF; }
           set { m_TransWind_LF = value; }
        }



        //   Attr Name:   TransTension LF
        //   Attr Group:Pole LFs
        //   Alt Display Name:Wire Tension (Guyed, Junction, Angle) Pole LF
        //   Description:   NESC Transverse Wire Tesnion Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TransTension_LF;
        [Category("Pole LFs")]
        [Description("TransTension LF")]
        public double TransTension_LF
        {
           get { return m_TransTension_LF; }
           set { m_TransTension_LF = value; }
        }



        //   Attr Name:   GeneralLongitude LF
        //   Attr Group:Pole LFs
        //   Alt Display Name:Wire Tension (Unguyed Tangent) Pole LF
        //   Description:   NESC General Non-Deadend Longitudinal Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.10
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_GeneralLongitude_LF;
        [Category("Pole LFs")]
        [Description("GeneralLongitude LF")]
        public double GeneralLongitude_LF
        {
           get { return m_GeneralLongitude_LF; }
           set { m_GeneralLongitude_LF = value; }
        }



        //   Attr Name:   DeadendLongitude LF
        //   Attr Group:Pole LFs
        //   Alt Display Name:Wire Tension (Deadend) Pole LF
        //   Description:   NESC Deadend Longitudinal Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DeadendLongitude_LF;
        [Category("Pole LFs")]
        [Description("DeadendLongitude LF")]
        public double DeadendLongitude_LF
        {
           get { return m_DeadendLongitude_LF; }
           set { m_DeadendLongitude_LF = value; }
        }



        //   Attr Name:   Guy Vertical LF
        //   Attr Group:Guy LFs
        //   Alt Display Name:Vertical Guy LF
        //   Description:   NESC Vertical Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_Vertical_LF;
        [Category("Guy LFs")]
        [Description("Guy Vertical LF")]
        public double Guy_Vertical_LF
        {
           get { return m_Guy_Vertical_LF; }
           set { m_Guy_Vertical_LF = value; }
        }



        //   Attr Name:   Guy TransWind LF
        //   Attr Group:Guy LFs
        //   Alt Display Name:Transverse Wind Guy LF
        //   Description:   NESC Transverse Wind Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   2.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_TransWind_LF;
        [Category("Guy LFs")]
        [Description("Guy TransWind LF")]
        public double Guy_TransWind_LF
        {
           get { return m_Guy_TransWind_LF; }
           set { m_Guy_TransWind_LF = value; }
        }



        //   Attr Name:   Guy TransTension LF
        //   Attr Group:Guy LFs
        //   Alt Display Name:Wire Tension (Guyed, Junction, Angle) Guy LF
        //   Description:   NESC Transverse Wire Tesnion Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_TransTension_LF;
        [Category("Guy LFs")]
        [Description("Guy TransTension LF")]
        public double Guy_TransTension_LF
        {
           get { return m_Guy_TransTension_LF; }
           set { m_Guy_TransTension_LF = value; }
        }



        //   Attr Name:   Guy GeneralLongitude LF
        //   Attr Group:Guy LFs
        //   Alt Display Name:Wire Tension (Unguyed Tangent) Guy LF
        //   Description:   NESC General Non-Deadend Longitudinal Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.10
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_GeneralLongitude_LF;
        [Category("Guy LFs")]
        [Description("Guy GeneralLongitude LF")]
        public double Guy_GeneralLongitude_LF
        {
           get { return m_Guy_GeneralLongitude_LF; }
           set { m_Guy_GeneralLongitude_LF = value; }
        }



        //   Attr Name:   Guy DeadendLongitude LF
        //   Attr Group:Guy LFs
        //   Alt Display Name:Wire Tension (Deadend) Guy LF
        //   Description:   NESC Deadend Longitudinal Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_DeadendLongitude_LF;
        [Category("Guy LFs")]
        [Description("Guy DeadendLongitude LF")]
        public double Guy_DeadendLongitude_LF
        {
           get { return m_Guy_DeadendLongitude_LF; }
           set { m_Guy_DeadendLongitude_LF = value; }
        }



        //   Attr Name:   Anchor Vertical LF
        //   Attr Group:Anchor LFs
        //   Alt Display Name:Vertical Anchor LF
        //   Description:   NESC Vertical Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Anchor_Vertical_LF;
        [Category("Anchor LFs")]
        [Description("Anchor Vertical LF")]
        public double Anchor_Vertical_LF
        {
           get { return m_Anchor_Vertical_LF; }
           set { m_Anchor_Vertical_LF = value; }
        }



        //   Attr Name:   Anchor TransWind LF
        //   Attr Group:Anchor LFs
        //   Alt Display Name:Transverse Wind Anchor LF
        //   Description:   NESC Transverse Wind Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   2.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Anchor_TransWind_LF;
        [Category("Anchor LFs")]
        [Description("Anchor TransWind LF")]
        public double Anchor_TransWind_LF
        {
           get { return m_Anchor_TransWind_LF; }
           set { m_Anchor_TransWind_LF = value; }
        }



        //   Attr Name:   Anchor TransTension LF
        //   Attr Group:Anchor LFs
        //   Alt Display Name:Wire Tension (Guyed, Junction, Angle) Anchor LF
        //   Description:   NESC Transverse Wire Tesnion Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Anchor_TransTension_LF;
        [Category("Anchor LFs")]
        [Description("Anchor TransTension LF")]
        public double Anchor_TransTension_LF
        {
           get { return m_Anchor_TransTension_LF; }
           set { m_Anchor_TransTension_LF = value; }
        }



        //   Attr Name:   Anchor GeneralLongitude LF
        //   Attr Group:Anchor LFs
        //   Alt Display Name:Wire Tension (Unguyed Tangent) Anchor LF
        //   Description:   NESC General Non-Deadend Longitudinal Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.10
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Anchor_GeneralLongitude_LF;
        [Category("Anchor LFs")]
        [Description("Anchor GeneralLongitude LF")]
        public double Anchor_GeneralLongitude_LF
        {
           get { return m_Anchor_GeneralLongitude_LF; }
           set { m_Anchor_GeneralLongitude_LF = value; }
        }



        //   Attr Name:   Anchor DeadendLongitude LF
        //   Attr Group:Anchor LFs
        //   Alt Display Name:Wire Tension (Deadend) Anchor LF
        //   Description:   NESC Deadend Longitudinal Load Factor for applied 250 rule calculations for the selected Construction Grade
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Anchor_DeadendLongitude_LF;
        [Category("Anchor LFs")]
        [Description("Anchor DeadendLongitude LF")]
        public double Anchor_DeadendLongitude_LF
        {
           get { return m_Anchor_DeadendLongitude_LF; }
           set { m_Anchor_DeadendLongitude_LF = value; }
        }



        //   Attr Name:   Pole Strength Factor
        //   Attr Group:Standard
        //   Description:   Pole Strength Factor value
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   0.85
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Pole_Strength_Factor;
        [Category("Standard")]
        [Description("Pole Strength Factor")]
        public double Pole_Strength_Factor
        {
           get { return m_Pole_Strength_Factor; }
           set { m_Pole_Strength_Factor = value; }
        }



        //   Attr Name:   Manufactured Pole Strength Factor
        //   Attr Group:Strength
        //   Alt Display Name:Mfg Pole Str Factor
        //   Description:   Pole Strength Factor value
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Manufactured_Pole_Strength_Factor;
        [Category("Strength")]
        [Description("Manufactured Pole Strength Factor")]
        public double Manufactured_Pole_Strength_Factor
        {
           get { return m_Manufactured_Pole_Strength_Factor; }
           set { m_Manufactured_Pole_Strength_Factor = value; }
        }



        //   Attr Name:   Crossarm Strength Factor
        //   Attr Group:Strength
        //   Description:   Crosarm Strength Factor value
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   0.50
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Crossarm_Strength_Factor;
        [Category("Strength")]
        [Description("Crossarm Strength Factor")]
        public double Crossarm_Strength_Factor
        {
           get { return m_Crossarm_Strength_Factor; }
           set { m_Crossarm_Strength_Factor = value; }
        }



        //   Attr Name:   Guy Strength Factor
        //   Attr Group:Strength
        //   Description:   Guy Strength Factor value
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   0.9
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_Strength_Factor;
        [Category("Strength")]
        [Description("Guy Strength Factor")]
        public double Guy_Strength_Factor
        {
           get { return m_Guy_Strength_Factor; }
           set { m_Guy_Strength_Factor = value; }
        }



        //   Attr Name:   Anchor Strength Factor
        //   Attr Group:Strength
        //   Description:   Anchor Strength Factor value
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   1.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Anchor_Strength_Factor;
        [Category("Strength")]
        [Description("Anchor Strength Factor")]
        public double Anchor_Strength_Factor
        {
           get { return m_Anchor_Strength_Factor; }
           set { m_Anchor_Strength_Factor = value; }
        }



        //   Attr Name:   PoleCapacityThreshhold
        //   Attr Group:Threshold
        //   Alt Display Name:Capacity Threshold %
        //   Description:   PoleCapacityThreshhold
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   75
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoleCapacityThreshhold;
        [Category("Threshold")]
        [Description("PoleCapacityThreshhold")]
        public double PoleCapacityThreshhold
        {
           get { return m_PoleCapacityThreshhold; }
           set { m_PoleCapacityThreshhold = value; }
        }



        //   Attr Name:   Guy Inadequate Thresh
        //   Attr Group:Threshold
        //   Alt Display Name:Guy/Anch Inadequate %
        //   Description:   
        //   Displayed Units:   store as PERCENT 0 TO 100 display as INVERSE PERCENT 0 TO 100
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_Inadequate_Thresh;
        [Category("Threshold")]
        [Description("Guy Inadequate Thresh")]
        public double Guy_Inadequate_Thresh
        {
           get { return m_Guy_Inadequate_Thresh; }
           set { m_Guy_Inadequate_Thresh = value; }
        }



        //   Attr Name:   Guy At Cap Thresh
        //   Attr Group:Threshold
        //   Alt Display Name:Guy/Anch At Cap %
        //   Description:   
        //   Displayed Units:   store as PERCENT 0 TO 100 display as INVERSE PERCENT 0 TO 100
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   2
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_At_Cap_Thresh;
        [Category("Threshold")]
        [Description("Guy At Cap Thresh")]
        public double Guy_At_Cap_Thresh
        {
           get { return m_Guy_At_Cap_Thresh; }
           set { m_Guy_At_Cap_Thresh = value; }
        }



        //   Attr Name:   Guy Near Cap Thresh
        //   Attr Group:Threshold
        //   Alt Display Name:Guy/Anch Near Cap %
        //   Description:   
        //   Displayed Units:   store as PERCENT 0 TO 100 display as INVERSE PERCENT 0 TO 100
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   10
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Guy_Near_Cap_Thresh;
        [Category("Threshold")]
        [Description("Guy Near Cap Thresh")]
        public double Guy_Near_Cap_Thresh
        {
           get { return m_Guy_Near_Cap_Thresh; }
           set { m_Guy_Near_Cap_Thresh = value; }
        }



        //   Attr Name:   Span Cap Thresh
        //   Attr Group:Threshold
        //   Alt Display Name:Span Load Thresh %
        //   Description:   
        //   Displayed Units:   store as PERCENT 0 TO 100 display as INVERSE PERCENT 0 TO 100
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   10
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Span_Cap_Thresh;
        [Category("Threshold")]
        [Description("Span Cap Thresh")]
        public double Span_Cap_Thresh
        {
           get { return m_Span_Cap_Thresh; }
           set { m_Span_Cap_Thresh = value; }
        }



        //   Attr Name:   BucklingConstUnguyed
        //   Attr Group:Buckling
        //   Alt Display Name:Unguyed Constant
        //   Description:   Buckling constant unguyed
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   2
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BucklingConstUnguyed;
        [Category("Buckling")]
        [Description("BucklingConstUnguyed")]
        public double BucklingConstUnguyed
        {
           get { return m_BucklingConstUnguyed; }
           set { m_BucklingConstUnguyed = value; }
        }



        //   Attr Name:   BucklingConstGuyed
        //   Attr Group:Buckling
        //   Alt Display Name:Guyed Constant
        //   Description:   Buckling constant guyed
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   0.707106781186548
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BucklingConstGuyed;
        [Category("Buckling")]
        [Description("BucklingConstGuyed")]
        public double BucklingConstGuyed
        {
           get { return m_BucklingConstGuyed; }
           set { m_BucklingConstGuyed = value; }
        }



        //   Attr Name:   BucklingConstGuyedDeadend
        //   Attr Group:Buckling
        //   Alt Display Name:Guyed Deadend Const
        //   Description:   Buckling constant guyed deadend
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   0.707106781186548
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BucklingConstGuyedDeadend;
        [Category("Buckling")]
        [Description("BucklingConstGuyedDeadend")]
        public double BucklingConstGuyedDeadend
        {
           get { return m_BucklingConstGuyedDeadend; }
           set { m_BucklingConstGuyedDeadend = value; }
        }



        //   Attr Name:   SectionHeightMethod
        //   Attr Group:Buckling
        //   Alt Display Name:Section Method
        //   Description:   Buckling method
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Standard
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Percent BCH  (Percent BCH)
        public enum SectionHeightMethod_val
        {
           [Description("Standard")]
           Standard,    //Standard
           [Description("Percent BCH")]
           Percent_BCH     //Percent BCH
        }
        private SectionHeightMethod_val m_SectionHeightMethod;
        [Category("Buckling")]
        [Description("SectionHeightMethod")]
        public SectionHeightMethod_val SectionHeightMethod
        {
           get
           { return m_SectionHeightMethod; }
           set
           { m_SectionHeightMethod = value; }
        }

        public SectionHeightMethod_val String_to_SectionHeightMethod_val(string pKey)
        {
           switch (pKey)
           {
                case "Standard":
                   return SectionHeightMethod_val.Standard;    //Standard
                case "Percent BCH":
                   return SectionHeightMethod_val.Percent_BCH;    //Percent BCH
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string SectionHeightMethod_val_to_String(SectionHeightMethod_val pKey)
        {
           switch (pKey)
           {
                case SectionHeightMethod_val.Standard:
                   return "Standard";    //Standard
                case SectionHeightMethod_val.Percent_BCH:
                   return "Percent BCH";    //Percent BCH
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   BucklingSectionPercentBCH
        //   Attr Group:Buckling
        //   Alt Display Name:Percent BCH
        //   Description:   Percent of Buckling Column Height to use as Buckling Section
        //   Displayed Units:   store as PERCENT 0 TO 1 display as PERCENT 0 TO 100
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.33333333
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        private double m_BucklingSectionPercentBCH;
        [Category("Buckling")]
        [Description("BucklingSectionPercentBCH")]
        public double BucklingSectionPercentBCH
        {
           get { return m_BucklingSectionPercentBCH; }
           set { m_BucklingSectionPercentBCH = value; }
        }



        //   Attr Name:   ReportingAngleMode
        //   Attr Group:Reporting
        //   Alt Display Name:Report Angle Mode
        //   Description:   Reporting Angle Mode
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Load
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Wind  (Wind)
        //        Load  (Load)
        //        Relative  (Relative)
        //        Fixed  (Fixed)
        public enum ReportingAngleMode_val
        {
           [Description("Tip Deflection")]
           Tip_Deflection,    //Tip Deflection
           [Description("Wind")]
           Wind,    //Wind
           [Description("Load")]
           Load,    //Load
           [Description("Relative")]
           Relative,    //Relative
           [Description("Fixed")]
           Fixed     //Fixed
        }
        private ReportingAngleMode_val m_ReportingAngleMode;
        [Category("Reporting")]
        [Description("ReportingAngleMode")]
        public ReportingAngleMode_val ReportingAngleMode
        {
           get
           { return m_ReportingAngleMode; }
           set
           { m_ReportingAngleMode = value; }
        }

        public ReportingAngleMode_val String_to_ReportingAngleMode_val(string pKey)
        {
           switch (pKey)
           {
                case "Tip Deflection":
                   return ReportingAngleMode_val.Tip_Deflection;    //Tip Deflection
                case "Wind":
                   return ReportingAngleMode_val.Wind;    //Wind
                case "Load":
                   return ReportingAngleMode_val.Load;    //Load
                case "Relative":
                   return ReportingAngleMode_val.Relative;    //Relative
                case "Fixed":
                   return ReportingAngleMode_val.Fixed;    //Fixed
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string ReportingAngleMode_val_to_String(ReportingAngleMode_val pKey)
        {
           switch (pKey)
           {
                case ReportingAngleMode_val.Tip_Deflection:
                   return "Tip Deflection";    //Tip Deflection
                case ReportingAngleMode_val.Wind:
                   return "Wind";    //Wind
                case ReportingAngleMode_val.Load:
                   return "Load";    //Load
                case ReportingAngleMode_val.Relative:
                   return "Relative";    //Relative
                case ReportingAngleMode_val.Fixed:
                   return "Fixed";    //Fixed
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   ReportingAngle
        //   Attr Group:Reporting
        //   Alt Display Name:Reporting Angle (°)
        //   Description:   Reporting angle
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   1.5707963267949
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ReportingAngle;
        [Category("Reporting")]
        [Description("ReportingAngle")]
        public double ReportingAngle
        {
           get { return m_ReportingAngle; }
           set { m_ReportingAngle = value; }
        }



        //   Attr Name:   SpanCapSignal
        //   Attr Group:Reporting
        //   Alt Display Name:Signal Span Over Cap
        //   Description:   
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_SpanCapSignal;
        [Category("Reporting")]
        [Description("SpanCapSignal")]
        public bool SpanCapSignal
        {
           get { return m_SpanCapSignal; }
           set { m_SpanCapSignal = value; }
        }



        //   Attr Name:   ArmCapSignal
        //   Attr Group:Reporting
        //   Alt Display Name:Signal Crossarm Over Cap
        //   Description:   
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_ArmCapSignal;
        [Category("Reporting")]
        [Description("ArmCapSignal")]
        public bool ArmCapSignal
        {
           get { return m_ArmCapSignal; }
           set { m_ArmCapSignal = value; }
        }



        //   Attr Name:   GuyCapSignal
        //   Attr Group:Reporting
        //   Alt Display Name:Signal Guy Over Cap
        //   Description:   
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_GuyCapSignal;
        [Category("Reporting")]
        [Description("GuyCapSignal")]
        public bool GuyCapSignal
        {
           get { return m_GuyCapSignal; }
           set { m_GuyCapSignal = value; }
        }



        //   Attr Name:   AnchorCapSignal
        //   Attr Group:Reporting
        //   Alt Display Name:Signal Anchor Over Cap
        //   Description:   
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_AnchorCapSignal;
        [Category("Reporting")]
        [Description("AnchorCapSignal")]
        public bool AnchorCapSignal
        {
           get { return m_AnchorCapSignal; }
           set { m_AnchorCapSignal = value; }
        }



        //   Attr Name:   CarryCapacityUp
        //   Attr Group:Reporting
        //   Alt Display Name:Carry Cap Up
        //   Description:   Carry moment capacy upwards
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_CarryCapacityUp;
        [Category("Reporting")]
        [Description("CarryCapacityUp")]
        public bool CarryCapacityUp
        {
           get { return m_CarryCapacityUp; }
           set { m_CarryCapacityUp = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Notes
   // Mirrors: PPLNotes : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Notes : ElementBase
   {

      public static string gXMLkey = "Notes";
      public override string XMLkey() { return gXMLkey; }

      public Notes(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Note";
               m_Type = Type_val.Normal;
               m_Owner = "<Undefined>";
               m_Author = "";
               m_Date = "";
               m_Contents = "";
               m_Grid = "";
               m_SplitPercent = 0.6;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the note
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Note
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Alt Display Name:Note Type
        //   Description:   Note type
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Normal
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        High Priority  (High Priority)
        //        TBD Initial  (TBD Initial)
        //        TBD Complete  (TBD Complete)
        //        TBD Accepted  (TBD Accepted)
        public enum Type_val
        {
           [Description("Normal")]
           Normal,    //Normal
           [Description("High Priority")]
           High_Priority,    //High Priority
           [Description("TBD Initial")]
           TBD_Initial,    //TBD Initial
           [Description("TBD Complete")]
           TBD_Complete,    //TBD Complete
           [Description("TBD Accepted")]
           TBD_Accepted     //TBD Accepted
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Normal":
                   return Type_val.Normal;    //Normal
                case "High Priority":
                   return Type_val.High_Priority;    //High Priority
                case "TBD Initial":
                   return Type_val.TBD_Initial;    //TBD Initial
                case "TBD Complete":
                   return Type_val.TBD_Complete;    //TBD Complete
                case "TBD Accepted":
                   return Type_val.TBD_Accepted;    //TBD Accepted
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Normal:
                   return "Normal";    //Normal
                case Type_val.High_Priority:
                   return "High Priority";    //High Priority
                case Type_val.TBD_Initial:
                   return "TBD Initial";    //TBD Initial
                case Type_val.TBD_Complete:
                   return "TBD Complete";    //TBD Complete
                case Type_val.TBD_Accepted:
                   return "TBD Accepted";    //TBD Accepted
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Author
        //   Attr Group:Standard
        //   Description:   Author of the note
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Author;
        [Category("Standard")]
        [Description("Author")]
        public string Author
        {
           get { return m_Author; }
           set { m_Author = value; }
        }



        //   Attr Name:   Date
        //   Attr Group:Standard
        //   Description:   Creation date of the note
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   DATE
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Date;
        [Category("Standard")]
        [Description("Date")]
        public string Date
        {
           get { return m_Date; }
           set { m_Date = value; }
        }



        //   Attr Name:   Contents
        //   Attr Group:Standard
        //   Description:   RTF Contents
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Contents;
        [Category("Standard")]
        [Description("Contents")]
        public string Contents
        {
           get { return m_Contents; }
           set { m_Contents = value; }
        }



        //   Attr Name:   Grid
        //   Attr Group:Standard
        //   Description:   SpreadSheet Contents
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        private string m_Grid;
        [Category("Standard")]
        [Description("Grid")]
        public string Grid
        {
           get { return m_Grid; }
           set { m_Grid = value; }
        }



        //   Attr Name:   SplitPercent
        //   Attr Group:Standard
        //   Description:   Split bar position
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   0.6
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        private double m_SplitPercent;
        [Category("Standard")]
        [Description("SplitPercent")]
        public double SplitPercent
        {
           get { return m_SplitPercent; }
           set { m_SplitPercent = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: LinkedURI
   // Mirrors: PPLLinkedURI : PPLElement
   //--------------------------------------------------------------------------------------------
   public class LinkedURI : ElementBase
   {

      public static string gXMLkey = "LinkedURI";
      public override string XMLkey() { return gXMLkey; }

      public LinkedURI(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Link";
               m_Owner = "<Undefined>";
               m_URI = "";
               m_Viewer = Viewer_val.External;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the link
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Link
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   URI
        //   Attr Group:Standard
        //   Description:   Universal Resource Identifier
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   URI
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_URI;
        [Category("Standard")]
        [Description("URI")]
        public string URI
        {
           get { return m_URI; }
           set { m_URI = value; }
        }



        //   Attr Name:   Viewer
        //   Attr Group:Standard
        //   Description:   Viewer to use
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   External
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        External  (External)
        public enum Viewer_val
        {
           [Description("Built in")]
           Built_in,    //Built in
           [Description("External")]
           External     //External
        }
        private Viewer_val m_Viewer;
        [Category("Standard")]
        [Description("Viewer")]
        public Viewer_val Viewer
        {
           get
           { return m_Viewer; }
           set
           { m_Viewer = value; }
        }

        public Viewer_val String_to_Viewer_val(string pKey)
        {
           switch (pKey)
           {
                case "Built in":
                   return Viewer_val.Built_in;    //Built in
                case "External":
                   return Viewer_val.External;    //External
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Viewer_val_to_String(Viewer_val pKey)
        {
           switch (pKey)
           {
                case Viewer_val.Built_in:
                   return "Built in";    //Built in
                case Viewer_val.External:
                   return "External";    //External
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: PoleInfoPoint
   // Mirrors: PPLPoleInfoPoint : PPLElement
   //--------------------------------------------------------------------------------------------
   public class PoleInfoPoint : ElementBase
   {

      public static string gXMLkey = "PoleInfoPoint";
      public override string XMLkey() { return gXMLkey; }

      public PoleInfoPoint(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "";
               m_Owner = "<Undefined>";
               m_CoordinateZ = 120;
               m_CoordinateA = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Brief description of the damage
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Location (ft)
        //   Description:   Distance from the butt of the pole to center of damage or decay
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   120.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The rotation angle around the center of the pole
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: PoleSegment
   // Mirrors: PPLSegment : PPLElement
   //--------------------------------------------------------------------------------------------
   public class PoleSegment : ElementBase
   {

      public static string gXMLkey = "PoleSegment";
      public override string XMLkey() { return gXMLkey; }

      public PoleSegment(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Material";
               m_Name = "<tbd>";
               m_CoordinateZ = -72;
               m_Type = Type_val.Simple;
               m_Color = "#FFD2B48C";
               m_LengthInInches = 60;
               m_LapLengthInInches = 60;
               m_LapMode = LapMode_val.None;
               m_RadiusAtTipInInches = 8;
               m_RadiusAtBaseInInches = 8;
               m_Weight = 500;
               m_Shape = Shape_val.Round;
               m_Faces = 8;
               m_ThicknessTable = new ValTable("Thick;0,0.25;");
               m_Modulus_of_Elasticity = 1600000;
               m_PoissonsRatio = 0.23;
               m_WindDragCoef = 0;
               m_ThermalCoefficient = 2.7E-06;
               m_MomentCapacityTable = new ValTable("Moment;0,50000;");
               m_BucklingCapacityTable = new ValTable("Buckling;0,5000;");
               m_MaterialTip = "<unset>";
               m_MaterialBase = "<unset>";
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Material) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the segment
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Material
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the segment
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <tbd>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Height (ft)
        //   Description:   Height
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0##
        //   Attribute Type:   TRACKERZ
        //   Default Value:   -72
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   Segment type
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Simple
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Advanced  (Advanced)
        public enum Type_val
        {
           [Description("Simple")]
           Simple,    //Simple
           [Description("Advanced")]
           Advanced     //Advanced
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Simple":
                   return Type_val.Simple;    //Simple
                case "Advanced":
                   return Type_val.Advanced;    //Advanced
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Simple:
                   return "Simple";    //Simple
                case Type_val.Advanced:
                   return "Advanced";    //Advanced
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Color
        //   Attr Group:Standard
        //   Alt Display Name:Display Color
        //   Description:   The color of the segment
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   COLOR
        //   Default Value:   #FFD2B48C
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Color;
        [Category("Standard")]
        [Description("Color")]
        public string Color
        {
           get { return m_Color; }
           set { m_Color = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Dimensions
        //   Alt Display Name:Length (in)
        //   Description:   Length
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   60
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Dimensions")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   LapLengthInInches
        //   Attr Group:Dimensions
        //   Alt Display Name:Lap Length (in)
        //   Description:   Lap Length
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   60
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LapLengthInInches;
        [Category("Dimensions")]
        [Description("LapLengthInInches")]
        public double LapLengthInInches
        {
           get { return m_LapLengthInInches; }
           set { m_LapLengthInInches = value; }
        }



        //   Attr Name:   LapMode
        //   Attr Group:Dimensions
        //   Alt Display Name:Lap Mode
        //   Description:   Lap Mode
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   None
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Bottom  (Bottom)
        //        None  (None)
        public enum LapMode_val
        {
           [Description("Top")]
           Top,    //Top
           [Description("Bottom")]
           Bottom,    //Bottom
           [Description("None")]
           None     //None
        }
        private LapMode_val m_LapMode;
        [Category("Dimensions")]
        [Description("LapMode")]
        public LapMode_val LapMode
        {
           get
           { return m_LapMode; }
           set
           { m_LapMode = value; }
        }

        public LapMode_val String_to_LapMode_val(string pKey)
        {
           switch (pKey)
           {
                case "Top":
                   return LapMode_val.Top;    //Top
                case "Bottom":
                   return LapMode_val.Bottom;    //Bottom
                case "None":
                   return LapMode_val.None;    //None
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string LapMode_val_to_String(LapMode_val pKey)
        {
           switch (pKey)
           {
                case LapMode_val.Top:
                   return "Top";    //Top
                case LapMode_val.Bottom:
                   return "Bottom";    //Bottom
                case LapMode_val.None:
                   return "None";    //None
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   RadiusAtTipInInches
        //   Attr Group:Dimensions
        //   Alt Display Name:Radius at Tip (in)
        //   Description:   Radius at tip
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   8
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtTipInInches;
        [Category("Dimensions")]
        [Description("RadiusAtTipInInches")]
        public double RadiusAtTipInInches
        {
           get { return m_RadiusAtTipInInches; }
           set { m_RadiusAtTipInInches = value; }
        }



        //   Attr Name:   RadiusAtBaseInInches
        //   Attr Group:Dimensions
        //   Alt Display Name:Radius at Base (in)
        //   Description:   Radius at base
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   8
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtBaseInInches;
        [Category("Dimensions")]
        [Description("RadiusAtBaseInInches")]
        public double RadiusAtBaseInInches
        {
           get { return m_RadiusAtBaseInInches; }
           set { m_RadiusAtBaseInInches = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Dimensions
        //   Alt Display Name:Segment Weight (lbs)
        //   Description:   Segment weight in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Dimensions")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   Shape
        //   Attr Group:Section
        //   Description:   Cross section shape
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Round
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Polygonal  (Polygonal)
        public enum Shape_val
        {
           [Description("Round")]
           Round,    //Round
           [Description("Polygonal")]
           Polygonal     //Polygonal
        }
        private Shape_val m_Shape;
        [Category("Section")]
        [Description("Shape")]
        public Shape_val Shape
        {
           get
           { return m_Shape; }
           set
           { m_Shape = value; }
        }

        public Shape_val String_to_Shape_val(string pKey)
        {
           switch (pKey)
           {
                case "Round":
                   return Shape_val.Round;    //Round
                case "Polygonal":
                   return Shape_val.Polygonal;    //Polygonal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Shape_val_to_String(Shape_val pKey)
        {
           switch (pKey)
           {
                case Shape_val.Round:
                   return "Round";    //Round
                case Shape_val.Polygonal:
                   return "Polygonal";    //Polygonal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Faces
        //   Attr Group:Section
        //   Alt Display Name:Polygon Faces
        //   Description:   Number of polygon faces
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   PLUSMINUS
        //   Default Value:   8
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_Faces;
        [Category("Section")]
        [Description("Faces")]
        public int Faces
        {
           get { return m_Faces; }
           set { m_Faces = value; }
        }



        //   Attr Name:   ThicknessTable
        //   Attr Group:Capacity
        //   Alt Display Name:Thickness
        //   Description:   The material thickness
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   THICKNESS_TABLE
        //   Default Value:   Thick;0,0.25;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_ThicknessTable = new ValTable();
        [Category("Capacity")]
        [Description("ThicknessTable")]
        public ValTable ThicknessTable
        {
           get { return m_ThicknessTable; }
           set { m_ThicknessTable = value; }
        }



        //   Attr Name:   Modulus of Elasticity
        //   Attr Group:Capacity
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   Modulus of elasticty for the material
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   1600000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Elasticity;
        [Category("Capacity")]
        [Description("Modulus of Elasticity")]
        public double Modulus_of_Elasticity
        {
           get { return m_Modulus_of_Elasticity; }
           set { m_Modulus_of_Elasticity = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Capacity
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.23
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Capacity")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Capacity
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Capacity")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Capacity
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000027
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Capacity")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   MomentCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Moment Cap (ft-lb)
        //   Description:   The moment capacity table
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   MOMENT_TABLE
        //   Default Value:   Moment;0,50000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_MomentCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("MomentCapacityTable")]
        public ValTable MomentCapacityTable
        {
           get { return m_MomentCapacityTable; }
           set { m_MomentCapacityTable = value; }
        }



        //   Attr Name:   BucklingCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Buckling Cap (lbs)
        //   Description:   The buckling capacity table
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BUCKLING_TABLE
        //   Default Value:   Buckling;0,5000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_BucklingCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("BucklingCapacityTable")]
        public ValTable BucklingCapacityTable
        {
           get { return m_BucklingCapacityTable; }
           set { m_BucklingCapacityTable = value; }
        }



        //   Attr Name:   MaterialTip
        //   Attr Group:Materials
        //   Alt Display Name:Material at Tip
        //   Description:   Material Tip
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   CHILD_MATERIAL_NAME
        //   Default Value:   <unset>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_MaterialTip;
        [Category("Materials")]
        [Description("MaterialTip")]
        public string MaterialTip
        {
           get { return m_MaterialTip; }
           set { m_MaterialTip = value; }
        }



        //   Attr Name:   MaterialBase
        //   Attr Group:Materials
        //   Alt Display Name:Material at Base
        //   Description:   Material Base
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   CHILD_MATERIAL_NAME
        //   Default Value:   <unset>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_MaterialBase;
        [Category("Materials")]
        [Description("MaterialBase")]
        public string MaterialBase
        {
           get { return m_MaterialBase; }
           set { m_MaterialBase = value; }
        }



        //   Attr Name:   Blend by
        //   Attr Group:Materials
        //   Description:   Blending Function
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Radius  (Radius)
        //        Area  (Area)
        //        Area Squared  (Area Squared)
        public enum Blend_by_val
        {
           [Description("Height")]
           Height,    //Height
           [Description("Radius")]
           Radius,    //Radius
           [Description("Area")]
           Area,    //Area
           [Description("Area Squared")]
           Area_Squared     //Area Squared
        }
        private Blend_by_val m_Blend_by;
        [Category("Materials")]
        [Description("Blend by")]
        public Blend_by_val Blend_by
        {
           get
           { return m_Blend_by; }
           set
           { m_Blend_by = value; }
        }

        public Blend_by_val String_to_Blend_by_val(string pKey)
        {
           switch (pKey)
           {
                case "Height":
                   return Blend_by_val.Height;    //Height
                case "Radius":
                   return Blend_by_val.Radius;    //Radius
                case "Area":
                   return Blend_by_val.Area;    //Area
                case "Area Squared":
                   return Blend_by_val.Area_Squared;    //Area Squared
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Blend_by_val_to_String(Blend_by_val pKey)
        {
           switch (pKey)
           {
                case Blend_by_val.Height:
                   return "Height";    //Height
                case Blend_by_val.Radius:
                   return "Radius";    //Radius
                case Blend_by_val.Area:
                   return "Area";    //Area
                case Blend_by_val.Area_Squared:
                   return "Area Squared";    //Area Squared
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: WoodPole
   // Mirrors: PPLWoodPole : PPLElement
   //--------------------------------------------------------------------------------------------
   public class WoodPole : ElementBase
   {

      public static string gXMLkey = "WoodPole";
      public override string XMLkey() { return gXMLkey; }

      public WoodPole(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Pole_Number = "Unset";
               m_Owner = "Pole";
               m_Structure_Type = Structure_Type_val.Auto;
               m_Class = "4";
               m_LengthInInches = 480;
               m_Species = "SOUTHERN PINE";
               m_Species_Code = "NESC Standard";
               m_BuryDepthInInches = 72;
               m_LineOfLead = 0;
               m_LeanDirection = 0;
               m_LeanAmount = 0;
               m_RadiusAtTipInInches = 3.3422538049298;
               m_GLCircumMethod = GLCircumMethod_val.By_Specs;
               m_Circum6ft = 33.5;
               m_MeasuredRadiusGL = 5.33169059357849;
               m_ApplyEffectiveRadiusGL = false;
               m_EffectiveRadiusGL = 5.33169059357849;
               m_StrengthRemainingGL = 1;
               m_OverturnMoment = 0;
               m_Modulus_of_Rupture = 8000;
               m_Modulus_of_Elasticity = 1600000;
               m_PoissonsRatio = 0.3;
               m_WindDragCoef = 0;
               m_ThermalCoefficient = 2.7E-06;
               m_Density = 0.0347222222222222;
               m_Characteristic_Shear_Strength = 450;
               m_Characteristic_Compression_Strength = 3500;
               m_Effective_Length = -1;
               m_Material_Constant = 1.24;
               m_PoleMfgLength = 480;
               m_Table_No = "ANSI8";
               m_Offset = 0;
               m_Aux_Data_1 = "Unset";
               m_Aux_Data_2 = "Unset";
               m_Aux_Data_3 = "Unset";
               m_Aux_Data_4 = "Unset";
               m_Aux_Data_5 = "Unset";
               m_Aux_Data_6 = "Unset";
               m_Aux_Data_7 = "Unset";
               m_Aux_Data_8 = "Unset";
               m_BucklingConstant = 0;
               m_UseMomentCapacityTable = false;
               m_MomentCapacityTable = new ValTable("Moment;0,50000;");
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Crossarm) return true;
         if(pChildCandidate is PowerEquipment) return true;
         if(pChildCandidate is Streetlight) return true;
         if(pChildCandidate is Insulator) return true;
         if(pChildCandidate is NodeJunction) return true;
         if(pChildCandidate is Riser) return true;
         if(pChildCandidate is GenericEquipment) return true;
         if(pChildCandidate is Anchor) return true;
         if(pChildCandidate is PoleRestoration) return true;
         if(pChildCandidate is LoadCase) return true;
         if(pChildCandidate is WoodPoleDamageOrDecay) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Pole Number
        //   Attr Group:Standard
        //   Description:   Pole identification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Pole_Number;
        [Category("Standard")]
        [Description("Pole Number")]
        public string Pole_Number
        {
           get { return m_Pole_Number; }
           set { m_Pole_Number = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Structure Type
        //   Attr Group:Standard
        //   Description:   Pole structure type specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Auto
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Tangent  (Pole with all wires running in line with each other)
        //        Angle  (Pole with at least one wire that is at an angle relative to the others)
        //        Deadend  (Pole with wires ending at the pole)
        //        Junction  (Pole with wires crossing at or near the pole)
        public enum Structure_Type_val
        {
           [Description("Auto")]
           Auto,    //Automatically determine the structure type from attached equipment
           [Description("Tangent")]
           Tangent,    //Pole with all wires running in line with each other
           [Description("Angle")]
           Angle,    //Pole with at least one wire that is at an angle relative to the others
           [Description("Deadend")]
           Deadend,    //Pole with wires ending at the pole
           [Description("Junction")]
           Junction     //Pole with wires crossing at or near the pole
        }
        private Structure_Type_val m_Structure_Type;
        [Category("Standard")]
        [Description("Structure Type")]
        public Structure_Type_val Structure_Type
        {
           get
           { return m_Structure_Type; }
           set
           { m_Structure_Type = value; }
        }

        public Structure_Type_val String_to_Structure_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Auto":
                   return Structure_Type_val.Auto;    //Automatically determine the structure type from attached equipment
                case "Tangent":
                   return Structure_Type_val.Tangent;    //Pole with all wires running in line with each other
                case "Angle":
                   return Structure_Type_val.Angle;    //Pole with at least one wire that is at an angle relative to the others
                case "Deadend":
                   return Structure_Type_val.Deadend;    //Pole with wires ending at the pole
                case "Junction":
                   return Structure_Type_val.Junction;    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Structure_Type_val_to_String(Structure_Type_val pKey)
        {
           switch (pKey)
           {
                case Structure_Type_val.Auto:
                   return "Auto";    //Automatically determine the structure type from attached equipment
                case Structure_Type_val.Tangent:
                   return "Tangent";    //Pole with all wires running in line with each other
                case Structure_Type_val.Angle:
                   return "Angle";    //Pole with at least one wire that is at an angle relative to the others
                case Structure_Type_val.Deadend:
                   return "Deadend";    //Pole with wires ending at the pole
                case Structure_Type_val.Junction:
                   return "Junction";    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Class
        //   Attr Group:Standard
        //   Alt Display Name:Pole Class
        //   Description:   Pole class specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   4
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Class;
        [Category("Standard")]
        [Description("Class")]
        public string Class
        {
           get { return m_Class; }
           set { m_Class = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Pole Length (ft)
        //   Description:   Pole length in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   480
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Standard")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   Species
        //   Attr Group:Standard
        //   Description:   Wood species of the pole
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   SOUTHERN PINE
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Species;
        [Category("Standard")]
        [Description("Species")]
        public string Species
        {
           get { return m_Species; }
           set { m_Species = value; }
        }



        //   Attr Name:   Species Code
        //   Attr Group:Standard
        //   Alt Display Name:Code
        //   Description:   Code Standard
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   NESC Standard
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Species_Code;
        [Category("Standard")]
        [Description("Species Code")]
        public string Species_Code
        {
           get { return m_Species_Code; }
           set { m_Species_Code = value; }
        }



        //   Attr Name:   BuryDepthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Setting Depth (ft)
        //   Description:   Bury depth in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   72
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BuryDepthInInches;
        [Category("Standard")]
        [Description("BuryDepthInInches")]
        public double BuryDepthInInches
        {
           get { return m_BuryDepthInInches; }
           set { m_BuryDepthInInches = value; }
        }



        //   Attr Name:   LineOfLead
        //   Attr Group:Standard
        //   Alt Display Name:Line of Lead (°)
        //   Description:   The overall line of lead of the entire pole assembly
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LineOfLead;
        [Category("Standard")]
        [Description("LineOfLead")]
        public double LineOfLead
        {
           get { return m_LineOfLead; }
           set { m_LineOfLead = value; }
        }



        //   Attr Name:   LeanDirection
        //   Attr Group:Standard
        //   Alt Display Name:Lean Direction (°)
        //   Description:   Pole lean direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanDirection;
        [Category("Standard")]
        [Description("LeanDirection")]
        public double LeanDirection
        {
           get { return m_LeanDirection; }
           set { m_LeanDirection = value; }
        }



        //   Attr Name:   LeanAmount
        //   Attr Group:Standard
        //   Alt Display Name:Lean Amount (°)
        //   Description:   Pole amount direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanAmount;
        [Category("Standard")]
        [Description("LeanAmount")]
        public double LeanAmount
        {
           get { return m_LeanAmount; }
           set { m_LeanAmount = value; }
        }



        //   Attr Name:   RadiusAtTipInInches
        //   Attr Group:Circumference
        //   Alt Display Name:Tip Circum (in)
        //   Description:   
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.3422538049298
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtTipInInches;
        [Category("Circumference")]
        [Description("RadiusAtTipInInches")]
        public double RadiusAtTipInInches
        {
           get { return m_RadiusAtTipInInches; }
           set { m_RadiusAtTipInInches = value; }
        }



        //   Attr Name:   GLCircumMethod
        //   Attr Group:Circumference
        //   Alt Display Name:GL Circum Method
        //   Description:   Groundline method.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   By Specs
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Measured  (Measured)
        public enum GLCircumMethod_val
        {
           [Description("By Specs")]
           By_Specs,    //By Specs
           [Description("Measured")]
           Measured     //Measured
        }
        private GLCircumMethod_val m_GLCircumMethod;
        [Category("Circumference")]
        [Description("GLCircumMethod")]
        public GLCircumMethod_val GLCircumMethod
        {
           get
           { return m_GLCircumMethod; }
           set
           { m_GLCircumMethod = value; }
        }

        public GLCircumMethod_val String_to_GLCircumMethod_val(string pKey)
        {
           switch (pKey)
           {
                case "By Specs":
                   return GLCircumMethod_val.By_Specs;    //By Specs
                case "Measured":
                   return GLCircumMethod_val.Measured;    //Measured
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string GLCircumMethod_val_to_String(GLCircumMethod_val pKey)
        {
           switch (pKey)
           {
                case GLCircumMethod_val.By_Specs:
                   return "By Specs";    //By Specs
                case GLCircumMethod_val.Measured:
                   return "Measured";    //Measured
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Circum6ft
        //   Attr Group:Circumference
        //   Alt Display Name:6ft Circum (in)
        //   Description:   The pole circumference at the 6 foot point
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   33.5
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        private double m_Circum6ft;
        [Category("Circumference")]
        [Description("Circum6ft")]
        public double Circum6ft
        {
           get { return m_Circum6ft; }
           set { m_Circum6ft = value; }
        }



        //   Attr Name:   MeasuredRadiusGL
        //   Attr Group:Circumference
        //   Alt Display Name:GL Circum (in)
        //   Description:   Measured radius at the groundline
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   5.33169059357849
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MeasuredRadiusGL;
        [Category("Circumference")]
        [Description("MeasuredRadiusGL")]
        public double MeasuredRadiusGL
        {
           get { return m_MeasuredRadiusGL; }
           set { m_MeasuredRadiusGL = value; }
        }



        //   Attr Name:   ApplyEffectiveRadiusGL
        //   Attr Group:Circumference
        //   Alt Display Name:Apply Eff. GL Reduction
        //   Description:   Apply Effective radius at the groundline
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_ApplyEffectiveRadiusGL;
        [Category("Circumference")]
        [Description("ApplyEffectiveRadiusGL")]
        public bool ApplyEffectiveRadiusGL
        {
           get { return m_ApplyEffectiveRadiusGL; }
           set { m_ApplyEffectiveRadiusGL = value; }
        }



        //   Attr Name:   EffectiveRadiusGL
        //   Attr Group:Circumference
        //   Alt Display Name:Eff. GL Circ (in)
        //   Description:   Effective radius at the groundline
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   5.33169059357849
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_EffectiveRadiusGL;
        [Category("Circumference")]
        [Description("EffectiveRadiusGL")]
        public double EffectiveRadiusGL
        {
           get { return m_EffectiveRadiusGL; }
           set { m_EffectiveRadiusGL = value; }
        }



        //   Attr Name:   StrengthRemainingGL
        //   Attr Group:Circumference
        //   Alt Display Name:GL Remaining Strength (%)
        //   Description:   % remaining strength at the groundline
        //   Displayed Units:   store as PERCENT 0 TO 1 display as PERCENT 0 TO 100
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrengthRemainingGL;
        [Category("Circumference")]
        [Description("StrengthRemainingGL")]
        public double StrengthRemainingGL
        {
           get { return m_StrengthRemainingGL; }
           set { m_StrengthRemainingGL = value; }
        }



        //   Attr Name:   SoilClass
        //   Attr Group:Overturn
        //   Alt Display Name:Soil Class
        //   Description:   The class of soil at the site of the anchor
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Class 1  (Very dense and/or cemented sands, coarse gravel and cobbles)
        //        Class 2  (Dense fine sand, very hard silts and clays (may be preloaded))
        //        Class 3  (Dense sands and gravel, hard silts and clays)
        //        Class 4  (Medium dense sandy gravel, very stiff to hard silts and clays)
        //        Class 5  (Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays)
        //        Class 6  (Loose to medium dense fine to coarse sand, firm to stiff clays and silts)
        //        Class 7  (Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill)
        //        Class 8  (Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays)
        //        Unsset  (Unset)
        public enum SoilClass_val
        {
           [Description("Class 0")]
           Class_0,    //Sound hard rock, bedrock, unweathered
           [Description("Class 1")]
           Class_1,    //Very dense and/or cemented sands, coarse gravel and cobbles
           [Description("Class 2")]
           Class_2,    //Dense fine sand, very hard silts and clays (may be preloaded)
           [Description("Class 3")]
           Class_3,    //Dense sands and gravel, hard silts and clays
           [Description("Class 4")]
           Class_4,    //Medium dense sandy gravel, very stiff to hard silts and clays
           [Description("Class 5")]
           Class_5,    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
           [Description("Class 6")]
           Class_6,    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
           [Description("Class 7")]
           Class_7,    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
           [Description("Class 8")]
           Class_8,    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
           [Description("Unsset")]
           Unsset     //Unset
        }
        private SoilClass_val m_SoilClass;
        [Category("Overturn")]
        [Description("SoilClass")]
        public SoilClass_val SoilClass
        {
           get
           { return m_SoilClass; }
           set
           { m_SoilClass = value; }
        }

        public SoilClass_val String_to_SoilClass_val(string pKey)
        {
           switch (pKey)
           {
                case "Class 0":
                   return SoilClass_val.Class_0;    //Sound hard rock, bedrock, unweathered
                case "Class 1":
                   return SoilClass_val.Class_1;    //Very dense and/or cemented sands, coarse gravel and cobbles
                case "Class 2":
                   return SoilClass_val.Class_2;    //Dense fine sand, very hard silts and clays (may be preloaded)
                case "Class 3":
                   return SoilClass_val.Class_3;    //Dense sands and gravel, hard silts and clays
                case "Class 4":
                   return SoilClass_val.Class_4;    //Medium dense sandy gravel, very stiff to hard silts and clays
                case "Class 5":
                   return SoilClass_val.Class_5;    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case "Class 6":
                   return SoilClass_val.Class_6;    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case "Class 7":
                   return SoilClass_val.Class_7;    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case "Class 8":
                   return SoilClass_val.Class_8;    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case "Unsset":
                   return SoilClass_val.Unsset;    //Unset
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string SoilClass_val_to_String(SoilClass_val pKey)
        {
           switch (pKey)
           {
                case SoilClass_val.Class_0:
                   return "Class 0";    //Sound hard rock, bedrock, unweathered
                case SoilClass_val.Class_1:
                   return "Class 1";    //Very dense and/or cemented sands, coarse gravel and cobbles
                case SoilClass_val.Class_2:
                   return "Class 2";    //Dense fine sand, very hard silts and clays (may be preloaded)
                case SoilClass_val.Class_3:
                   return "Class 3";    //Dense sands and gravel, hard silts and clays
                case SoilClass_val.Class_4:
                   return "Class 4";    //Medium dense sandy gravel, very stiff to hard silts and clays
                case SoilClass_val.Class_5:
                   return "Class 5";    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case SoilClass_val.Class_6:
                   return "Class 6";    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case SoilClass_val.Class_7:
                   return "Class 7";    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case SoilClass_val.Class_8:
                   return "Class 8";    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case SoilClass_val.Unsset:
                   return "Unsset";    //Unset
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   OverturnMoment
        //   Attr Group:Overturn
        //   Alt Display Name:Overturn Moment (ft-lbs)
        //   Description:   Overturn Moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.#
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OverturnMoment;
        [Category("Overturn")]
        [Description("OverturnMoment")]
        public double OverturnMoment
        {
           get { return m_OverturnMoment; }
           set { m_OverturnMoment = value; }
        }



        //   Attr Name:   Modulus of Rupture
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Modulus of Rupture (psi)
        //   Description:   Modulus of rupture for the given species
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   8000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Rupture;
        [Category("Phys. Consts")]
        [Description("Modulus of Rupture")]
        public double Modulus_of_Rupture
        {
           get { return m_Modulus_of_Rupture; }
           set { m_Modulus_of_Rupture = value; }
        }



        //   Attr Name:   Modulus of Elasticity
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   Modulus of elasticty for the material
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   1600000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Elasticity;
        [Category("Phys. Consts")]
        [Description("Modulus of Elasticity")]
        public double Modulus_of_Elasticity
        {
           get { return m_Modulus_of_Elasticity; }
           set { m_Modulus_of_Elasticity = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.3
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Phys. Consts")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Phys. Consts")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000027
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Phys. Consts")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   Density
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Density (lb/ft^3)
        //   Description:   Density for the given species in lbs per cubic inch
        //   Displayed Units:   store as POUNDS PER CUBIC INCH display as POUNDS PER CUBIC FOOT or KILOGRAMS PER CUBIC METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0347222222222222
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Density;
        [Category("Phys. Consts")]
        [Description("Density")]
        public double Density
        {
           get { return m_Density; }
           set { m_Density = value; }
        }



        //   Attr Name:   Characteristic Shear Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Shear Str (psi)
        //   Description:   Characteristic Shear Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   450
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Shear_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Shear Strength")]
        public double Characteristic_Shear_Strength
        {
           get { return m_Characteristic_Shear_Strength; }
           set { m_Characteristic_Shear_Strength = value; }
        }



        //   Attr Name:   Characteristic Compression Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Compression Str (psi)
        //   Description:   Characteristic Compression Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   3500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Compression_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Compression Strength")]
        public double Characteristic_Compression_Strength
        {
           get { return m_Characteristic_Compression_Strength; }
           set { m_Characteristic_Compression_Strength = value; }
        }



        //   Attr Name:   Effective Length
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Effective Length (ft)
        //   Description:   Effective Length
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0#
        //   Attribute Type:   FLOAT
        //   Default Value:   -1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Effective_Length;
        [Category("AS/NZS 7000")]
        [Description("Effective Length")]
        public double Effective_Length
        {
           get { return m_Effective_Length; }
           set { m_Effective_Length = value; }
        }



        //   Attr Name:   Material Constant
        //   Attr Group:AS/NZS 7000
        //   Description:   Material Constant
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   1.24
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Material_Constant;
        [Category("AS/NZS 7000")]
        [Description("Material Constant")]
        public double Material_Constant
        {
           get { return m_Material_Constant; }
           set { m_Material_Constant = value; }
        }



        //   Attr Name:   PoleMfgLength
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Pole Mfg Length (ft)
        //   Description:   Pole manufactured length in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   480
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoleMfgLength;
        [Category("Phys. Consts")]
        [Description("PoleMfgLength")]
        public double PoleMfgLength
        {
           get { return m_PoleMfgLength; }
           set { m_PoleMfgLength = value; }
        }



        //   Attr Name:   Table No
        //   Attr Group:Phys. Consts
        //   Description:   Table Designation
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   ANSI8
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Table_No;
        [Category("Phys. Consts")]
        [Description("Table No")]
        public string Table_No
        {
           get { return m_Table_No; }
           set { m_Table_No = value; }
        }



        //   Attr Name:   Offset
        //   Attr Group:Multi Pole
        //   Alt Display Name:Offset (ft)
        //   Description:   Pole offset in feet
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_Offset;
        [Category("Multi Pole")]
        [Description("Offset")]
        public double Offset
        {
           get { return m_Offset; }
           set { m_Offset = value; }
        }



        //   Attr Name:   Aux Data 1
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_1;
        [Category("User Data")]
        [Description("Aux Data 1")]
        public string Aux_Data_1
        {
           get { return m_Aux_Data_1; }
           set { m_Aux_Data_1 = value; }
        }



        //   Attr Name:   Aux Data 2
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_2;
        [Category("User Data")]
        [Description("Aux Data 2")]
        public string Aux_Data_2
        {
           get { return m_Aux_Data_2; }
           set { m_Aux_Data_2 = value; }
        }



        //   Attr Name:   Aux Data 3
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_3;
        [Category("User Data")]
        [Description("Aux Data 3")]
        public string Aux_Data_3
        {
           get { return m_Aux_Data_3; }
           set { m_Aux_Data_3 = value; }
        }



        //   Attr Name:   Aux Data 4
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_4;
        [Category("User Data")]
        [Description("Aux Data 4")]
        public string Aux_Data_4
        {
           get { return m_Aux_Data_4; }
           set { m_Aux_Data_4 = value; }
        }



        //   Attr Name:   Aux Data 5
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_5;
        [Category("User Data")]
        [Description("Aux Data 5")]
        public string Aux_Data_5
        {
           get { return m_Aux_Data_5; }
           set { m_Aux_Data_5 = value; }
        }



        //   Attr Name:   Aux Data 6
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_6;
        [Category("User Data")]
        [Description("Aux Data 6")]
        public string Aux_Data_6
        {
           get { return m_Aux_Data_6; }
           set { m_Aux_Data_6 = value; }
        }



        //   Attr Name:   Aux Data 7
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_7;
        [Category("User Data")]
        [Description("Aux Data 7")]
        public string Aux_Data_7
        {
           get { return m_Aux_Data_7; }
           set { m_Aux_Data_7 = value; }
        }



        //   Attr Name:   Aux Data 8
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_8;
        [Category("User Data")]
        [Description("Aux Data 8")]
        public string Aux_Data_8
        {
           get { return m_Aux_Data_8; }
           set { m_Aux_Data_8 = value; }
        }



        //   Attr Name:   BucklingConstant
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Buckling Constant
        //   Description:   Column buckling constant
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.####
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BucklingConstant;
        [Category("Phys. Consts")]
        [Description("BucklingConstant")]
        public double BucklingConstant
        {
           get { return m_BucklingConstant; }
           set { m_BucklingConstant = value; }
        }



        //   Attr Name:   UseMomentCapacityTable
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Override Moment Cap
        //   Description:   Use the moment capacity table
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_UseMomentCapacityTable;
        [Category("Phys. Consts")]
        [Description("UseMomentCapacityTable")]
        public bool UseMomentCapacityTable
        {
           get { return m_UseMomentCapacityTable; }
           set { m_UseMomentCapacityTable = value; }
        }



        //   Attr Name:   MomentCapacityTable
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Moment Cap (ft-lb)
        //   Description:   The moment capacity table
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   MOMENT_TABLE
        //   Default Value:   Moment;0,50000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_MomentCapacityTable = new ValTable();
        [Category("Phys. Consts")]
        [Description("MomentCapacityTable")]
        public ValTable MomentCapacityTable
        {
           get { return m_MomentCapacityTable; }
           set { m_MomentCapacityTable = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: SteelPole
   // Mirrors: PPLSteelPole : PPLElement
   //--------------------------------------------------------------------------------------------
   public class SteelPole : ElementBase
   {

      public static string gXMLkey = "SteelPole";
      public override string XMLkey() { return gXMLkey; }

      public SteelPole(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Pole_Number = "Unset";
               m_Owner = "Pole";
               m_Structure_Type = Structure_Type_val.Auto;
               m_Class = "4";
               m_LengthInInches = 480;
               m_CatalogName = "User Defined";
               m_Pole_Code = Pole_Code_val.GO_95;
               m_Shape = Shape_val.Round;
               m_Faces = 12;
               m_Mount = Mount_val.Pedestal;
               m_PedestalRadius = 16;
               m_BuryDepthInInches = 72;
               m_LineOfLead = 0;
               m_LeanDirection = 0;
               m_LeanAmount = 0;
               m_RadiusAtTipInInches = 3.3422538049298;
               m_RadiusAtBaseInInches = 9;
               m_OverturnMoment = 0;
               m_Modulus_of_Elasticity = 29000000;
               m_PoissonsRatio = 0.4;
               m_WindDragCoef = 0;
               m_ThermalCoefficient = 2.7E-06;
               m_Density = 0.0347222222222222;
               m_Characteristic_Shear_Strength = 450;
               m_Characteristic_Compression_Strength = 3500;
               m_Effective_Length = -1;
               m_Material_Constant = 1.24;
               m_Offset = 0;
               m_Aux_Data_1 = "Unset";
               m_Aux_Data_2 = "Unset";
               m_Aux_Data_3 = "Unset";
               m_Aux_Data_4 = "Unset";
               m_Aux_Data_5 = "Unset";
               m_Aux_Data_6 = "Unset";
               m_Aux_Data_7 = "Unset";
               m_Aux_Data_8 = "Unset";
               m_ThicknessTable = new ValTable("Thick;0,0.25;");
               m_PedestalMomentCapacity = 90000;
               m_PedestalBucklingCapacity = 9000;
               m_DistToGrade = 0;
               m_MomentCapacityTable = new ValTable("Moment;0,50000;");
               m_BucklingCapacityTable = new ValTable("Buckling;0,5000;");
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Crossarm) return true;
         if(pChildCandidate is PowerEquipment) return true;
         if(pChildCandidate is Streetlight) return true;
         if(pChildCandidate is Insulator) return true;
         if(pChildCandidate is NodeJunction) return true;
         if(pChildCandidate is CapacityAdjustment) return true;
         if(pChildCandidate is Riser) return true;
         if(pChildCandidate is GenericEquipment) return true;
         if(pChildCandidate is Anchor) return true;
         if(pChildCandidate is LoadCase) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Pole Number
        //   Attr Group:Standard
        //   Description:   Pole identification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Pole_Number;
        [Category("Standard")]
        [Description("Pole Number")]
        public string Pole_Number
        {
           get { return m_Pole_Number; }
           set { m_Pole_Number = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Structure Type
        //   Attr Group:Standard
        //   Description:   Pole structure type specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Auto
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Tangent  (Pole with all wires running in line with each other)
        //        Angle  (Pole with at least one wire that is at an angle relative to the others)
        //        Deadend  (Pole with wires ending at the pole)
        //        Junction  (Pole with wires crossing at or near the pole)
        public enum Structure_Type_val
        {
           [Description("Auto")]
           Auto,    //Automatically determine the structure type from attached equipment
           [Description("Tangent")]
           Tangent,    //Pole with all wires running in line with each other
           [Description("Angle")]
           Angle,    //Pole with at least one wire that is at an angle relative to the others
           [Description("Deadend")]
           Deadend,    //Pole with wires ending at the pole
           [Description("Junction")]
           Junction     //Pole with wires crossing at or near the pole
        }
        private Structure_Type_val m_Structure_Type;
        [Category("Standard")]
        [Description("Structure Type")]
        public Structure_Type_val Structure_Type
        {
           get
           { return m_Structure_Type; }
           set
           { m_Structure_Type = value; }
        }

        public Structure_Type_val String_to_Structure_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Auto":
                   return Structure_Type_val.Auto;    //Automatically determine the structure type from attached equipment
                case "Tangent":
                   return Structure_Type_val.Tangent;    //Pole with all wires running in line with each other
                case "Angle":
                   return Structure_Type_val.Angle;    //Pole with at least one wire that is at an angle relative to the others
                case "Deadend":
                   return Structure_Type_val.Deadend;    //Pole with wires ending at the pole
                case "Junction":
                   return Structure_Type_val.Junction;    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Structure_Type_val_to_String(Structure_Type_val pKey)
        {
           switch (pKey)
           {
                case Structure_Type_val.Auto:
                   return "Auto";    //Automatically determine the structure type from attached equipment
                case Structure_Type_val.Tangent:
                   return "Tangent";    //Pole with all wires running in line with each other
                case Structure_Type_val.Angle:
                   return "Angle";    //Pole with at least one wire that is at an angle relative to the others
                case Structure_Type_val.Deadend:
                   return "Deadend";    //Pole with wires ending at the pole
                case Structure_Type_val.Junction:
                   return "Junction";    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Class
        //   Attr Group:Standard
        //   Alt Display Name:Pole Class
        //   Description:   Pole class specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   4
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Class;
        [Category("Standard")]
        [Description("Class")]
        public string Class
        {
           get { return m_Class; }
           set { m_Class = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Length (ft)
        //   Description:   Pole length in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   480
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Standard")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   CatalogName
        //   Attr Group:Standard
        //   Alt Display Name:Catalog Name
        //   Description:   Wood species of the pole
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   User Defined
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_CatalogName;
        [Category("Standard")]
        [Description("CatalogName")]
        public string CatalogName
        {
           get { return m_CatalogName; }
           set { m_CatalogName = value; }
        }



        //   Attr Name:   Pole Code
        //   Attr Group:Standard
        //   Alt Display Name:Code
        //   Description:   Pole Code
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   GO 95
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        GO 95  (GO 95)
        //        CSA C22.3 No. 1-10  (CSA C22.3 No. 1-10)
        public enum Pole_Code_val
        {
           [Description("NESC C2-2007")]
           NESC_C2_2007,    //NESC C2-2007
           [Description("GO 95")]
           GO_95,    //GO 95
           [Description("CSA C22.3 No. 1-10")]
           CSA_C22_3_No__1_10     //CSA C22.3 No. 1-10
        }
        private Pole_Code_val m_Pole_Code;
        [Category("Standard")]
        [Description("Pole Code")]
        public Pole_Code_val Pole_Code
        {
           get
           { return m_Pole_Code; }
           set
           { m_Pole_Code = value; }
        }

        public Pole_Code_val String_to_Pole_Code_val(string pKey)
        {
           switch (pKey)
           {
                case "NESC C2-2007":
                   return Pole_Code_val.NESC_C2_2007;    //NESC C2-2007
                case "GO 95":
                   return Pole_Code_val.GO_95;    //GO 95
                case "CSA C22.3 No. 1-10":
                   return Pole_Code_val.CSA_C22_3_No__1_10;    //CSA C22.3 No. 1-10
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Pole_Code_val_to_String(Pole_Code_val pKey)
        {
           switch (pKey)
           {
                case Pole_Code_val.NESC_C2_2007:
                   return "NESC C2-2007";    //NESC C2-2007
                case Pole_Code_val.GO_95:
                   return "GO 95";    //GO 95
                case Pole_Code_val.CSA_C22_3_No__1_10:
                   return "CSA C22.3 No. 1-10";    //CSA C22.3 No. 1-10
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Shape
        //   Attr Group:Standard
        //   Description:   Cross section shape
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Round
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Polygonal  (Polygonal)
        public enum Shape_val
        {
           [Description("Round")]
           Round,    //Round
           [Description("Polygonal")]
           Polygonal     //Polygonal
        }
        private Shape_val m_Shape;
        [Category("Standard")]
        [Description("Shape")]
        public Shape_val Shape
        {
           get
           { return m_Shape; }
           set
           { m_Shape = value; }
        }

        public Shape_val String_to_Shape_val(string pKey)
        {
           switch (pKey)
           {
                case "Round":
                   return Shape_val.Round;    //Round
                case "Polygonal":
                   return Shape_val.Polygonal;    //Polygonal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Shape_val_to_String(Shape_val pKey)
        {
           switch (pKey)
           {
                case Shape_val.Round:
                   return "Round";    //Round
                case Shape_val.Polygonal:
                   return "Polygonal";    //Polygonal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Faces
        //   Attr Group:Standard
        //   Alt Display Name:Polygon Faces
        //   Description:   Number of polygon faces
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   PLUSMINUS
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_Faces;
        [Category("Standard")]
        [Description("Faces")]
        public int Faces
        {
           get { return m_Faces; }
           set { m_Faces = value; }
        }



        //   Attr Name:   Mount
        //   Attr Group:Installation
        //   Alt Display Name:Mount Type
        //   Description:   Mount type
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Pedestal
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Pedestal  (Pedestal)
        public enum Mount_val
        {
           [Description("Embedded")]
           Embedded,    //Embedded
           [Description("Pedestal")]
           Pedestal     //Pedestal
        }
        private Mount_val m_Mount;
        [Category("Installation")]
        [Description("Mount")]
        public Mount_val Mount
        {
           get
           { return m_Mount; }
           set
           { m_Mount = value; }
        }

        public Mount_val String_to_Mount_val(string pKey)
        {
           switch (pKey)
           {
                case "Embedded":
                   return Mount_val.Embedded;    //Embedded
                case "Pedestal":
                   return Mount_val.Pedestal;    //Pedestal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Mount_val_to_String(Mount_val pKey)
        {
           switch (pKey)
           {
                case Mount_val.Embedded:
                   return "Embedded";    //Embedded
                case Mount_val.Pedestal:
                   return "Pedestal";    //Pedestal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   PedestalRadius
        //   Attr Group:Installation
        //   Alt Display Name:Pedestal Radius (in)
        //   Description:   Radius of pedestal mount 
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   16.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalRadius;
        [Category("Installation")]
        [Description("PedestalRadius")]
        public double PedestalRadius
        {
           get { return m_PedestalRadius; }
           set { m_PedestalRadius = value; }
        }



        //   Attr Name:   BuryDepthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Setting Depth (ft)
        //   Description:   Bury depth in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   72
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BuryDepthInInches;
        [Category("Standard")]
        [Description("BuryDepthInInches")]
        public double BuryDepthInInches
        {
           get { return m_BuryDepthInInches; }
           set { m_BuryDepthInInches = value; }
        }



        //   Attr Name:   LineOfLead
        //   Attr Group:Standard
        //   Alt Display Name:Line of Lead (°)
        //   Description:   The overall line of lead of the entire pole assembly
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LineOfLead;
        [Category("Standard")]
        [Description("LineOfLead")]
        public double LineOfLead
        {
           get { return m_LineOfLead; }
           set { m_LineOfLead = value; }
        }



        //   Attr Name:   LeanDirection
        //   Attr Group:Standard
        //   Alt Display Name:Lean Direction (°)
        //   Description:   Pole lean direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanDirection;
        [Category("Standard")]
        [Description("LeanDirection")]
        public double LeanDirection
        {
           get { return m_LeanDirection; }
           set { m_LeanDirection = value; }
        }



        //   Attr Name:   LeanAmount
        //   Attr Group:Standard
        //   Alt Display Name:Lean Amount (°)
        //   Description:   Pole amount direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanAmount;
        [Category("Standard")]
        [Description("LeanAmount")]
        public double LeanAmount
        {
           get { return m_LeanAmount; }
           set { m_LeanAmount = value; }
        }



        //   Attr Name:   RadiusAtTipInInches
        //   Attr Group:Circumference
        //   Alt Display Name:Tip Circum (in)
        //   Description:   
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.3422538049298
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtTipInInches;
        [Category("Circumference")]
        [Description("RadiusAtTipInInches")]
        public double RadiusAtTipInInches
        {
           get { return m_RadiusAtTipInInches; }
           set { m_RadiusAtTipInInches = value; }
        }



        //   Attr Name:   RadiusAtBaseInInches
        //   Attr Group:Circumference
        //   Alt Display Name:Base Circum (in)
        //   Description:   Radius At Base
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   9
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtBaseInInches;
        [Category("Circumference")]
        [Description("RadiusAtBaseInInches")]
        public double RadiusAtBaseInInches
        {
           get { return m_RadiusAtBaseInInches; }
           set { m_RadiusAtBaseInInches = value; }
        }



        //   Attr Name:   SoilClass
        //   Attr Group:Overturn
        //   Alt Display Name:Soil Class
        //   Description:   The class of soil at the site of the anchor
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Class 1  (Very dense and/or cemented sands, coarse gravel and cobbles)
        //        Class 2  (Dense fine sand, very hard silts and clays (may be preloaded))
        //        Class 3  (Dense sands and gravel, hard silts and clays)
        //        Class 4  (Medium dense sandy gravel, very stiff to hard silts and clays)
        //        Class 5  (Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays)
        //        Class 6  (Loose to medium dense fine to coarse sand, firm to stiff clays and silts)
        //        Class 7  (Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill)
        //        Class 8  (Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays)
        //        Unsset  (Unset)
        public enum SoilClass_val
        {
           [Description("Class 0")]
           Class_0,    //Sound hard rock, bedrock, unweathered
           [Description("Class 1")]
           Class_1,    //Very dense and/or cemented sands, coarse gravel and cobbles
           [Description("Class 2")]
           Class_2,    //Dense fine sand, very hard silts and clays (may be preloaded)
           [Description("Class 3")]
           Class_3,    //Dense sands and gravel, hard silts and clays
           [Description("Class 4")]
           Class_4,    //Medium dense sandy gravel, very stiff to hard silts and clays
           [Description("Class 5")]
           Class_5,    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
           [Description("Class 6")]
           Class_6,    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
           [Description("Class 7")]
           Class_7,    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
           [Description("Class 8")]
           Class_8,    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
           [Description("Unsset")]
           Unsset     //Unset
        }
        private SoilClass_val m_SoilClass;
        [Category("Overturn")]
        [Description("SoilClass")]
        public SoilClass_val SoilClass
        {
           get
           { return m_SoilClass; }
           set
           { m_SoilClass = value; }
        }

        public SoilClass_val String_to_SoilClass_val(string pKey)
        {
           switch (pKey)
           {
                case "Class 0":
                   return SoilClass_val.Class_0;    //Sound hard rock, bedrock, unweathered
                case "Class 1":
                   return SoilClass_val.Class_1;    //Very dense and/or cemented sands, coarse gravel and cobbles
                case "Class 2":
                   return SoilClass_val.Class_2;    //Dense fine sand, very hard silts and clays (may be preloaded)
                case "Class 3":
                   return SoilClass_val.Class_3;    //Dense sands and gravel, hard silts and clays
                case "Class 4":
                   return SoilClass_val.Class_4;    //Medium dense sandy gravel, very stiff to hard silts and clays
                case "Class 5":
                   return SoilClass_val.Class_5;    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case "Class 6":
                   return SoilClass_val.Class_6;    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case "Class 7":
                   return SoilClass_val.Class_7;    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case "Class 8":
                   return SoilClass_val.Class_8;    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case "Unsset":
                   return SoilClass_val.Unsset;    //Unset
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string SoilClass_val_to_String(SoilClass_val pKey)
        {
           switch (pKey)
           {
                case SoilClass_val.Class_0:
                   return "Class 0";    //Sound hard rock, bedrock, unweathered
                case SoilClass_val.Class_1:
                   return "Class 1";    //Very dense and/or cemented sands, coarse gravel and cobbles
                case SoilClass_val.Class_2:
                   return "Class 2";    //Dense fine sand, very hard silts and clays (may be preloaded)
                case SoilClass_val.Class_3:
                   return "Class 3";    //Dense sands and gravel, hard silts and clays
                case SoilClass_val.Class_4:
                   return "Class 4";    //Medium dense sandy gravel, very stiff to hard silts and clays
                case SoilClass_val.Class_5:
                   return "Class 5";    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case SoilClass_val.Class_6:
                   return "Class 6";    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case SoilClass_val.Class_7:
                   return "Class 7";    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case SoilClass_val.Class_8:
                   return "Class 8";    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case SoilClass_val.Unsset:
                   return "Unsset";    //Unset
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   OverturnMoment
        //   Attr Group:Overturn
        //   Alt Display Name:Overturn Moment (ft-lbs)
        //   Description:   Overturn Moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.#
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OverturnMoment;
        [Category("Overturn")]
        [Description("OverturnMoment")]
        public double OverturnMoment
        {
           get { return m_OverturnMoment; }
           set { m_OverturnMoment = value; }
        }



        //   Attr Name:   Modulus of Elasticity
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   Modulus of elasticty for the material
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   29000000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Elasticity;
        [Category("Phys. Consts")]
        [Description("Modulus of Elasticity")]
        public double Modulus_of_Elasticity
        {
           get { return m_Modulus_of_Elasticity; }
           set { m_Modulus_of_Elasticity = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.4
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Phys. Consts")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Phys. Consts")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000027
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Phys. Consts")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   Density
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Density (lb/ft^3)
        //   Description:   Density for the given species in lbs per cubic inch
        //   Displayed Units:   store as POUNDS PER CUBIC INCH display as POUNDS PER CUBIC FOOT or KILOGRAMS PER CUBIC METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0347222222222222
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Density;
        [Category("Phys. Consts")]
        [Description("Density")]
        public double Density
        {
           get { return m_Density; }
           set { m_Density = value; }
        }



        //   Attr Name:   Characteristic Shear Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Shear Str (psi)
        //   Description:   Characteristic Shear Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   450
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Shear_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Shear Strength")]
        public double Characteristic_Shear_Strength
        {
           get { return m_Characteristic_Shear_Strength; }
           set { m_Characteristic_Shear_Strength = value; }
        }



        //   Attr Name:   Characteristic Compression Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Compression Str (psi)
        //   Description:   Characteristic Compression Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   3500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Compression_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Compression Strength")]
        public double Characteristic_Compression_Strength
        {
           get { return m_Characteristic_Compression_Strength; }
           set { m_Characteristic_Compression_Strength = value; }
        }



        //   Attr Name:   Effective Length
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Effective Length (ft)
        //   Description:   Effective Length
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0#
        //   Attribute Type:   FLOAT
        //   Default Value:   -1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Effective_Length;
        [Category("AS/NZS 7000")]
        [Description("Effective Length")]
        public double Effective_Length
        {
           get { return m_Effective_Length; }
           set { m_Effective_Length = value; }
        }



        //   Attr Name:   Material Constant
        //   Attr Group:AS/NZS 7000
        //   Description:   Material Constant
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   1.24
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Material_Constant;
        [Category("AS/NZS 7000")]
        [Description("Material Constant")]
        public double Material_Constant
        {
           get { return m_Material_Constant; }
           set { m_Material_Constant = value; }
        }



        //   Attr Name:   Offset
        //   Attr Group:Multi Pole
        //   Alt Display Name:Offset (ft)
        //   Description:   Pole offset in feet
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_Offset;
        [Category("Multi Pole")]
        [Description("Offset")]
        public double Offset
        {
           get { return m_Offset; }
           set { m_Offset = value; }
        }



        //   Attr Name:   Aux Data 1
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_1;
        [Category("User Data")]
        [Description("Aux Data 1")]
        public string Aux_Data_1
        {
           get { return m_Aux_Data_1; }
           set { m_Aux_Data_1 = value; }
        }



        //   Attr Name:   Aux Data 2
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_2;
        [Category("User Data")]
        [Description("Aux Data 2")]
        public string Aux_Data_2
        {
           get { return m_Aux_Data_2; }
           set { m_Aux_Data_2 = value; }
        }



        //   Attr Name:   Aux Data 3
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_3;
        [Category("User Data")]
        [Description("Aux Data 3")]
        public string Aux_Data_3
        {
           get { return m_Aux_Data_3; }
           set { m_Aux_Data_3 = value; }
        }



        //   Attr Name:   Aux Data 4
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_4;
        [Category("User Data")]
        [Description("Aux Data 4")]
        public string Aux_Data_4
        {
           get { return m_Aux_Data_4; }
           set { m_Aux_Data_4 = value; }
        }



        //   Attr Name:   Aux Data 5
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_5;
        [Category("User Data")]
        [Description("Aux Data 5")]
        public string Aux_Data_5
        {
           get { return m_Aux_Data_5; }
           set { m_Aux_Data_5 = value; }
        }



        //   Attr Name:   Aux Data 6
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_6;
        [Category("User Data")]
        [Description("Aux Data 6")]
        public string Aux_Data_6
        {
           get { return m_Aux_Data_6; }
           set { m_Aux_Data_6 = value; }
        }



        //   Attr Name:   Aux Data 7
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_7;
        [Category("User Data")]
        [Description("Aux Data 7")]
        public string Aux_Data_7
        {
           get { return m_Aux_Data_7; }
           set { m_Aux_Data_7 = value; }
        }



        //   Attr Name:   Aux Data 8
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_8;
        [Category("User Data")]
        [Description("Aux Data 8")]
        public string Aux_Data_8
        {
           get { return m_Aux_Data_8; }
           set { m_Aux_Data_8 = value; }
        }



        //   Attr Name:   ThicknessTable
        //   Attr Group:Standard
        //   Alt Display Name:Thickness
        //   Description:   The material thickness
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   THICKNESS_TABLE
        //   Default Value:   Thick;0,0.25;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_ThicknessTable = new ValTable();
        [Category("Standard")]
        [Description("ThicknessTable")]
        public ValTable ThicknessTable
        {
           get { return m_ThicknessTable; }
           set { m_ThicknessTable = value; }
        }



        //   Attr Name:   PedestalMomentCapacity
        //   Attr Group:Installation
        //   Alt Display Name:Ped Mom Cap (ft-lb)
        //   Description:   The pedestal moment capacity
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   90000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalMomentCapacity;
        [Category("Installation")]
        [Description("PedestalMomentCapacity")]
        public double PedestalMomentCapacity
        {
           get { return m_PedestalMomentCapacity; }
           set { m_PedestalMomentCapacity = value; }
        }



        //   Attr Name:   PedestalBucklingCapacity
        //   Attr Group:Installation
        //   Alt Display Name:Ped Buck Cap (lbs)
        //   Description:   The pedestal buckling capacity
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   9000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalBucklingCapacity;
        [Category("Installation")]
        [Description("PedestalBucklingCapacity")]
        public double PedestalBucklingCapacity
        {
           get { return m_PedestalBucklingCapacity; }
           set { m_PedestalBucklingCapacity = value; }
        }



        //   Attr Name:   DistToGrade
        //   Attr Group:Installation
        //   Alt Display Name:Dist To Grade (ft)
        //   Description:   Distance to grade of pedastal mount 
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DistToGrade;
        [Category("Installation")]
        [Description("DistToGrade")]
        public double DistToGrade
        {
           get { return m_DistToGrade; }
           set { m_DistToGrade = value; }
        }



        //   Attr Name:   MomentCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Moment Cap (ft-lb)
        //   Description:   The moment capacity table
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   MOMENT_TABLE
        //   Default Value:   Moment;0,50000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_MomentCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("MomentCapacityTable")]
        public ValTable MomentCapacityTable
        {
           get { return m_MomentCapacityTable; }
           set { m_MomentCapacityTable = value; }
        }



        //   Attr Name:   BucklingCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Buckling Cap (lbs)
        //   Description:   The buckling capacity table
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BUCKLING_TABLE
        //   Default Value:   Buckling;0,5000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_BucklingCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("BucklingCapacityTable")]
        public ValTable BucklingCapacityTable
        {
           get { return m_BucklingCapacityTable; }
           set { m_BucklingCapacityTable = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: ConcretePole
   // Mirrors: PPLConcretePole : PPLElement
   //--------------------------------------------------------------------------------------------
   public class ConcretePole : ElementBase
   {

      public static string gXMLkey = "ConcretePole";
      public override string XMLkey() { return gXMLkey; }

      public ConcretePole(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Pole_Number = "Unset";
               m_Owner = "Pole";
               m_Structure_Type = Structure_Type_val.Auto;
               m_Class = "Unset";
               m_LengthInInches = 480;
               m_CatalogName = "User Defined";
               m_Pole_Code = Pole_Code_val.GO_95;
               m_Shape = Shape_val.Round;
               m_Faces = 12;
               m_Mount = Mount_val.Embedded;
               m_PedestalRadius = 16;
               m_BuryDepthInInches = 72;
               m_LineOfLead = 0;
               m_LeanDirection = 0;
               m_LeanAmount = 0;
               m_RadiusAtTipInInches = 3.3422538049298;
               m_RadiusAtBaseInInches = 9;
               m_OverturnMoment = 0;
               m_Modulus_of_Elasticity = 4350000;
               m_PoissonsRatio = 0.2;
               m_WindDragCoef = 0;
               m_ThermalCoefficient = 2.7E-06;
               m_Density = 0.0347222222222222;
               m_Characteristic_Shear_Strength = 450;
               m_Characteristic_Compression_Strength = 3500;
               m_Effective_Length = -1;
               m_Material_Constant = 1.24;
               m_Offset = 0;
               m_Aux_Data_1 = "Unset";
               m_Aux_Data_2 = "Unset";
               m_Aux_Data_3 = "Unset";
               m_Aux_Data_4 = "Unset";
               m_Aux_Data_5 = "Unset";
               m_Aux_Data_6 = "Unset";
               m_Aux_Data_7 = "Unset";
               m_Aux_Data_8 = "Unset";
               m_ThicknessTable = new ValTable("Thick;0,0.25;");
               m_PedestalMomentCapacity = 90000;
               m_PedestalBucklingCapacity = 9000;
               m_DistToGrade = 0;
               m_MomentCapacityTable = new ValTable("Moment;0,50000;");
               m_BucklingCapacityTable = new ValTable("Buckling;0,5000;");
               m_Torque = 600;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Crossarm) return true;
         if(pChildCandidate is PowerEquipment) return true;
         if(pChildCandidate is Streetlight) return true;
         if(pChildCandidate is Insulator) return true;
         if(pChildCandidate is NodeJunction) return true;
         if(pChildCandidate is CapacityAdjustment) return true;
         if(pChildCandidate is Riser) return true;
         if(pChildCandidate is GenericEquipment) return true;
         if(pChildCandidate is Anchor) return true;
         if(pChildCandidate is LoadCase) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Pole Number
        //   Attr Group:Standard
        //   Description:   Pole identification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Pole_Number;
        [Category("Standard")]
        [Description("Pole Number")]
        public string Pole_Number
        {
           get { return m_Pole_Number; }
           set { m_Pole_Number = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Structure Type
        //   Attr Group:Standard
        //   Description:   Pole structure type specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Auto
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Tangent  (Pole with all wires running in line with each other)
        //        Angle  (Pole with at least one wire that is at an angle relative to the others)
        //        Deadend  (Pole with wires ending at the pole)
        //        Junction  (Pole with wires crossing at or near the pole)
        public enum Structure_Type_val
        {
           [Description("Auto")]
           Auto,    //Automatically determine the structure type from attached equipment
           [Description("Tangent")]
           Tangent,    //Pole with all wires running in line with each other
           [Description("Angle")]
           Angle,    //Pole with at least one wire that is at an angle relative to the others
           [Description("Deadend")]
           Deadend,    //Pole with wires ending at the pole
           [Description("Junction")]
           Junction     //Pole with wires crossing at or near the pole
        }
        private Structure_Type_val m_Structure_Type;
        [Category("Standard")]
        [Description("Structure Type")]
        public Structure_Type_val Structure_Type
        {
           get
           { return m_Structure_Type; }
           set
           { m_Structure_Type = value; }
        }

        public Structure_Type_val String_to_Structure_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Auto":
                   return Structure_Type_val.Auto;    //Automatically determine the structure type from attached equipment
                case "Tangent":
                   return Structure_Type_val.Tangent;    //Pole with all wires running in line with each other
                case "Angle":
                   return Structure_Type_val.Angle;    //Pole with at least one wire that is at an angle relative to the others
                case "Deadend":
                   return Structure_Type_val.Deadend;    //Pole with wires ending at the pole
                case "Junction":
                   return Structure_Type_val.Junction;    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Structure_Type_val_to_String(Structure_Type_val pKey)
        {
           switch (pKey)
           {
                case Structure_Type_val.Auto:
                   return "Auto";    //Automatically determine the structure type from attached equipment
                case Structure_Type_val.Tangent:
                   return "Tangent";    //Pole with all wires running in line with each other
                case Structure_Type_val.Angle:
                   return "Angle";    //Pole with at least one wire that is at an angle relative to the others
                case Structure_Type_val.Deadend:
                   return "Deadend";    //Pole with wires ending at the pole
                case Structure_Type_val.Junction:
                   return "Junction";    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Class
        //   Attr Group:Standard
        //   Alt Display Name:Pole Class
        //   Description:   Pole class specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   CONCRETE_POLE_CLASS
        //   Default Value:   Unset
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Class;
        [Category("Standard")]
        [Description("Class")]
        public string Class
        {
           get { return m_Class; }
           set { m_Class = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Length (ft)
        //   Description:   Pole length in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   480
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Standard")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   CatalogName
        //   Attr Group:Standard
        //   Alt Display Name:Catalog Name
        //   Description:   Wood species of the pole
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   User Defined
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_CatalogName;
        [Category("Standard")]
        [Description("CatalogName")]
        public string CatalogName
        {
           get { return m_CatalogName; }
           set { m_CatalogName = value; }
        }



        //   Attr Name:   Pole Code
        //   Attr Group:Standard
        //   Alt Display Name:Code
        //   Description:   Pole Code
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   GO 95
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        GO 95  (GO 95)
        public enum Pole_Code_val
        {
           [Description("NESC")]
           NESC,    //NESC
           [Description("GO 95")]
           GO_95     //GO 95
        }
        private Pole_Code_val m_Pole_Code;
        [Category("Standard")]
        [Description("Pole Code")]
        public Pole_Code_val Pole_Code
        {
           get
           { return m_Pole_Code; }
           set
           { m_Pole_Code = value; }
        }

        public Pole_Code_val String_to_Pole_Code_val(string pKey)
        {
           switch (pKey)
           {
                case "NESC":
                   return Pole_Code_val.NESC;    //NESC
                case "GO 95":
                   return Pole_Code_val.GO_95;    //GO 95
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Pole_Code_val_to_String(Pole_Code_val pKey)
        {
           switch (pKey)
           {
                case Pole_Code_val.NESC:
                   return "NESC";    //NESC
                case Pole_Code_val.GO_95:
                   return "GO 95";    //GO 95
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Shape
        //   Attr Group:Standard
        //   Description:   Cross section shape
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Round
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Polygonal  (Polygonal)
        public enum Shape_val
        {
           [Description("Round")]
           Round,    //Round
           [Description("Polygonal")]
           Polygonal     //Polygonal
        }
        private Shape_val m_Shape;
        [Category("Standard")]
        [Description("Shape")]
        public Shape_val Shape
        {
           get
           { return m_Shape; }
           set
           { m_Shape = value; }
        }

        public Shape_val String_to_Shape_val(string pKey)
        {
           switch (pKey)
           {
                case "Round":
                   return Shape_val.Round;    //Round
                case "Polygonal":
                   return Shape_val.Polygonal;    //Polygonal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Shape_val_to_String(Shape_val pKey)
        {
           switch (pKey)
           {
                case Shape_val.Round:
                   return "Round";    //Round
                case Shape_val.Polygonal:
                   return "Polygonal";    //Polygonal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Faces
        //   Attr Group:Standard
        //   Alt Display Name:Polygon Faces
        //   Description:   Number of polygon faces
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   PLUSMINUS
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_Faces;
        [Category("Standard")]
        [Description("Faces")]
        public int Faces
        {
           get { return m_Faces; }
           set { m_Faces = value; }
        }



        //   Attr Name:   Mount
        //   Attr Group:Installation
        //   Alt Display Name:Mount Type
        //   Description:   Mount type
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Embedded
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Pedestal  (Pedestal)
        public enum Mount_val
        {
           [Description("Embedded")]
           Embedded,    //Embedded
           [Description("Pedestal")]
           Pedestal     //Pedestal
        }
        private Mount_val m_Mount;
        [Category("Installation")]
        [Description("Mount")]
        public Mount_val Mount
        {
           get
           { return m_Mount; }
           set
           { m_Mount = value; }
        }

        public Mount_val String_to_Mount_val(string pKey)
        {
           switch (pKey)
           {
                case "Embedded":
                   return Mount_val.Embedded;    //Embedded
                case "Pedestal":
                   return Mount_val.Pedestal;    //Pedestal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Mount_val_to_String(Mount_val pKey)
        {
           switch (pKey)
           {
                case Mount_val.Embedded:
                   return "Embedded";    //Embedded
                case Mount_val.Pedestal:
                   return "Pedestal";    //Pedestal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   PedestalRadius
        //   Attr Group:Installation
        //   Alt Display Name:Pedestal Radius (in)
        //   Description:   Radius of pedestal mount 
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   16.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalRadius;
        [Category("Installation")]
        [Description("PedestalRadius")]
        public double PedestalRadius
        {
           get { return m_PedestalRadius; }
           set { m_PedestalRadius = value; }
        }



        //   Attr Name:   BuryDepthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Setting Depth (ft)
        //   Description:   Bury depth in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   72
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BuryDepthInInches;
        [Category("Standard")]
        [Description("BuryDepthInInches")]
        public double BuryDepthInInches
        {
           get { return m_BuryDepthInInches; }
           set { m_BuryDepthInInches = value; }
        }



        //   Attr Name:   LineOfLead
        //   Attr Group:Standard
        //   Alt Display Name:Line of Lead (°)
        //   Description:   The overall line of lead of the entire pole assembly
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LineOfLead;
        [Category("Standard")]
        [Description("LineOfLead")]
        public double LineOfLead
        {
           get { return m_LineOfLead; }
           set { m_LineOfLead = value; }
        }



        //   Attr Name:   LeanDirection
        //   Attr Group:Standard
        //   Alt Display Name:Lean Direction (°)
        //   Description:   Pole lean direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanDirection;
        [Category("Standard")]
        [Description("LeanDirection")]
        public double LeanDirection
        {
           get { return m_LeanDirection; }
           set { m_LeanDirection = value; }
        }



        //   Attr Name:   LeanAmount
        //   Attr Group:Standard
        //   Alt Display Name:Lean Amount (°)
        //   Description:   Pole amount direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanAmount;
        [Category("Standard")]
        [Description("LeanAmount")]
        public double LeanAmount
        {
           get { return m_LeanAmount; }
           set { m_LeanAmount = value; }
        }



        //   Attr Name:   RadiusAtTipInInches
        //   Attr Group:Circumference
        //   Alt Display Name:Tip Circum (in)
        //   Description:   
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.3422538049298
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtTipInInches;
        [Category("Circumference")]
        [Description("RadiusAtTipInInches")]
        public double RadiusAtTipInInches
        {
           get { return m_RadiusAtTipInInches; }
           set { m_RadiusAtTipInInches = value; }
        }



        //   Attr Name:   RadiusAtBaseInInches
        //   Attr Group:Circumference
        //   Alt Display Name:Base Circum (in)
        //   Description:   Radius At Base
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   9
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtBaseInInches;
        [Category("Circumference")]
        [Description("RadiusAtBaseInInches")]
        public double RadiusAtBaseInInches
        {
           get { return m_RadiusAtBaseInInches; }
           set { m_RadiusAtBaseInInches = value; }
        }



        //   Attr Name:   SoilClass
        //   Attr Group:Overturn
        //   Alt Display Name:Soil Class
        //   Description:   The class of soil at the site of the anchor
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Class 1  (Very dense and/or cemented sands, coarse gravel and cobbles)
        //        Class 2  (Dense fine sand, very hard silts and clays (may be preloaded))
        //        Class 3  (Dense sands and gravel, hard silts and clays)
        //        Class 4  (Medium dense sandy gravel, very stiff to hard silts and clays)
        //        Class 5  (Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays)
        //        Class 6  (Loose to medium dense fine to coarse sand, firm to stiff clays and silts)
        //        Class 7  (Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill)
        //        Class 8  (Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays)
        //        Unsset  (Unset)
        public enum SoilClass_val
        {
           [Description("Class 0")]
           Class_0,    //Sound hard rock, bedrock, unweathered
           [Description("Class 1")]
           Class_1,    //Very dense and/or cemented sands, coarse gravel and cobbles
           [Description("Class 2")]
           Class_2,    //Dense fine sand, very hard silts and clays (may be preloaded)
           [Description("Class 3")]
           Class_3,    //Dense sands and gravel, hard silts and clays
           [Description("Class 4")]
           Class_4,    //Medium dense sandy gravel, very stiff to hard silts and clays
           [Description("Class 5")]
           Class_5,    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
           [Description("Class 6")]
           Class_6,    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
           [Description("Class 7")]
           Class_7,    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
           [Description("Class 8")]
           Class_8,    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
           [Description("Unsset")]
           Unsset     //Unset
        }
        private SoilClass_val m_SoilClass;
        [Category("Overturn")]
        [Description("SoilClass")]
        public SoilClass_val SoilClass
        {
           get
           { return m_SoilClass; }
           set
           { m_SoilClass = value; }
        }

        public SoilClass_val String_to_SoilClass_val(string pKey)
        {
           switch (pKey)
           {
                case "Class 0":
                   return SoilClass_val.Class_0;    //Sound hard rock, bedrock, unweathered
                case "Class 1":
                   return SoilClass_val.Class_1;    //Very dense and/or cemented sands, coarse gravel and cobbles
                case "Class 2":
                   return SoilClass_val.Class_2;    //Dense fine sand, very hard silts and clays (may be preloaded)
                case "Class 3":
                   return SoilClass_val.Class_3;    //Dense sands and gravel, hard silts and clays
                case "Class 4":
                   return SoilClass_val.Class_4;    //Medium dense sandy gravel, very stiff to hard silts and clays
                case "Class 5":
                   return SoilClass_val.Class_5;    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case "Class 6":
                   return SoilClass_val.Class_6;    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case "Class 7":
                   return SoilClass_val.Class_7;    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case "Class 8":
                   return SoilClass_val.Class_8;    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case "Unsset":
                   return SoilClass_val.Unsset;    //Unset
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string SoilClass_val_to_String(SoilClass_val pKey)
        {
           switch (pKey)
           {
                case SoilClass_val.Class_0:
                   return "Class 0";    //Sound hard rock, bedrock, unweathered
                case SoilClass_val.Class_1:
                   return "Class 1";    //Very dense and/or cemented sands, coarse gravel and cobbles
                case SoilClass_val.Class_2:
                   return "Class 2";    //Dense fine sand, very hard silts and clays (may be preloaded)
                case SoilClass_val.Class_3:
                   return "Class 3";    //Dense sands and gravel, hard silts and clays
                case SoilClass_val.Class_4:
                   return "Class 4";    //Medium dense sandy gravel, very stiff to hard silts and clays
                case SoilClass_val.Class_5:
                   return "Class 5";    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case SoilClass_val.Class_6:
                   return "Class 6";    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case SoilClass_val.Class_7:
                   return "Class 7";    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case SoilClass_val.Class_8:
                   return "Class 8";    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case SoilClass_val.Unsset:
                   return "Unsset";    //Unset
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   OverturnMoment
        //   Attr Group:Overturn
        //   Alt Display Name:Overturn Moment (ft-lbs)
        //   Description:   Overturn Moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.#
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OverturnMoment;
        [Category("Overturn")]
        [Description("OverturnMoment")]
        public double OverturnMoment
        {
           get { return m_OverturnMoment; }
           set { m_OverturnMoment = value; }
        }



        //   Attr Name:   Modulus of Elasticity
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   Modulus of elasticty for the material
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   4350000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Elasticity;
        [Category("Phys. Consts")]
        [Description("Modulus of Elasticity")]
        public double Modulus_of_Elasticity
        {
           get { return m_Modulus_of_Elasticity; }
           set { m_Modulus_of_Elasticity = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.2
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Phys. Consts")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Phys. Consts")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000027
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Phys. Consts")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   Density
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Density (lb/ft^3)
        //   Description:   Density for the given species in lbs per cubic inch
        //   Displayed Units:   store as POUNDS PER CUBIC INCH display as POUNDS PER CUBIC FOOT or KILOGRAMS PER CUBIC METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0347222222222222
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Density;
        [Category("Phys. Consts")]
        [Description("Density")]
        public double Density
        {
           get { return m_Density; }
           set { m_Density = value; }
        }



        //   Attr Name:   Characteristic Shear Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Shear Str (psi)
        //   Description:   Characteristic Shear Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   450
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Shear_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Shear Strength")]
        public double Characteristic_Shear_Strength
        {
           get { return m_Characteristic_Shear_Strength; }
           set { m_Characteristic_Shear_Strength = value; }
        }



        //   Attr Name:   Characteristic Compression Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Compression Str (psi)
        //   Description:   Characteristic Compression Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   3500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Compression_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Compression Strength")]
        public double Characteristic_Compression_Strength
        {
           get { return m_Characteristic_Compression_Strength; }
           set { m_Characteristic_Compression_Strength = value; }
        }



        //   Attr Name:   Effective Length
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Effective Length (ft)
        //   Description:   Effective Length
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0#
        //   Attribute Type:   FLOAT
        //   Default Value:   -1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Effective_Length;
        [Category("AS/NZS 7000")]
        [Description("Effective Length")]
        public double Effective_Length
        {
           get { return m_Effective_Length; }
           set { m_Effective_Length = value; }
        }



        //   Attr Name:   Material Constant
        //   Attr Group:AS/NZS 7000
        //   Description:   Material Constant
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   1.24
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Material_Constant;
        [Category("AS/NZS 7000")]
        [Description("Material Constant")]
        public double Material_Constant
        {
           get { return m_Material_Constant; }
           set { m_Material_Constant = value; }
        }



        //   Attr Name:   Offset
        //   Attr Group:Multi Pole
        //   Alt Display Name:Offset (ft)
        //   Description:   Pole offset in feet
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_Offset;
        [Category("Multi Pole")]
        [Description("Offset")]
        public double Offset
        {
           get { return m_Offset; }
           set { m_Offset = value; }
        }



        //   Attr Name:   Aux Data 1
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_1;
        [Category("User Data")]
        [Description("Aux Data 1")]
        public string Aux_Data_1
        {
           get { return m_Aux_Data_1; }
           set { m_Aux_Data_1 = value; }
        }



        //   Attr Name:   Aux Data 2
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_2;
        [Category("User Data")]
        [Description("Aux Data 2")]
        public string Aux_Data_2
        {
           get { return m_Aux_Data_2; }
           set { m_Aux_Data_2 = value; }
        }



        //   Attr Name:   Aux Data 3
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_3;
        [Category("User Data")]
        [Description("Aux Data 3")]
        public string Aux_Data_3
        {
           get { return m_Aux_Data_3; }
           set { m_Aux_Data_3 = value; }
        }



        //   Attr Name:   Aux Data 4
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_4;
        [Category("User Data")]
        [Description("Aux Data 4")]
        public string Aux_Data_4
        {
           get { return m_Aux_Data_4; }
           set { m_Aux_Data_4 = value; }
        }



        //   Attr Name:   Aux Data 5
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_5;
        [Category("User Data")]
        [Description("Aux Data 5")]
        public string Aux_Data_5
        {
           get { return m_Aux_Data_5; }
           set { m_Aux_Data_5 = value; }
        }



        //   Attr Name:   Aux Data 6
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_6;
        [Category("User Data")]
        [Description("Aux Data 6")]
        public string Aux_Data_6
        {
           get { return m_Aux_Data_6; }
           set { m_Aux_Data_6 = value; }
        }



        //   Attr Name:   Aux Data 7
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_7;
        [Category("User Data")]
        [Description("Aux Data 7")]
        public string Aux_Data_7
        {
           get { return m_Aux_Data_7; }
           set { m_Aux_Data_7 = value; }
        }



        //   Attr Name:   Aux Data 8
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_8;
        [Category("User Data")]
        [Description("Aux Data 8")]
        public string Aux_Data_8
        {
           get { return m_Aux_Data_8; }
           set { m_Aux_Data_8 = value; }
        }



        //   Attr Name:   ThicknessTable
        //   Attr Group:Standard
        //   Alt Display Name:Thickness
        //   Description:   The material thickness
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   THICKNESS_TABLE
        //   Default Value:   Thick;0,0.25;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_ThicknessTable = new ValTable();
        [Category("Standard")]
        [Description("ThicknessTable")]
        public ValTable ThicknessTable
        {
           get { return m_ThicknessTable; }
           set { m_ThicknessTable = value; }
        }



        //   Attr Name:   PedestalMomentCapacity
        //   Attr Group:Installation
        //   Alt Display Name:Ped Mom Cap (ft-lb)
        //   Description:   The pedestal moment capacity
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   90000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalMomentCapacity;
        [Category("Installation")]
        [Description("PedestalMomentCapacity")]
        public double PedestalMomentCapacity
        {
           get { return m_PedestalMomentCapacity; }
           set { m_PedestalMomentCapacity = value; }
        }



        //   Attr Name:   PedestalBucklingCapacity
        //   Attr Group:Installation
        //   Alt Display Name:Ped Buck Cap (lbs)
        //   Description:   The pedestal buckling capacity
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   9000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalBucklingCapacity;
        [Category("Installation")]
        [Description("PedestalBucklingCapacity")]
        public double PedestalBucklingCapacity
        {
           get { return m_PedestalBucklingCapacity; }
           set { m_PedestalBucklingCapacity = value; }
        }



        //   Attr Name:   DistToGrade
        //   Attr Group:Installation
        //   Alt Display Name:Dist To Grade (ft)
        //   Description:   Distance to grade of pedastal mount 
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DistToGrade;
        [Category("Installation")]
        [Description("DistToGrade")]
        public double DistToGrade
        {
           get { return m_DistToGrade; }
           set { m_DistToGrade = value; }
        }



        //   Attr Name:   MomentCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Moment Cap (ft-lb)
        //   Description:   The moment capacity table
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   MOMENT_TABLE
        //   Default Value:   Moment;0,50000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_MomentCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("MomentCapacityTable")]
        public ValTable MomentCapacityTable
        {
           get { return m_MomentCapacityTable; }
           set { m_MomentCapacityTable = value; }
        }



        //   Attr Name:   BucklingCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Buckling Cap (lbs)
        //   Description:   The buckling capacity table
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BUCKLING_TABLE
        //   Default Value:   Buckling;0,5000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_BucklingCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("BucklingCapacityTable")]
        public ValTable BucklingCapacityTable
        {
           get { return m_BucklingCapacityTable; }
           set { m_BucklingCapacityTable = value; }
        }



        //   Attr Name:   Torque
        //   Attr Group:Standard
        //   Alt Display Name:Ultimate Torque (ft-lbs)
        //   Description:   Maximum allowable torque
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   600
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Torque;
        [Category("Standard")]
        [Description("Torque")]
        public double Torque
        {
           get { return m_Torque; }
           set { m_Torque = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: CompositePole
   // Mirrors: PPLCompositePole : PPLElement
   //--------------------------------------------------------------------------------------------
   public class CompositePole : ElementBase
   {

      public static string gXMLkey = "CompositePole";
      public override string XMLkey() { return gXMLkey; }

      public CompositePole(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Pole_Number = "Unset";
               m_Owner = "Pole";
               m_Structure_Type = Structure_Type_val.Auto;
               m_Class = "4";
               m_LengthInInches = 480;
               m_CatalogName = "User Defined";
               m_Pole_Code = Pole_Code_val.GO_95;
               m_Shape = Shape_val.Polygonal;
               m_Faces = 4;
               m_Mount = Mount_val.Pedestal;
               m_PedestalRadius = 16;
               m_BuryDepthInInches = 72;
               m_LineOfLead = 0;
               m_LeanDirection = 0;
               m_LeanAmount = 0;
               m_RadiusAtTipInInches = 3.3422538049298;
               m_RadiusAtBaseInInches = 9;
               m_OverturnMoment = 0;
               m_Modulus_of_Elasticity = 29000000;
               m_PoissonsRatio = 0.4;
               m_WindDragCoef = 0;
               m_ThermalCoefficient = 2.7E-06;
               m_Density = 0.0347222222222222;
               m_Characteristic_Shear_Strength = 450;
               m_Characteristic_Compression_Strength = 3500;
               m_Effective_Length = -1;
               m_Material_Constant = 1.24;
               m_Offset = 0;
               m_Aux_Data_1 = "Unset";
               m_Aux_Data_2 = "Unset";
               m_Aux_Data_3 = "Unset";
               m_Aux_Data_4 = "Unset";
               m_Aux_Data_5 = "Unset";
               m_Aux_Data_6 = "Unset";
               m_Aux_Data_7 = "Unset";
               m_Aux_Data_8 = "Unset";
               m_ThicknessTable = new ValTable("Thick;0,0.25;");
               m_PedestalMomentCapacity = 90000;
               m_PedestalBucklingCapacity = 9000;
               m_DistToGrade = 0;
               m_MomentCapacityTable = new ValTable("Moment;0,50000;");
               m_BucklingCapacityTable = new ValTable("Buckling;0,5000;");
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Crossarm) return true;
         if(pChildCandidate is PowerEquipment) return true;
         if(pChildCandidate is Streetlight) return true;
         if(pChildCandidate is Insulator) return true;
         if(pChildCandidate is NodeJunction) return true;
         if(pChildCandidate is CapacityAdjustment) return true;
         if(pChildCandidate is Riser) return true;
         if(pChildCandidate is GenericEquipment) return true;
         if(pChildCandidate is Anchor) return true;
         if(pChildCandidate is LoadCase) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Pole Number
        //   Attr Group:Standard
        //   Description:   Pole identification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Pole_Number;
        [Category("Standard")]
        [Description("Pole Number")]
        public string Pole_Number
        {
           get { return m_Pole_Number; }
           set { m_Pole_Number = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Structure Type
        //   Attr Group:Standard
        //   Description:   Pole structure type specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Auto
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Tangent  (Pole with all wires running in line with each other)
        //        Angle  (Pole with at least one wire that is at an angle relative to the others)
        //        Deadend  (Pole with wires ending at the pole)
        //        Junction  (Pole with wires crossing at or near the pole)
        public enum Structure_Type_val
        {
           [Description("Auto")]
           Auto,    //Automatically determine the structure type from attached equipment
           [Description("Tangent")]
           Tangent,    //Pole with all wires running in line with each other
           [Description("Angle")]
           Angle,    //Pole with at least one wire that is at an angle relative to the others
           [Description("Deadend")]
           Deadend,    //Pole with wires ending at the pole
           [Description("Junction")]
           Junction     //Pole with wires crossing at or near the pole
        }
        private Structure_Type_val m_Structure_Type;
        [Category("Standard")]
        [Description("Structure Type")]
        public Structure_Type_val Structure_Type
        {
           get
           { return m_Structure_Type; }
           set
           { m_Structure_Type = value; }
        }

        public Structure_Type_val String_to_Structure_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Auto":
                   return Structure_Type_val.Auto;    //Automatically determine the structure type from attached equipment
                case "Tangent":
                   return Structure_Type_val.Tangent;    //Pole with all wires running in line with each other
                case "Angle":
                   return Structure_Type_val.Angle;    //Pole with at least one wire that is at an angle relative to the others
                case "Deadend":
                   return Structure_Type_val.Deadend;    //Pole with wires ending at the pole
                case "Junction":
                   return Structure_Type_val.Junction;    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Structure_Type_val_to_String(Structure_Type_val pKey)
        {
           switch (pKey)
           {
                case Structure_Type_val.Auto:
                   return "Auto";    //Automatically determine the structure type from attached equipment
                case Structure_Type_val.Tangent:
                   return "Tangent";    //Pole with all wires running in line with each other
                case Structure_Type_val.Angle:
                   return "Angle";    //Pole with at least one wire that is at an angle relative to the others
                case Structure_Type_val.Deadend:
                   return "Deadend";    //Pole with wires ending at the pole
                case Structure_Type_val.Junction:
                   return "Junction";    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Class
        //   Attr Group:Standard
        //   Alt Display Name:Pole Class
        //   Description:   Pole class specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   4
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Class;
        [Category("Standard")]
        [Description("Class")]
        public string Class
        {
           get { return m_Class; }
           set { m_Class = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Length (ft)
        //   Description:   Pole length in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   480
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Standard")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   CatalogName
        //   Attr Group:Standard
        //   Alt Display Name:Catalog Name
        //   Description:   Wood species of the pole
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   User Defined
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_CatalogName;
        [Category("Standard")]
        [Description("CatalogName")]
        public string CatalogName
        {
           get { return m_CatalogName; }
           set { m_CatalogName = value; }
        }



        //   Attr Name:   Pole Code
        //   Attr Group:Standard
        //   Alt Display Name:Code
        //   Description:   Pole Code
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   GO 95
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        GO 95  (GO 95)
        public enum Pole_Code_val
        {
           [Description("NESC")]
           NESC,    //NESC
           [Description("GO 95")]
           GO_95     //GO 95
        }
        private Pole_Code_val m_Pole_Code;
        [Category("Standard")]
        [Description("Pole Code")]
        public Pole_Code_val Pole_Code
        {
           get
           { return m_Pole_Code; }
           set
           { m_Pole_Code = value; }
        }

        public Pole_Code_val String_to_Pole_Code_val(string pKey)
        {
           switch (pKey)
           {
                case "NESC":
                   return Pole_Code_val.NESC;    //NESC
                case "GO 95":
                   return Pole_Code_val.GO_95;    //GO 95
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Pole_Code_val_to_String(Pole_Code_val pKey)
        {
           switch (pKey)
           {
                case Pole_Code_val.NESC:
                   return "NESC";    //NESC
                case Pole_Code_val.GO_95:
                   return "GO 95";    //GO 95
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Shape
        //   Attr Group:Standard
        //   Description:   Cross section shape
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Polygonal
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Polygonal  (Polygonal)
        public enum Shape_val
        {
           [Description("Round")]
           Round,    //Round
           [Description("Polygonal")]
           Polygonal     //Polygonal
        }
        private Shape_val m_Shape;
        [Category("Standard")]
        [Description("Shape")]
        public Shape_val Shape
        {
           get
           { return m_Shape; }
           set
           { m_Shape = value; }
        }

        public Shape_val String_to_Shape_val(string pKey)
        {
           switch (pKey)
           {
                case "Round":
                   return Shape_val.Round;    //Round
                case "Polygonal":
                   return Shape_val.Polygonal;    //Polygonal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Shape_val_to_String(Shape_val pKey)
        {
           switch (pKey)
           {
                case Shape_val.Round:
                   return "Round";    //Round
                case Shape_val.Polygonal:
                   return "Polygonal";    //Polygonal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Faces
        //   Attr Group:Standard
        //   Alt Display Name:Polygon Faces
        //   Description:   Number of polygon faces
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   PLUSMINUS
        //   Default Value:   4
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_Faces;
        [Category("Standard")]
        [Description("Faces")]
        public int Faces
        {
           get { return m_Faces; }
           set { m_Faces = value; }
        }



        //   Attr Name:   Mount
        //   Attr Group:Installation
        //   Alt Display Name:Mount Type
        //   Description:   Mount type
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Pedestal
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Pedestal  (Pedestal)
        public enum Mount_val
        {
           [Description("Embedded")]
           Embedded,    //Embedded
           [Description("Pedestal")]
           Pedestal     //Pedestal
        }
        private Mount_val m_Mount;
        [Category("Installation")]
        [Description("Mount")]
        public Mount_val Mount
        {
           get
           { return m_Mount; }
           set
           { m_Mount = value; }
        }

        public Mount_val String_to_Mount_val(string pKey)
        {
           switch (pKey)
           {
                case "Embedded":
                   return Mount_val.Embedded;    //Embedded
                case "Pedestal":
                   return Mount_val.Pedestal;    //Pedestal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Mount_val_to_String(Mount_val pKey)
        {
           switch (pKey)
           {
                case Mount_val.Embedded:
                   return "Embedded";    //Embedded
                case Mount_val.Pedestal:
                   return "Pedestal";    //Pedestal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   PedestalRadius
        //   Attr Group:Installation
        //   Alt Display Name:Pedestal Radius (in)
        //   Description:   Radius of pedestal mount 
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   16.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalRadius;
        [Category("Installation")]
        [Description("PedestalRadius")]
        public double PedestalRadius
        {
           get { return m_PedestalRadius; }
           set { m_PedestalRadius = value; }
        }



        //   Attr Name:   BuryDepthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Setting Depth (ft)
        //   Description:   Bury depth in inches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   72
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BuryDepthInInches;
        [Category("Standard")]
        [Description("BuryDepthInInches")]
        public double BuryDepthInInches
        {
           get { return m_BuryDepthInInches; }
           set { m_BuryDepthInInches = value; }
        }



        //   Attr Name:   LineOfLead
        //   Attr Group:Standard
        //   Alt Display Name:Line of Lead (°)
        //   Description:   The overall line of lead of the entire pole assembly
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LineOfLead;
        [Category("Standard")]
        [Description("LineOfLead")]
        public double LineOfLead
        {
           get { return m_LineOfLead; }
           set { m_LineOfLead = value; }
        }



        //   Attr Name:   LeanDirection
        //   Attr Group:Standard
        //   Alt Display Name:Lean Direction (°)
        //   Description:   Pole lean direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanDirection;
        [Category("Standard")]
        [Description("LeanDirection")]
        public double LeanDirection
        {
           get { return m_LeanDirection; }
           set { m_LeanDirection = value; }
        }



        //   Attr Name:   LeanAmount
        //   Attr Group:Standard
        //   Alt Display Name:Lean Amount (°)
        //   Description:   Pole amount direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanAmount;
        [Category("Standard")]
        [Description("LeanAmount")]
        public double LeanAmount
        {
           get { return m_LeanAmount; }
           set { m_LeanAmount = value; }
        }



        //   Attr Name:   RadiusAtTipInInches
        //   Attr Group:Circumference
        //   Alt Display Name:Tip Circum (in)
        //   Description:   
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.3422538049298
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtTipInInches;
        [Category("Circumference")]
        [Description("RadiusAtTipInInches")]
        public double RadiusAtTipInInches
        {
           get { return m_RadiusAtTipInInches; }
           set { m_RadiusAtTipInInches = value; }
        }



        //   Attr Name:   RadiusAtBaseInInches
        //   Attr Group:Circumference
        //   Alt Display Name:Base Circum (in)
        //   Description:   Radius At Base
        //   Displayed Units:   store as RADIUS IN INCHES display as CIRCUMFERENCE IN INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   9
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RadiusAtBaseInInches;
        [Category("Circumference")]
        [Description("RadiusAtBaseInInches")]
        public double RadiusAtBaseInInches
        {
           get { return m_RadiusAtBaseInInches; }
           set { m_RadiusAtBaseInInches = value; }
        }



        //   Attr Name:   SoilClass
        //   Attr Group:Overturn
        //   Alt Display Name:Soil Class
        //   Description:   The class of soil at the site of the anchor
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Class 1  (Very dense and/or cemented sands, coarse gravel and cobbles)
        //        Class 2  (Dense fine sand, very hard silts and clays (may be preloaded))
        //        Class 3  (Dense sands and gravel, hard silts and clays)
        //        Class 4  (Medium dense sandy gravel, very stiff to hard silts and clays)
        //        Class 5  (Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays)
        //        Class 6  (Loose to medium dense fine to coarse sand, firm to stiff clays and silts)
        //        Class 7  (Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill)
        //        Class 8  (Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays)
        //        Unsset  (Unset)
        public enum SoilClass_val
        {
           [Description("Class 0")]
           Class_0,    //Sound hard rock, bedrock, unweathered
           [Description("Class 1")]
           Class_1,    //Very dense and/or cemented sands, coarse gravel and cobbles
           [Description("Class 2")]
           Class_2,    //Dense fine sand, very hard silts and clays (may be preloaded)
           [Description("Class 3")]
           Class_3,    //Dense sands and gravel, hard silts and clays
           [Description("Class 4")]
           Class_4,    //Medium dense sandy gravel, very stiff to hard silts and clays
           [Description("Class 5")]
           Class_5,    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
           [Description("Class 6")]
           Class_6,    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
           [Description("Class 7")]
           Class_7,    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
           [Description("Class 8")]
           Class_8,    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
           [Description("Unsset")]
           Unsset     //Unset
        }
        private SoilClass_val m_SoilClass;
        [Category("Overturn")]
        [Description("SoilClass")]
        public SoilClass_val SoilClass
        {
           get
           { return m_SoilClass; }
           set
           { m_SoilClass = value; }
        }

        public SoilClass_val String_to_SoilClass_val(string pKey)
        {
           switch (pKey)
           {
                case "Class 0":
                   return SoilClass_val.Class_0;    //Sound hard rock, bedrock, unweathered
                case "Class 1":
                   return SoilClass_val.Class_1;    //Very dense and/or cemented sands, coarse gravel and cobbles
                case "Class 2":
                   return SoilClass_val.Class_2;    //Dense fine sand, very hard silts and clays (may be preloaded)
                case "Class 3":
                   return SoilClass_val.Class_3;    //Dense sands and gravel, hard silts and clays
                case "Class 4":
                   return SoilClass_val.Class_4;    //Medium dense sandy gravel, very stiff to hard silts and clays
                case "Class 5":
                   return SoilClass_val.Class_5;    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case "Class 6":
                   return SoilClass_val.Class_6;    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case "Class 7":
                   return SoilClass_val.Class_7;    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case "Class 8":
                   return SoilClass_val.Class_8;    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case "Unsset":
                   return SoilClass_val.Unsset;    //Unset
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string SoilClass_val_to_String(SoilClass_val pKey)
        {
           switch (pKey)
           {
                case SoilClass_val.Class_0:
                   return "Class 0";    //Sound hard rock, bedrock, unweathered
                case SoilClass_val.Class_1:
                   return "Class 1";    //Very dense and/or cemented sands, coarse gravel and cobbles
                case SoilClass_val.Class_2:
                   return "Class 2";    //Dense fine sand, very hard silts and clays (may be preloaded)
                case SoilClass_val.Class_3:
                   return "Class 3";    //Dense sands and gravel, hard silts and clays
                case SoilClass_val.Class_4:
                   return "Class 4";    //Medium dense sandy gravel, very stiff to hard silts and clays
                case SoilClass_val.Class_5:
                   return "Class 5";    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case SoilClass_val.Class_6:
                   return "Class 6";    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case SoilClass_val.Class_7:
                   return "Class 7";    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case SoilClass_val.Class_8:
                   return "Class 8";    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case SoilClass_val.Unsset:
                   return "Unsset";    //Unset
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   OverturnMoment
        //   Attr Group:Overturn
        //   Alt Display Name:Overturn Moment (ft-lbs)
        //   Description:   Overturn Moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.#
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OverturnMoment;
        [Category("Overturn")]
        [Description("OverturnMoment")]
        public double OverturnMoment
        {
           get { return m_OverturnMoment; }
           set { m_OverturnMoment = value; }
        }



        //   Attr Name:   Modulus of Elasticity
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   Modulus of elasticty for the material
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   29000000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Elasticity;
        [Category("Phys. Consts")]
        [Description("Modulus of Elasticity")]
        public double Modulus_of_Elasticity
        {
           get { return m_Modulus_of_Elasticity; }
           set { m_Modulus_of_Elasticity = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.4
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Phys. Consts")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Phys. Consts")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000027
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Phys. Consts")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   Density
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Density (lb/ft^3)
        //   Description:   Density for the given species in lbs per cubic inch
        //   Displayed Units:   store as POUNDS PER CUBIC INCH display as POUNDS PER CUBIC FOOT or KILOGRAMS PER CUBIC METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0347222222222222
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Density;
        [Category("Phys. Consts")]
        [Description("Density")]
        public double Density
        {
           get { return m_Density; }
           set { m_Density = value; }
        }



        //   Attr Name:   Characteristic Shear Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Shear Str (psi)
        //   Description:   Characteristic Shear Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   450
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Shear_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Shear Strength")]
        public double Characteristic_Shear_Strength
        {
           get { return m_Characteristic_Shear_Strength; }
           set { m_Characteristic_Shear_Strength = value; }
        }



        //   Attr Name:   Characteristic Compression Strength
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Char Compression Str (psi)
        //   Description:   Characteristic Compression Strength
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   3500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Characteristic_Compression_Strength;
        [Category("AS/NZS 7000")]
        [Description("Characteristic Compression Strength")]
        public double Characteristic_Compression_Strength
        {
           get { return m_Characteristic_Compression_Strength; }
           set { m_Characteristic_Compression_Strength = value; }
        }



        //   Attr Name:   Effective Length
        //   Attr Group:AS/NZS 7000
        //   Alt Display Name:Effective Length (ft)
        //   Description:   Effective Length
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0#
        //   Attribute Type:   FLOAT
        //   Default Value:   -1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Effective_Length;
        [Category("AS/NZS 7000")]
        [Description("Effective Length")]
        public double Effective_Length
        {
           get { return m_Effective_Length; }
           set { m_Effective_Length = value; }
        }



        //   Attr Name:   Material Constant
        //   Attr Group:AS/NZS 7000
        //   Description:   Material Constant
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   1.24
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Material_Constant;
        [Category("AS/NZS 7000")]
        [Description("Material Constant")]
        public double Material_Constant
        {
           get { return m_Material_Constant; }
           set { m_Material_Constant = value; }
        }



        //   Attr Name:   Offset
        //   Attr Group:Multi Pole
        //   Alt Display Name:Offset (ft)
        //   Description:   Pole offset in feet
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_Offset;
        [Category("Multi Pole")]
        [Description("Offset")]
        public double Offset
        {
           get { return m_Offset; }
           set { m_Offset = value; }
        }



        //   Attr Name:   Aux Data 1
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_1;
        [Category("User Data")]
        [Description("Aux Data 1")]
        public string Aux_Data_1
        {
           get { return m_Aux_Data_1; }
           set { m_Aux_Data_1 = value; }
        }



        //   Attr Name:   Aux Data 2
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_2;
        [Category("User Data")]
        [Description("Aux Data 2")]
        public string Aux_Data_2
        {
           get { return m_Aux_Data_2; }
           set { m_Aux_Data_2 = value; }
        }



        //   Attr Name:   Aux Data 3
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_3;
        [Category("User Data")]
        [Description("Aux Data 3")]
        public string Aux_Data_3
        {
           get { return m_Aux_Data_3; }
           set { m_Aux_Data_3 = value; }
        }



        //   Attr Name:   Aux Data 4
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_4;
        [Category("User Data")]
        [Description("Aux Data 4")]
        public string Aux_Data_4
        {
           get { return m_Aux_Data_4; }
           set { m_Aux_Data_4 = value; }
        }



        //   Attr Name:   Aux Data 5
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_5;
        [Category("User Data")]
        [Description("Aux Data 5")]
        public string Aux_Data_5
        {
           get { return m_Aux_Data_5; }
           set { m_Aux_Data_5 = value; }
        }



        //   Attr Name:   Aux Data 6
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_6;
        [Category("User Data")]
        [Description("Aux Data 6")]
        public string Aux_Data_6
        {
           get { return m_Aux_Data_6; }
           set { m_Aux_Data_6 = value; }
        }



        //   Attr Name:   Aux Data 7
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_7;
        [Category("User Data")]
        [Description("Aux Data 7")]
        public string Aux_Data_7
        {
           get { return m_Aux_Data_7; }
           set { m_Aux_Data_7 = value; }
        }



        //   Attr Name:   Aux Data 8
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_8;
        [Category("User Data")]
        [Description("Aux Data 8")]
        public string Aux_Data_8
        {
           get { return m_Aux_Data_8; }
           set { m_Aux_Data_8 = value; }
        }



        //   Attr Name:   ThicknessTable
        //   Attr Group:Standard
        //   Alt Display Name:Thickness
        //   Description:   The material thickness
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   THICKNESS_TABLE
        //   Default Value:   Thick;0,0.25;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_ThicknessTable = new ValTable();
        [Category("Standard")]
        [Description("ThicknessTable")]
        public ValTable ThicknessTable
        {
           get { return m_ThicknessTable; }
           set { m_ThicknessTable = value; }
        }



        //   Attr Name:   PedestalMomentCapacity
        //   Attr Group:Installation
        //   Alt Display Name:Ped Mom Cap (ft-lb)
        //   Description:   The pedestal moment capacity
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   90000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalMomentCapacity;
        [Category("Installation")]
        [Description("PedestalMomentCapacity")]
        public double PedestalMomentCapacity
        {
           get { return m_PedestalMomentCapacity; }
           set { m_PedestalMomentCapacity = value; }
        }



        //   Attr Name:   PedestalBucklingCapacity
        //   Attr Group:Installation
        //   Alt Display Name:Ped Buck Cap (lbs)
        //   Description:   The pedestal buckling capacity
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   9000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PedestalBucklingCapacity;
        [Category("Installation")]
        [Description("PedestalBucklingCapacity")]
        public double PedestalBucklingCapacity
        {
           get { return m_PedestalBucklingCapacity; }
           set { m_PedestalBucklingCapacity = value; }
        }



        //   Attr Name:   DistToGrade
        //   Attr Group:Installation
        //   Alt Display Name:Dist To Grade (ft)
        //   Description:   Distance to grade of pedastal mount 
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DistToGrade;
        [Category("Installation")]
        [Description("DistToGrade")]
        public double DistToGrade
        {
           get { return m_DistToGrade; }
           set { m_DistToGrade = value; }
        }



        //   Attr Name:   MomentCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Moment Cap (ft-lb)
        //   Description:   The moment capacity table
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   MOMENT_TABLE
        //   Default Value:   Moment;0,50000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_MomentCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("MomentCapacityTable")]
        public ValTable MomentCapacityTable
        {
           get { return m_MomentCapacityTable; }
           set { m_MomentCapacityTable = value; }
        }



        //   Attr Name:   BucklingCapacityTable
        //   Attr Group:Capacity
        //   Alt Display Name:Buckling Cap (lbs)
        //   Description:   The buckling capacity table
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BUCKLING_TABLE
        //   Default Value:   Buckling;0,5000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_BucklingCapacityTable = new ValTable();
        [Category("Capacity")]
        [Description("BucklingCapacityTable")]
        public ValTable BucklingCapacityTable
        {
           get { return m_BucklingCapacityTable; }
           set { m_BucklingCapacityTable = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: SegmentedPole
   // Mirrors: PPLSegmentedPole : PPLElement
   //--------------------------------------------------------------------------------------------
   public class SegmentedPole : ElementBase
   {

      public static string gXMLkey = "SegmentedPole";
      public override string XMLkey() { return gXMLkey; }

      public SegmentedPole(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Pole_Number = "Unset";
               m_Owner = "Pole";
               m_Structure_Type = Structure_Type_val.Auto;
               m_OverlapCombineCap = false;
               m_LineOfLead = 0;
               m_LeanDirection = 0;
               m_LeanAmount = 0;
               m_OverturnMoment = 0;
               m_Aux_Data_1 = "Unset";
               m_Aux_Data_2 = "Unset";
               m_Aux_Data_3 = "Unset";
               m_Aux_Data_4 = "Unset";
               m_Aux_Data_5 = "Unset";
               m_Aux_Data_6 = "Unset";
               m_Aux_Data_7 = "Unset";
               m_Aux_Data_8 = "Unset";
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Crossarm) return true;
         if(pChildCandidate is PowerEquipment) return true;
         if(pChildCandidate is Streetlight) return true;
         if(pChildCandidate is Insulator) return true;
         if(pChildCandidate is NodeJunction) return true;
         if(pChildCandidate is Riser) return true;
         if(pChildCandidate is GenericEquipment) return true;
         if(pChildCandidate is Anchor) return true;
         if(pChildCandidate is LoadCase) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is PoleSegment) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Pole Number
        //   Attr Group:Standard
        //   Description:   Pole identification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Pole_Number;
        [Category("Standard")]
        [Description("Pole Number")]
        public string Pole_Number
        {
           get { return m_Pole_Number; }
           set { m_Pole_Number = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Structure Type
        //   Attr Group:Standard
        //   Description:   Pole structure type specification
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Auto
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Tangent  (Pole with all wires running in line with each other)
        //        Angle  (Pole with at least one wire that is at an angle relative to the others)
        //        Deadend  (Pole with wires ending at the pole)
        //        Junction  (Pole with wires crossing at or near the pole)
        public enum Structure_Type_val
        {
           [Description("Auto")]
           Auto,    //Automatically determine the structure type from attached equipment
           [Description("Tangent")]
           Tangent,    //Pole with all wires running in line with each other
           [Description("Angle")]
           Angle,    //Pole with at least one wire that is at an angle relative to the others
           [Description("Deadend")]
           Deadend,    //Pole with wires ending at the pole
           [Description("Junction")]
           Junction     //Pole with wires crossing at or near the pole
        }
        private Structure_Type_val m_Structure_Type;
        [Category("Standard")]
        [Description("Structure Type")]
        public Structure_Type_val Structure_Type
        {
           get
           { return m_Structure_Type; }
           set
           { m_Structure_Type = value; }
        }

        public Structure_Type_val String_to_Structure_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Auto":
                   return Structure_Type_val.Auto;    //Automatically determine the structure type from attached equipment
                case "Tangent":
                   return Structure_Type_val.Tangent;    //Pole with all wires running in line with each other
                case "Angle":
                   return Structure_Type_val.Angle;    //Pole with at least one wire that is at an angle relative to the others
                case "Deadend":
                   return Structure_Type_val.Deadend;    //Pole with wires ending at the pole
                case "Junction":
                   return Structure_Type_val.Junction;    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Structure_Type_val_to_String(Structure_Type_val pKey)
        {
           switch (pKey)
           {
                case Structure_Type_val.Auto:
                   return "Auto";    //Automatically determine the structure type from attached equipment
                case Structure_Type_val.Tangent:
                   return "Tangent";    //Pole with all wires running in line with each other
                case Structure_Type_val.Angle:
                   return "Angle";    //Pole with at least one wire that is at an angle relative to the others
                case Structure_Type_val.Deadend:
                   return "Deadend";    //Pole with wires ending at the pole
                case Structure_Type_val.Junction:
                   return "Junction";    //Pole with wires crossing at or near the pole
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   OverlapCombineCap
        //   Attr Group:Standard
        //   Alt Display Name:Overlap Combine Cap
        //   Description:   OverlapCombineCap
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverlapCombineCap;
        [Category("Standard")]
        [Description("OverlapCombineCap")]
        public bool OverlapCombineCap
        {
           get { return m_OverlapCombineCap; }
           set { m_OverlapCombineCap = value; }
        }



        //   Attr Name:   LineOfLead
        //   Attr Group:Standard
        //   Alt Display Name:Line of Lead (°)
        //   Description:   The overall line of lead of the entire pole assembly
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LineOfLead;
        [Category("Standard")]
        [Description("LineOfLead")]
        public double LineOfLead
        {
           get { return m_LineOfLead; }
           set { m_LineOfLead = value; }
        }



        //   Attr Name:   LeanDirection
        //   Attr Group:Standard
        //   Alt Display Name:Lean Direction (°)
        //   Description:   Pole lean direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanDirection;
        [Category("Standard")]
        [Description("LeanDirection")]
        public double LeanDirection
        {
           get { return m_LeanDirection; }
           set { m_LeanDirection = value; }
        }



        //   Attr Name:   LeanAmount
        //   Attr Group:Standard
        //   Alt Display Name:Lean Amount (°)
        //   Description:   Pole amount direction in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeanAmount;
        [Category("Standard")]
        [Description("LeanAmount")]
        public double LeanAmount
        {
           get { return m_LeanAmount; }
           set { m_LeanAmount = value; }
        }



        //   Attr Name:   OverturnMoment
        //   Attr Group:Overturn
        //   Alt Display Name:Overturn Moment (ft-lbs)
        //   Description:   Overturn Moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.#
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OverturnMoment;
        [Category("Overturn")]
        [Description("OverturnMoment")]
        public double OverturnMoment
        {
           get { return m_OverturnMoment; }
           set { m_OverturnMoment = value; }
        }



        //   Attr Name:   Aux Data 1
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_1;
        [Category("User Data")]
        [Description("Aux Data 1")]
        public string Aux_Data_1
        {
           get { return m_Aux_Data_1; }
           set { m_Aux_Data_1 = value; }
        }



        //   Attr Name:   Aux Data 2
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_2;
        [Category("User Data")]
        [Description("Aux Data 2")]
        public string Aux_Data_2
        {
           get { return m_Aux_Data_2; }
           set { m_Aux_Data_2 = value; }
        }



        //   Attr Name:   Aux Data 3
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_3;
        [Category("User Data")]
        [Description("Aux Data 3")]
        public string Aux_Data_3
        {
           get { return m_Aux_Data_3; }
           set { m_Aux_Data_3 = value; }
        }



        //   Attr Name:   Aux Data 4
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_4;
        [Category("User Data")]
        [Description("Aux Data 4")]
        public string Aux_Data_4
        {
           get { return m_Aux_Data_4; }
           set { m_Aux_Data_4 = value; }
        }



        //   Attr Name:   Aux Data 5
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_5;
        [Category("User Data")]
        [Description("Aux Data 5")]
        public string Aux_Data_5
        {
           get { return m_Aux_Data_5; }
           set { m_Aux_Data_5 = value; }
        }



        //   Attr Name:   Aux Data 6
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_6;
        [Category("User Data")]
        [Description("Aux Data 6")]
        public string Aux_Data_6
        {
           get { return m_Aux_Data_6; }
           set { m_Aux_Data_6 = value; }
        }



        //   Attr Name:   Aux Data 7
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_7;
        [Category("User Data")]
        [Description("Aux Data 7")]
        public string Aux_Data_7
        {
           get { return m_Aux_Data_7; }
           set { m_Aux_Data_7 = value; }
        }



        //   Attr Name:   Aux Data 8
        //   Attr Group:User Data
        //   Description:   
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Aux_Data_8;
        [Category("User Data")]
        [Description("Aux Data 8")]
        public string Aux_Data_8
        {
           get { return m_Aux_Data_8; }
           set { m_Aux_Data_8 = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Anchor
   // Mirrors: PPLAnchor : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Anchor : ElementBase
   {

      public static string gXMLkey = "Anchor";
      public override string XMLkey() { return gXMLkey; }

      public Anchor(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Anchor";
               m_Owner = "<Undefined>";
               m_CoordinateZ = 0;
               m_CoordinateX = 381;
               m_CoordinateA = 1.5707963267949;
               m_DeltaHeight = 0;
               m_OffsetAngle = 0;
               m_RodDiameterInInches = 0.75;
               m_RodLengthAboveGLInInches = 30;
               m_RodDescription = "Joslyn Copperbonded 1in x 10ft Twineye";
               m_RodStrength = 45000;
               m_MergeAnchors = false;
               m_HoldingStrength = 20000;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is GuyBrace) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the anchor
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Anchor
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Height from GL (ft)
        //   Description:   The vertical distance from the butt of the pole to the anchor point
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Alt Display Name:Lead Length (ft)
        //   Description:   The horizontal lead length from the pole to the anchor point
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   381
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Lead Angle (°)
        //   Description:   Lead Angle
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   1.5707963267949
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   DeltaHeight
        //   Attr Group:Standard
        //   Alt Display Name:Delta Height (ft)
        //   Description:   Delta Height
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DeltaHeight;
        [Category("Standard")]
        [Description("DeltaHeight")]
        public double DeltaHeight
        {
           get { return m_DeltaHeight; }
           set { m_DeltaHeight = value; }
        }



        //   Attr Name:   OffsetAngle
        //   Attr Group:Standard
        //   Alt Display Name:Offset Angle (°)
        //   Description:   Offset Angle
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OffsetAngle;
        [Category("Standard")]
        [Description("OffsetAngle")]
        public double OffsetAngle
        {
           get { return m_OffsetAngle; }
           set { m_OffsetAngle = value; }
        }



        //   Attr Name:   RodDiameterInInches
        //   Attr Group:Standard
        //   Alt Display Name:Rod Diameter (in)
        //   Description:   The diameter of the rod associated with this anchor
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.75
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RodDiameterInInches;
        [Category("Standard")]
        [Description("RodDiameterInInches")]
        public double RodDiameterInInches
        {
           get { return m_RodDiameterInInches; }
           set { m_RodDiameterInInches = value; }
        }



        //   Attr Name:   RodLengthAboveGLInInches
        //   Attr Group:Standard
        //   Alt Display Name:Rod Length AGL (in)
        //   Description:   The length of the rod associated with this anchor above GL
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   30
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RodLengthAboveGLInInches;
        [Category("Standard")]
        [Description("RodLengthAboveGLInInches")]
        public double RodLengthAboveGLInInches
        {
           get { return m_RodLengthAboveGLInInches; }
           set { m_RodLengthAboveGLInInches = value; }
        }



        //   Attr Name:   RodDescription
        //   Attr Group:Standard
        //   Alt Display Name:Rod Description
        //   Description:   Description of the rod used at the site of the anchor
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Joslyn Copperbonded 1in x 10ft Twineye
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_RodDescription;
        [Category("Standard")]
        [Description("RodDescription")]
        public string RodDescription
        {
           get { return m_RodDescription; }
           set { m_RodDescription = value; }
        }



        //   Attr Name:   RodStrength
        //   Attr Group:Standard
        //   Alt Display Name:Rod Strength (lbs)
        //   Description:   The strength of the rod in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   45000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RodStrength;
        [Category("Standard")]
        [Description("RodStrength")]
        public double RodStrength
        {
           get { return m_RodStrength; }
           set { m_RodStrength = value; }
        }



        //   Attr Name:   MergeAnchors
        //   Attr Group:Standard
        //   Alt Display Name:Merge Anchors
        //   Description:   Merge with anchors at same position,
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_MergeAnchors;
        [Category("Standard")]
        [Description("MergeAnchors")]
        public bool MergeAnchors
        {
           get { return m_MergeAnchors; }
           set { m_MergeAnchors = value; }
        }



        //   Attr Name:   SoilClass
        //   Attr Group:Standard
        //   Alt Display Name:Soil Class
        //   Description:   The class of soil at the site of the anchor
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Class 1  (Very dense and/or cemented sands, coarse gravel and cobbles)
        //        Class 2  (Dense fine sand, very hard silts and clays (may be preloaded))
        //        Class 3  (Dense sands and gravel, hard silts and clays)
        //        Class 4  (Medium dense sandy gravel, very stiff to hard silts and clays)
        //        Class 5  (Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays)
        //        Class 6  (Loose to medium dense fine to coarse sand, firm to stiff clays and silts)
        //        Class 7  (Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill)
        //        Class 8  (Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays)
        //        Unsset  (Unset)
        public enum SoilClass_val
        {
           [Description("Class 0")]
           Class_0,    //Sound hard rock, bedrock, unweathered
           [Description("Class 1")]
           Class_1,    //Very dense and/or cemented sands, coarse gravel and cobbles
           [Description("Class 2")]
           Class_2,    //Dense fine sand, very hard silts and clays (may be preloaded)
           [Description("Class 3")]
           Class_3,    //Dense sands and gravel, hard silts and clays
           [Description("Class 4")]
           Class_4,    //Medium dense sandy gravel, very stiff to hard silts and clays
           [Description("Class 5")]
           Class_5,    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
           [Description("Class 6")]
           Class_6,    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
           [Description("Class 7")]
           Class_7,    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
           [Description("Class 8")]
           Class_8,    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
           [Description("Unsset")]
           Unsset     //Unset
        }
        private SoilClass_val m_SoilClass;
        [Category("Standard")]
        [Description("SoilClass")]
        public SoilClass_val SoilClass
        {
           get
           { return m_SoilClass; }
           set
           { m_SoilClass = value; }
        }

        public SoilClass_val String_to_SoilClass_val(string pKey)
        {
           switch (pKey)
           {
                case "Class 0":
                   return SoilClass_val.Class_0;    //Sound hard rock, bedrock, unweathered
                case "Class 1":
                   return SoilClass_val.Class_1;    //Very dense and/or cemented sands, coarse gravel and cobbles
                case "Class 2":
                   return SoilClass_val.Class_2;    //Dense fine sand, very hard silts and clays (may be preloaded)
                case "Class 3":
                   return SoilClass_val.Class_3;    //Dense sands and gravel, hard silts and clays
                case "Class 4":
                   return SoilClass_val.Class_4;    //Medium dense sandy gravel, very stiff to hard silts and clays
                case "Class 5":
                   return SoilClass_val.Class_5;    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case "Class 6":
                   return SoilClass_val.Class_6;    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case "Class 7":
                   return SoilClass_val.Class_7;    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case "Class 8":
                   return SoilClass_val.Class_8;    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case "Unsset":
                   return SoilClass_val.Unsset;    //Unset
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string SoilClass_val_to_String(SoilClass_val pKey)
        {
           switch (pKey)
           {
                case SoilClass_val.Class_0:
                   return "Class 0";    //Sound hard rock, bedrock, unweathered
                case SoilClass_val.Class_1:
                   return "Class 1";    //Very dense and/or cemented sands, coarse gravel and cobbles
                case SoilClass_val.Class_2:
                   return "Class 2";    //Dense fine sand, very hard silts and clays (may be preloaded)
                case SoilClass_val.Class_3:
                   return "Class 3";    //Dense sands and gravel, hard silts and clays
                case SoilClass_val.Class_4:
                   return "Class 4";    //Medium dense sandy gravel, very stiff to hard silts and clays
                case SoilClass_val.Class_5:
                   return "Class 5";    //Medium dense coarse sand and sandy gravels, stiff to very stiff silts and clays
                case SoilClass_val.Class_6:
                   return "Class 6";    //Loose to medium dense fine to coarse sand, firm to stiff clays and silts
                case SoilClass_val.Class_7:
                   return "Class 7";    //Loose fine sand, alluvium, loess clays, soft-firm clays, varied clays, fill
                case SoilClass_val.Class_8:
                   return "Class 8";    //Peat, organic silts, inundated silts, fly ash, very loose sands, very soft to soft clays
                case SoilClass_val.Unsset:
                   return "Unsset";    //Unset
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   HoldingStrength
        //   Attr Group:Standard
        //   Alt Display Name:Holding Strength (lbs)
        //   Description:   The holding strength of anchor in pounds for the selected soil class
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   20000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_HoldingStrength;
        [Category("Standard")]
        [Description("HoldingStrength")]
        public double HoldingStrength
        {
           get { return m_HoldingStrength; }
           set { m_HoldingStrength = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Crossarm
   // Mirrors: PPLCrossArm : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Crossarm : ElementBase
   {

      public static string gXMLkey = "Crossarm";
      public override string XMLkey() { return gXMLkey; }

      public Crossarm(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_CoordinateX = 0;
               m_Description = "Crossarm";
               m_Owner = "<Undefined>";
               m_CoordinateZ = 462;
               m_CoordinateA = 0;
               m_Type = Type_val.Normal;
               m_Count = 1;
               m_Braced = Braced_val.None;
               m_BraceAll = false;
               m_BraceOffset = 24;
               m_BraceDrop = 24;
               m_LengthInInches = 96;
               m_HeightInInches = 4.5;
               m_DepthInInches = 3.5;
               m_Tilt = 0;
               m_VerticalOffset = 0;
               m_HorizontalOffset = 0;
               m_LateralOffset = 0;
               m_Weight = 50;
               m_Modulus_of_Rupture = 8000;
               m_Modulus_of_Elasticity = 1600000;
               m_PoissonsRatio = 0.3;
               m_WindDragCoef = 0;
               m_ThermalCoefficient = 2.7E-06;
               m_Analysis_Mode = Analysis_Mode_val.Automatic;
               m_AllowableMomentVertical = 500;
               m_AllowableMomentLongitudinal = 500;
               m_AllowableLoadTransverse = 0;
               m_AllowableLoadLongitudinal = 0;
               m_OverrideStrength = false;
               m_StrengthFactor = 0.5;
               m_Analysis_Method = Analysis_Method_val.Superposition;
               m_Offset = 0;
               m_Material = Material_val.Wood;
               m_Species = "Southern Pine";
               m_ArmMaterial = "<Default>";
               m_BraceMaterial = "<Default>";
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Insulator) return true;
         if(pChildCandidate is NodeJunction) return true;
         if(pChildCandidate is Material) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Alt Display Name:Pole Offset (in)
        //   Description:   Distance from the center of the pole to the center of the crossarm
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of crossarm
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Crossarm
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Install Height (ft)
        //   Description:   Distance from the butt of the parent pole to the center of the crossarm
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   462
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The rotation angle around the center of the pole
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Alt Display Name:Install Type
        //   Description:   Crossarm type (centered or offset)
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Normal
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Offset  (Offset)
        //        Pole Extension  (Pole Extension)
        //        Full Gull  (Full Gull)
        //        Half Gull  (Half Gull)
        //        Standoff  (Standoff)
        public enum Type_val
        {
           [Description("Normal")]
           Normal,    //Normal
           [Description("Offset")]
           Offset,    //Offset
           [Description("Pole Extension")]
           Pole_Extension,    //Pole Extension
           [Description("Full Gull")]
           Full_Gull,    //Full Gull
           [Description("Half Gull")]
           Half_Gull,    //Half Gull
           [Description("Standoff")]
           Standoff     //Standoff
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Normal":
                   return Type_val.Normal;    //Normal
                case "Offset":
                   return Type_val.Offset;    //Offset
                case "Pole Extension":
                   return Type_val.Pole_Extension;    //Pole Extension
                case "Full Gull":
                   return Type_val.Full_Gull;    //Full Gull
                case "Half Gull":
                   return Type_val.Half_Gull;    //Half Gull
                case "Standoff":
                   return Type_val.Standoff;    //Standoff
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Normal:
                   return "Normal";    //Normal
                case Type_val.Offset:
                   return "Offset";    //Offset
                case Type_val.Pole_Extension:
                   return "Pole Extension";    //Pole Extension
                case Type_val.Full_Gull:
                   return "Full Gull";    //Full Gull
                case Type_val.Half_Gull:
                   return "Half Gull";    //Half Gull
                case Type_val.Standoff:
                   return "Standoff";    //Standoff
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Count
        //   Attr Group:Standard
        //   Alt Display Name:Arm Count
        //   Description:   Crossarm count (1 or 2)
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   PLUSMINUS
        //   Default Value:   1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_Count;
        [Category("Standard")]
        [Description("Count")]
        public int Count
        {
           get { return m_Count; }
           set { m_Count = value; }
        }



        //   Attr Name:   Braced
        //   Attr Group:Brace
        //   Alt Display Name:Brace Config.
        //   Description:   Indicates if the crossarm is braced
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   None
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Single  (Single)
        //        None  (None)
        public enum Braced_val
        {
           [Description("Double")]
           Double,    //Double
           [Description("Single")]
           Single,    //Single
           [Description("None")]
           None     //None
        }
        private Braced_val m_Braced;
        [Category("Brace")]
        [Description("Braced")]
        public Braced_val Braced
        {
           get
           { return m_Braced; }
           set
           { m_Braced = value; }
        }

        public Braced_val String_to_Braced_val(string pKey)
        {
           switch (pKey)
           {
                case "Double":
                   return Braced_val.Double;    //Double
                case "Single":
                   return Braced_val.Single;    //Single
                case "None":
                   return Braced_val.None;    //None
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Braced_val_to_String(Braced_val pKey)
        {
           switch (pKey)
           {
                case Braced_val.Double:
                   return "Double";    //Double
                case Braced_val.Single:
                   return "Single";    //Single
                case Braced_val.None:
                   return "None";    //None
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   BraceAll
        //   Attr Group:Brace
        //   Alt Display Name:Brace All Arms
        //   Description:   Brace all arms
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_BraceAll;
        [Category("Brace")]
        [Description("BraceAll")]
        public bool BraceAll
        {
           get { return m_BraceAll; }
           set { m_BraceAll = value; }
        }



        //   Attr Name:   BraceOffset
        //   Attr Group:Brace
        //   Alt Display Name:Brace Horiz Offset (in)
        //   Description:   Horizontal distance to brace point
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   TRACKERX
        //   Default Value:   24.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BraceOffset;
        [Category("Brace")]
        [Description("BraceOffset")]
        public double BraceOffset
        {
           get { return m_BraceOffset; }
           set { m_BraceOffset = value; }
        }



        //   Attr Name:   BraceDrop
        //   Attr Group:Brace
        //   Alt Display Name:Brace Vert Offset (in)
        //   Description:   Vertical distance to brace point
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   TRACKERZ
        //   Default Value:   24.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BraceDrop;
        [Category("Brace")]
        [Description("BraceDrop")]
        public double BraceDrop
        {
           get { return m_BraceDrop; }
           set { m_BraceDrop = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Dimensions
        //   Alt Display Name:Arm Length (ft)
        //   Description:   The length from the crossarm from tip to tip
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   96
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Dimensions")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   HeightInInches
        //   Attr Group:Dimensions
        //   Alt Display Name:Arm Height (in)
        //   Description:   The distance from the top of the crossarm to the bottom
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   4.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_HeightInInches;
        [Category("Dimensions")]
        [Description("HeightInInches")]
        public double HeightInInches
        {
           get { return m_HeightInInches; }
           set { m_HeightInInches = value; }
        }



        //   Attr Name:   DepthInInches
        //   Attr Group:Dimensions
        //   Alt Display Name:Arm Depth (in)
        //   Description:   The distance (depth) from the face of the crossarm to the surface of the pole in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DepthInInches;
        [Category("Dimensions")]
        [Description("DepthInInches")]
        public double DepthInInches
        {
           get { return m_DepthInInches; }
           set { m_DepthInInches = value; }
        }



        //   Attr Name:   Tilt
        //   Attr Group:Physical
        //   Alt Display Name:Arm Tilt
        //   Description:   The crossarm tilt in radians.
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Tilt;
        [Category("Physical")]
        [Description("Tilt")]
        public double Tilt
        {
           get { return m_Tilt; }
           set { m_Tilt = value; }
        }



        //   Attr Name:   VerticalOffset
        //   Attr Group:Physical
        //   Alt Display Name:Vertical Offset (in)
        //   Description:   Vertical Offset
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_VerticalOffset;
        [Category("Physical")]
        [Description("VerticalOffset")]
        public double VerticalOffset
        {
           get { return m_VerticalOffset; }
           set { m_VerticalOffset = value; }
        }



        //   Attr Name:   HorizontalOffset
        //   Attr Group:Physical
        //   Alt Display Name:Horizontal Offset (in)
        //   Description:   Horizontal Offset
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_HorizontalOffset;
        [Category("Physical")]
        [Description("HorizontalOffset")]
        public double HorizontalOffset
        {
           get { return m_HorizontalOffset; }
           set { m_HorizontalOffset = value; }
        }



        //   Attr Name:   LateralOffset
        //   Attr Group:Physical
        //   Alt Display Name:Lateral Offset (in)
        //   Description:   Lateral Offset
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LateralOffset;
        [Category("Physical")]
        [Description("LateralOffset")]
        public double LateralOffset
        {
           get { return m_LateralOffset; }
           set { m_LateralOffset = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Physical
        //   Alt Display Name:Arm Weight (lbs)
        //   Description:   Crossarm weight in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   50
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Physical")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   Modulus of Rupture
        //   Attr Group:Physical
        //   Alt Display Name:Modulus of Rupture (psi)
        //   Description:   Modulus of rupture
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   8000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Rupture;
        [Category("Physical")]
        [Description("Modulus of Rupture")]
        public double Modulus_of_Rupture
        {
           get { return m_Modulus_of_Rupture; }
           set { m_Modulus_of_Rupture = value; }
        }



        //   Attr Name:   Modulus of Elasticity
        //   Attr Group:Physical
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   Modulus of elasticty for the crossarm
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   1600000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Modulus_of_Elasticity;
        [Category("Physical")]
        [Description("Modulus of Elasticity")]
        public double Modulus_of_Elasticity
        {
           get { return m_Modulus_of_Elasticity; }
           set { m_Modulus_of_Elasticity = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Physical
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.3
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Physical")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Physical
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Physical")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Physical
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000027
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Physical")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   Analysis Mode
        //   Attr Group:Analysis
        //   Alt Display Name:Capacity Method
        //   Description:   Analysis Mode
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Automatic
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Manual  (Manual)
        public enum Analysis_Mode_val
        {
           [Description("Automatic")]
           Automatic,    //Automatic
           [Description("Manual")]
           Manual     //Manual
        }
        private Analysis_Mode_val m_Analysis_Mode;
        [Category("Analysis")]
        [Description("Analysis Mode")]
        public Analysis_Mode_val Analysis_Mode
        {
           get
           { return m_Analysis_Mode; }
           set
           { m_Analysis_Mode = value; }
        }

        public Analysis_Mode_val String_to_Analysis_Mode_val(string pKey)
        {
           switch (pKey)
           {
                case "Automatic":
                   return Analysis_Mode_val.Automatic;    //Automatic
                case "Manual":
                   return Analysis_Mode_val.Manual;    //Manual
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Analysis_Mode_val_to_String(Analysis_Mode_val pKey)
        {
           switch (pKey)
           {
                case Analysis_Mode_val.Automatic:
                   return "Automatic";    //Automatic
                case Analysis_Mode_val.Manual:
                   return "Manual";    //Manual
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   AllowableMomentVertical
        //   Attr Group:Analysis
        //   Alt Display Name:Max Moment Vert (ft-lb)
        //   Description:   AllowableMomentVertical
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_AllowableMomentVertical;
        [Category("Analysis")]
        [Description("AllowableMomentVertical")]
        public double AllowableMomentVertical
        {
           get { return m_AllowableMomentVertical; }
           set { m_AllowableMomentVertical = value; }
        }



        //   Attr Name:   AllowableMomentLongitudinal
        //   Attr Group:Analysis
        //   Alt Display Name:Max Moment Long (ft-lb)
        //   Description:   AllowableMomentLongitudinal
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_AllowableMomentLongitudinal;
        [Category("Analysis")]
        [Description("AllowableMomentLongitudinal")]
        public double AllowableMomentLongitudinal
        {
           get { return m_AllowableMomentLongitudinal; }
           set { m_AllowableMomentLongitudinal = value; }
        }



        //   Attr Name:   AllowableLoadTransverse
        //   Attr Group:Analysis
        //   Alt Display Name:Max Load Shear (lbs)
        //   Description:   AllowableLoadTransverse
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_AllowableLoadTransverse;
        [Category("Analysis")]
        [Description("AllowableLoadTransverse")]
        public double AllowableLoadTransverse
        {
           get { return m_AllowableLoadTransverse; }
           set { m_AllowableLoadTransverse = value; }
        }



        //   Attr Name:   AllowableLoadLongitudinal
        //   Attr Group:Analysis
        //   Alt Display Name:Max Load Tension (lbs)
        //   Description:   AllowableLoadLongitudinal
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_AllowableLoadLongitudinal;
        [Category("Analysis")]
        [Description("AllowableLoadLongitudinal")]
        public double AllowableLoadLongitudinal
        {
           get { return m_AllowableLoadLongitudinal; }
           set { m_AllowableLoadLongitudinal = value; }
        }



        //   Attr Name:   OverrideStrength
        //   Attr Group:Analysis
        //   Alt Display Name:Override Strength Factor
        //   Description:   Override Nominal Strength Factor
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverrideStrength;
        [Category("Analysis")]
        [Description("OverrideStrength")]
        public bool OverrideStrength
        {
           get { return m_OverrideStrength; }
           set { m_OverrideStrength = value; }
        }



        //   Attr Name:   StrengthFactor
        //   Attr Group:Analysis
        //   Alt Display Name:Strength Factor
        //   Description:   Crosarm Strength Factor value
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   0.50
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrengthFactor;
        [Category("Analysis")]
        [Description("StrengthFactor")]
        public double StrengthFactor
        {
           get { return m_StrengthFactor; }
           set { m_StrengthFactor = value; }
        }



        //   Attr Name:   Analysis Method
        //   Attr Group:Analysis
        //   Description:   Analysis Method
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Superposition
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Interaction  (Interaction)
        //        Worst Axis  (Worst Axis)
        public enum Analysis_Method_val
        {
           [Description("Superposition")]
           Superposition,    //Superposition
           [Description("Interaction")]
           Interaction,    //Interaction
           [Description("Worst Axis")]
           Worst_Axis     //Worst Axis
        }
        private Analysis_Method_val m_Analysis_Method;
        [Category("Analysis")]
        [Description("Analysis Method")]
        public Analysis_Method_val Analysis_Method
        {
           get
           { return m_Analysis_Method; }
           set
           { m_Analysis_Method = value; }
        }

        public Analysis_Method_val String_to_Analysis_Method_val(string pKey)
        {
           switch (pKey)
           {
                case "Superposition":
                   return Analysis_Method_val.Superposition;    //Superposition
                case "Interaction":
                   return Analysis_Method_val.Interaction;    //Interaction
                case "Worst Axis":
                   return Analysis_Method_val.Worst_Axis;    //Worst Axis
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Analysis_Method_val_to_String(Analysis_Method_val pKey)
        {
           switch (pKey)
           {
                case Analysis_Method_val.Superposition:
                   return "Superposition";    //Superposition
                case Analysis_Method_val.Interaction:
                   return "Interaction";    //Interaction
                case Analysis_Method_val.Worst_Axis:
                   return "Worst Axis";    //Worst Axis
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Offset
        //   Attr Group:Multi Pole
        //   Alt Display Name:Offset (ft)
        //   Description:   Multi Pole offset in feet
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_Offset;
        [Category("Multi Pole")]
        [Description("Offset")]
        public double Offset
        {
           get { return m_Offset; }
           set { m_Offset = value; }
        }



        //   Attr Name:   Material
        //   Attr Group:Material
        //   Description:   Material
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Wood
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Steel  (Steel)
        //        Composite  (Composite)
        //        Other  (Other)
        public enum Material_val
        {
           [Description("Wood")]
           Wood,    //Wood
           [Description("Steel")]
           Steel,    //Steel
           [Description("Composite")]
           Composite,    //Composite
           [Description("Other")]
           Other     //Other
        }
        private Material_val m_Material;
        [Category("Material")]
        [Description("Material")]
        public Material_val Material
        {
           get
           { return m_Material; }
           set
           { m_Material = value; }
        }

        public Material_val String_to_Material_val(string pKey)
        {
           switch (pKey)
           {
                case "Wood":
                   return Material_val.Wood;    //Wood
                case "Steel":
                   return Material_val.Steel;    //Steel
                case "Composite":
                   return Material_val.Composite;    //Composite
                case "Other":
                   return Material_val.Other;    //Other
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Material_val_to_String(Material_val pKey)
        {
           switch (pKey)
           {
                case Material_val.Wood:
                   return "Wood";    //Wood
                case Material_val.Steel:
                   return "Steel";    //Steel
                case Material_val.Composite:
                   return "Composite";    //Composite
                case Material_val.Other:
                   return "Other";    //Other
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Species
        //   Attr Group:Material
        //   Description:   Wood species of the crossarm
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Southern Pine
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Species;
        [Category("Material")]
        [Description("Species")]
        public string Species
        {
           get { return m_Species; }
           set { m_Species = value; }
        }



        //   Attr Name:   ArmMaterial
        //   Attr Group:Material
        //   Alt Display Name:Arm Material Props
        //   Description:   Arm Material
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   CHILD_MATERIAL_NAME
        //   Default Value:   <Default>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_ArmMaterial;
        [Category("Material")]
        [Description("ArmMaterial")]
        public string ArmMaterial
        {
           get { return m_ArmMaterial; }
           set { m_ArmMaterial = value; }
        }



        //   Attr Name:   BraceMaterial
        //   Attr Group:Material
        //   Alt Display Name:Brace Material Props
        //   Description:   Brace Material
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   CHILD_MATERIAL_NAME
        //   Default Value:   <Default>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_BraceMaterial;
        [Category("Material")]
        [Description("BraceMaterial")]
        public string BraceMaterial
        {
           get { return m_BraceMaterial; }
           set { m_BraceMaterial = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Insulator
   // Mirrors: PPLInsulator : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Insulator : ElementBase
   {

      public static string gXMLkey = "Insulator";
      public override string XMLkey() { return gXMLkey; }

      public Insulator(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Insulator";
               m_Owner = "<Undefined>";
               m_Type = Type_val.Pin;
               m_CoordinateZ = 300;
               m_CoordinateA = 0;
               m_Side = Side_val.Inline;
               m_CoordinateX = 0;
               m_LengthInInches = 8;
               m_DavitAngle = 1.18682389135614;
               m_Pitch = 0;
               m_Crab = 0;
               m_WidthInInches = 3;
               m_Weight = 8.99;
               m_Sheds = Sheds_val._Default_;
               m_WindDragCoef = 0;
               m_EndFitting = EndFitting_val.Clamped;
               m_StalkMaterial = "<Default>";
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Span) return true;
         if(pChildCandidate is SpanBundle) return true;
         if(pChildCandidate is GenericEquipment) return true;
         if(pChildCandidate is Material) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the insulator.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Insulator
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   The span connector type.  This may be a style of insulator, j-hook, or other hardware used to attach spans to the pole.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Pin
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Post  (Post)
        //        Davit  (Davit)
        //        Spool  (Spool)
        //        Underhung  (Underhung)
        //        Suspension  (Suspension)
        //        Deadend  (Deadend)
        //        J-Hook  (J-Hook)
        //        Bolt  (Bolt)
        //        Extension  (Extension)
        public enum Type_val
        {
           [Description("Pin")]
           Pin,    //Pin
           [Description("Post")]
           Post,    //Post
           [Description("Davit")]
           Davit,    //Davit
           [Description("Spool")]
           Spool,    //Spool
           [Description("Underhung")]
           Underhung,    //Underhung
           [Description("Suspension")]
           Suspension,    //Suspension
           [Description("Deadend")]
           Deadend,    //Deadend
           [Description("J-Hook")]
           J_Hook,    //J-Hook
           [Description("Bolt")]
           Bolt,    //Bolt
           [Description("Extension")]
           Extension     //Extension
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Pin":
                   return Type_val.Pin;    //Pin
                case "Post":
                   return Type_val.Post;    //Post
                case "Davit":
                   return Type_val.Davit;    //Davit
                case "Spool":
                   return Type_val.Spool;    //Spool
                case "Underhung":
                   return Type_val.Underhung;    //Underhung
                case "Suspension":
                   return Type_val.Suspension;    //Suspension
                case "Deadend":
                   return Type_val.Deadend;    //Deadend
                case "J-Hook":
                   return Type_val.J_Hook;    //J-Hook
                case "Bolt":
                   return Type_val.Bolt;    //Bolt
                case "Extension":
                   return Type_val.Extension;    //Extension
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Pin:
                   return "Pin";    //Pin
                case Type_val.Post:
                   return "Post";    //Post
                case Type_val.Davit:
                   return "Davit";    //Davit
                case Type_val.Spool:
                   return "Spool";    //Spool
                case Type_val.Underhung:
                   return "Underhung";    //Underhung
                case Type_val.Suspension:
                   return "Suspension";    //Suspension
                case Type_val.Deadend:
                   return "Deadend";    //Deadend
                case Type_val.J_Hook:
                   return "J-Hook";    //J-Hook
                case Type_val.Bolt:
                   return "Bolt";    //Bolt
                case Type_val.Extension:
                   return "Extension";    //Extension
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Install Height (ft)
        //   Description:   The Z coordinate relative to the parent.  This value is frequently set by SnapToParent
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   300.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The rotation of the insulator / span holder relative to its parent.  If the orientation is non-zero the stalk with lean alone this axis
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   Side
        //   Attr Group:Standard
        //   Description:   The span connector pole side (for insulators on a pole only).
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Inline
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Field  (Field)
        //        Inline  (Inline)
        //        Front  (Front)
        //        Back  (Back)
        //        Both  (Both)
        //        Split  (Split)
        public enum Side_val
        {
           [Description("Street")]
           Street,    //Street
           [Description("Field")]
           Field,    //Field
           [Description("Inline")]
           Inline,    //Inline
           [Description("Front")]
           Front,    //Front
           [Description("Back")]
           Back,    //Back
           [Description("Both")]
           Both,    //Both
           [Description("Split")]
           Split     //Split
        }
        private Side_val m_Side;
        [Category("Standard")]
        [Description("Side")]
        public Side_val Side
        {
           get
           { return m_Side; }
           set
           { m_Side = value; }
        }

        public Side_val String_to_Side_val(string pKey)
        {
           switch (pKey)
           {
                case "Street":
                   return Side_val.Street;    //Street
                case "Field":
                   return Side_val.Field;    //Field
                case "Inline":
                   return Side_val.Inline;    //Inline
                case "Front":
                   return Side_val.Front;    //Front
                case "Back":
                   return Side_val.Back;    //Back
                case "Both":
                   return Side_val.Both;    //Both
                case "Split":
                   return Side_val.Split;    //Split
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Side_val_to_String(Side_val pKey)
        {
           switch (pKey)
           {
                case Side_val.Street:
                   return "Street";    //Street
                case Side_val.Field:
                   return "Field";    //Field
                case Side_val.Inline:
                   return "Inline";    //Inline
                case Side_val.Front:
                   return "Front";    //Front
                case Side_val.Back:
                   return "Back";    //Back
                case Side_val.Both:
                   return "Both";    //Both
                case Side_val.Split:
                   return "Split";    //Split
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Alt Display Name:Horizontal Offset (in)
        //   Description:   Distance from the center of the parent.  In the case of a crossarm this is the position along the arm.  In the case of poles this is typically set by SnapToParent
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Properties
        //   Alt Display Name:Unit Length (in)
        //   Description:   The total length of the insulator structure
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   8.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Properties")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   DavitAngle
        //   Attr Group:Standard
        //   Alt Display Name:Davit Angle (°)
        //   Description:   Davit angle in radians.
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   1.18682389135614
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DavitAngle;
        [Category("Standard")]
        [Description("DavitAngle")]
        public double DavitAngle
        {
           get { return m_DavitAngle; }
           set { m_DavitAngle = value; }
        }



        //   Attr Name:   Pitch
        //   Attr Group:Properties
        //   Alt Display Name:Tilt (°)
        //   Description:   Pitch in radians.
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Pitch;
        [Category("Properties")]
        [Description("Pitch")]
        public double Pitch
        {
           get { return m_Pitch; }
           set { m_Pitch = value; }
        }



        //   Attr Name:   Crab
        //   Attr Group:Properties
        //   Alt Display Name:Crab Angle (°)
        //   Description:   Crab angle in radians.
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Crab;
        [Category("Properties")]
        [Description("Crab")]
        public double Crab
        {
           get { return m_Crab; }
           set { m_Crab = value; }
        }



        //   Attr Name:   WidthInInches
        //   Attr Group:Properties
        //   Alt Display Name:Unit Width (in)
        //   Description:   The effective width for wind area of the insulator structure
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WidthInInches;
        [Category("Properties")]
        [Description("WidthInInches")]
        public double WidthInInches
        {
           get { return m_WidthInInches; }
           set { m_WidthInInches = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Properties
        //   Alt Display Name:Unit Weight (lbs)
        //   Description:   Weight of the insulator / span holder in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   8.99
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Properties")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   Sheds
        //   Attr Group:Properties
        //   Alt Display Name:Shed Count
        //   Description:   Number of sheds
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   <Default>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        1  (1)
        //        2  (2)
        //        3  (3)
        //        4  (4)
        //        5  (5)
        //        6  (6)
        //        7  (7)
        //        8  (8)
        public enum Sheds_val
        {
           [Description("<Default>")]
           _Default_,    //<Default>
           [Description("1")]
           Sheds_1,    //1
           [Description("2")]
           Sheds_2,    //2
           [Description("3")]
           Sheds_3,    //3
           [Description("4")]
           Sheds_4,    //4
           [Description("5")]
           Sheds_5,    //5
           [Description("6")]
           Sheds_6,    //6
           [Description("7")]
           Sheds_7,    //7
           [Description("8")]
           Sheds_8     //8
        }
        private Sheds_val m_Sheds;
        [Category("Properties")]
        [Description("Sheds")]
        public Sheds_val Sheds
        {
           get
           { return m_Sheds; }
           set
           { m_Sheds = value; }
        }

        public Sheds_val String_to_Sheds_val(string pKey)
        {
           switch (pKey)
           {
                case "<Default>":
                   return Sheds_val._Default_;    //<Default>
                case "1":
                   return Sheds_val.Sheds_1;    //1
                case "2":
                   return Sheds_val.Sheds_2;    //2
                case "3":
                   return Sheds_val.Sheds_3;    //3
                case "4":
                   return Sheds_val.Sheds_4;    //4
                case "5":
                   return Sheds_val.Sheds_5;    //5
                case "6":
                   return Sheds_val.Sheds_6;    //6
                case "7":
                   return Sheds_val.Sheds_7;    //7
                case "8":
                   return Sheds_val.Sheds_8;    //8
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Sheds_val_to_String(Sheds_val pKey)
        {
           switch (pKey)
           {
                case Sheds_val._Default_:
                   return "<Default>";    //<Default>
                case Sheds_val.Sheds_1:
                   return "1";    //1
                case Sheds_val.Sheds_2:
                   return "2";    //2
                case Sheds_val.Sheds_3:
                   return "3";    //3
                case Sheds_val.Sheds_4:
                   return "4";    //4
                case Sheds_val.Sheds_5:
                   return "5";    //5
                case Sheds_val.Sheds_6:
                   return "6";    //6
                case Sheds_val.Sheds_7:
                   return "7";    //7
                case Sheds_val.Sheds_8:
                   return "8";    //8
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Properties
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Properties")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   EndFitting
        //   Attr Group:Properties
        //   Alt Display Name:Line End Fitting
        //   Description:   Line End Fitting
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Clamped
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Free  (Free)
        public enum EndFitting_val
        {
           [Description("Clamped")]
           Clamped,    //Clamped
           [Description("Free")]
           Free     //Free
        }
        private EndFitting_val m_EndFitting;
        [Category("Properties")]
        [Description("EndFitting")]
        public EndFitting_val EndFitting
        {
           get
           { return m_EndFitting; }
           set
           { m_EndFitting = value; }
        }

        public EndFitting_val String_to_EndFitting_val(string pKey)
        {
           switch (pKey)
           {
                case "Clamped":
                   return EndFitting_val.Clamped;    //Clamped
                case "Free":
                   return EndFitting_val.Free;    //Free
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string EndFitting_val_to_String(EndFitting_val pKey)
        {
           switch (pKey)
           {
                case EndFitting_val.Clamped:
                   return "Clamped";    //Clamped
                case EndFitting_val.Free:
                   return "Free";    //Free
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   StalkMaterial
        //   Attr Group:Material
        //   Alt Display Name:Stalk Material
        //   Description:   Stalk Material
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   CHILD_MATERIAL_NAME
        //   Default Value:   <Default>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_StalkMaterial;
        [Category("Material")]
        [Description("StalkMaterial")]
        public string StalkMaterial
        {
           get { return m_StalkMaterial; }
           set { m_StalkMaterial = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Span
   // Mirrors: PPLSpan : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Span : ElementBase
   {

      public static string gXMLkey = "Span";
      public override string XMLkey() { return gXMLkey; }

      public Span(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_CoordinateX = 0;
               m_CoordinateZ = 0;
               m_SpanType = SpanType_val.Primary;
               m_Owner = "<Undefined>";
               m_Type = "Generic Span";
               m_CoordinateA = 0;
               m_SpanDistanceInInches = 600;
               m_SpanEndHeightDelta = 0;
               m_MidspanDeflection = 0;
               m_Tension_Type = Tension_Type_val.Static;
               m_Tension = 0;
               m_TensionTable = new ValTable("Tension;0,500;");
               m_SlackTension = 10;
               m_RatedStrength = 5000;
               m_ConductorDiameter = 0.5;
               m_OverrideTemp = false;
               m_Temperature = 65;
               m_TempMin = 32;
               m_TempMax = 212;
               m_PoundsPerInch = 0.0076;
               m_ModulusOfElasticity = 11200000;
               m_PercentSolid = 0.75;
               m_ThermalCoefficient = 1.06E-05;
               m_CreepCoefficient = 0;
               m_IceAccumulationFactor = 0.75;
               m_WindTensionFactor = -1;
               m_WindDragCoef = 0;
               m_VerticalOffset = 0;
               m_HorizontalOffset = 0;
               m_StopAtTap = false;
               m_HasInlineBox = false;
               m_BoxOffset = 15;
               m_BoxLength = 20;
               m_BoxDiameter = 8;
               m_BoxWeight = 5;
               m_HasDripLoop = false;
               m_DripLoopOffset = 2;
               m_DripLoopLength = 20;
               m_DripLoopHeight = 10;
               m_Modifier = Modifier_val.None;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Tap) return true;
         if(pChildCandidate is SpanAddition) return true;
         if(pChildCandidate is Clearance) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Description:   The X coordinate relative to parent.  SnapToParent will set the value
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Description:   The Z coordinate relative to parent.  SnapToParent will set the value
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   SpanType
        //   Attr Group:Standard
        //   Description:   What type of span is this (CATV, Telco, Primary, Secondary / Service)
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Primary
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Secondary  (Secondary)
        //        Service  (Service)
        //        Neutral  (Neutral)
        //        Telco  (Telco)
        //        CATV  (CATV)
        //        Fiber  (Fiber)
        //        Sub-Transmission  (Sub-Transmission)
        //        Other  (Other)
        //        Unknown  (Unknown)
        public enum SpanType_val
        {
           [Description("Primary")]
           Primary,    //Primary
           [Description("Secondary")]
           Secondary,    //Secondary
           [Description("Service")]
           Service,    //Service
           [Description("Neutral")]
           Neutral,    //Neutral
           [Description("Telco")]
           Telco,    //Telco
           [Description("CATV")]
           CATV,    //CATV
           [Description("Fiber")]
           Fiber,    //Fiber
           [Description("Sub-Transmission")]
           Sub_Transmission,    //Sub-Transmission
           [Description("Other")]
           Other,    //Other
           [Description("Unknown")]
           Unknown     //Unknown
        }
        private SpanType_val m_SpanType;
        [Category("Standard")]
        [Description("SpanType")]
        public SpanType_val SpanType
        {
           get
           { return m_SpanType; }
           set
           { m_SpanType = value; }
        }

        public SpanType_val String_to_SpanType_val(string pKey)
        {
           switch (pKey)
           {
                case "Primary":
                   return SpanType_val.Primary;    //Primary
                case "Secondary":
                   return SpanType_val.Secondary;    //Secondary
                case "Service":
                   return SpanType_val.Service;    //Service
                case "Neutral":
                   return SpanType_val.Neutral;    //Neutral
                case "Telco":
                   return SpanType_val.Telco;    //Telco
                case "CATV":
                   return SpanType_val.CATV;    //CATV
                case "Fiber":
                   return SpanType_val.Fiber;    //Fiber
                case "Sub-Transmission":
                   return SpanType_val.Sub_Transmission;    //Sub-Transmission
                case "Other":
                   return SpanType_val.Other;    //Other
                case "Unknown":
                   return SpanType_val.Unknown;    //Unknown
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string SpanType_val_to_String(SpanType_val pKey)
        {
           switch (pKey)
           {
                case SpanType_val.Primary:
                   return "Primary";    //Primary
                case SpanType_val.Secondary:
                   return "Secondary";    //Secondary
                case SpanType_val.Service:
                   return "Service";    //Service
                case SpanType_val.Neutral:
                   return "Neutral";    //Neutral
                case SpanType_val.Telco:
                   return "Telco";    //Telco
                case SpanType_val.CATV:
                   return "CATV";    //CATV
                case SpanType_val.Fiber:
                   return "Fiber";    //Fiber
                case SpanType_val.Sub_Transmission:
                   return "Sub-Transmission";    //Sub-Transmission
                case SpanType_val.Other:
                   return "Other";    //Other
                case SpanType_val.Unknown:
                   return "Unknown";    //Unknown
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Alt Display Name:Description
        //   Description:   The name, type, material, code, or other designation assigned to this span type by the owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Generic Span
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Type;
        [Category("Standard")]
        [Description("Type")]
        public string Type
        {
           get { return m_Type; }
           set { m_Type = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The relative angle between this span its proximal connection at it's parent insulator or other holding structure
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   SpanDistanceInInches
        //   Attr Group:Standard
        //   Alt Display Name:Span Length (ft)
        //   Description:   The horizontal component of the distance between the proximal and distal ends of the span
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   600.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_SpanDistanceInInches;
        [Category("Standard")]
        [Description("SpanDistanceInInches")]
        public double SpanDistanceInInches
        {
           get { return m_SpanDistanceInInches; }
           set { m_SpanDistanceInInches = value; }
        }



        //   Attr Name:   SpanEndHeightDelta
        //   Attr Group:Standard
        //   Alt Display Name:End Drop/Rise (ft)
        //   Description:   The vertical component of the distance between the proximal and distal ends of the span
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_SpanEndHeightDelta;
        [Category("Standard")]
        [Description("SpanEndHeightDelta")]
        public double SpanEndHeightDelta
        {
           get { return m_SpanEndHeightDelta; }
           set { m_SpanEndHeightDelta = value; }
        }



        //   Attr Name:   MidspanDeflection
        //   Attr Group:Tension Sag
        //   Alt Display Name:Span Sag (ft)
        //   Description:   The vertical deflection between the proximal end of the span an the maximum deflection point of the catenary curve
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_MidspanDeflection;
        [Category("Tension Sag")]
        [Description("MidspanDeflection")]
        public double MidspanDeflection
        {
           get { return m_MidspanDeflection; }
           set { m_MidspanDeflection = value; }
        }



        //   Attr Name:   Tension Type
        //   Attr Group:Tension Sag
        //   Description:   Is the tension value calculated or static
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Static
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Slack  (The tension is a static slack constant supplied by the operator or other data source)
        //        Table  (The tension is a table supplied by the operator or other data source)
        //        Sag to Tension  (The tension is calculated based on the values entered for sag, weight, and LoadCase)
        //        Tension to Sag  (The tension is based on initial stringing tension and LoadCase)
        public enum Tension_Type_val
        {
           [Description("Static")]
           Static,    //The tension is a static normal constant supplied by the operator or other data source
           [Description("Slack")]
           Slack,    //The tension is a static slack constant supplied by the operator or other data source
           [Description("Table")]
           Table,    //The tension is a table supplied by the operator or other data source
           [Description("Sag to Tension")]
           Sag_to_Tension,    //The tension is calculated based on the values entered for sag, weight, and LoadCase
           [Description("Tension to Sag")]
           Tension_to_Sag     //The tension is based on initial stringing tension and LoadCase
        }
        private Tension_Type_val m_Tension_Type;
        [Category("Tension Sag")]
        [Description("Tension Type")]
        public Tension_Type_val Tension_Type
        {
           get
           { return m_Tension_Type; }
           set
           { m_Tension_Type = value; }
        }

        public Tension_Type_val String_to_Tension_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Static":
                   return Tension_Type_val.Static;    //The tension is a static normal constant supplied by the operator or other data source
                case "Slack":
                   return Tension_Type_val.Slack;    //The tension is a static slack constant supplied by the operator or other data source
                case "Table":
                   return Tension_Type_val.Table;    //The tension is a table supplied by the operator or other data source
                case "Sag to Tension":
                   return Tension_Type_val.Sag_to_Tension;    //The tension is calculated based on the values entered for sag, weight, and LoadCase
                case "Tension to Sag":
                   return Tension_Type_val.Tension_to_Sag;    //The tension is based on initial stringing tension and LoadCase
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Tension_Type_val_to_String(Tension_Type_val pKey)
        {
           switch (pKey)
           {
                case Tension_Type_val.Static:
                   return "Static";    //The tension is a static normal constant supplied by the operator or other data source
                case Tension_Type_val.Slack:
                   return "Slack";    //The tension is a static slack constant supplied by the operator or other data source
                case Tension_Type_val.Table:
                   return "Table";    //The tension is a table supplied by the operator or other data source
                case Tension_Type_val.Sag_to_Tension:
                   return "Sag to Tension";    //The tension is calculated based on the values entered for sag, weight, and LoadCase
                case Tension_Type_val.Tension_to_Sag:
                   return "Tension to Sag";    //The tension is based on initial stringing tension and LoadCase
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Tension
        //   Attr Group:Tension Sag
        //   Alt Display Name:Tension (lbs)
        //   Description:   The tension value used only when "Tension Type" is "Static"
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Tension;
        [Category("Tension Sag")]
        [Description("Tension")]
        public double Tension
        {
           get { return m_Tension; }
           set { m_Tension = value; }
        }



        //   Attr Name:   TensionTable
        //   Attr Group:Tension Sag
        //   Alt Display Name:Tension Table
        //   Description:   The tension values table used only when "Tension Type" is "Table"
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   TENSION_TABLE
        //   Default Value:   Tension;0,500;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_TensionTable = new ValTable();
        [Category("Tension Sag")]
        [Description("TensionTable")]
        public ValTable TensionTable
        {
           get { return m_TensionTable; }
           set { m_TensionTable = value; }
        }



        //   Attr Name:   SlackTension
        //   Attr Group:Tension Sag
        //   Alt Display Name:Slack Tension (lbs)
        //   Description:   The tension value used only when "Tension Type" is "Slack"
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   10.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SlackTension;
        [Category("Tension Sag")]
        [Description("SlackTension")]
        public double SlackTension
        {
           get { return m_SlackTension; }
           set { m_SlackTension = value; }
        }



        //   Attr Name:   RatedStrength
        //   Attr Group:Tension Sag
        //   Alt Display Name:Rated Strength (lbs)
        //   Description:   The rated strength in pounds.
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   5000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RatedStrength;
        [Category("Tension Sag")]
        [Description("RatedStrength")]
        public double RatedStrength
        {
           get { return m_RatedStrength; }
           set { m_RatedStrength = value; }
        }



        //   Attr Name:   ConductorDiameter
        //   Attr Group:Standard
        //   Alt Display Name:Span Diameter (in)
        //   Description:   The conductor diameter in inches including the insulation.
        //   Displayed Units:   store as INCHES display as INCHES or MILLIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.50
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ConductorDiameter;
        [Category("Standard")]
        [Description("ConductorDiameter")]
        public double ConductorDiameter
        {
           get { return m_ConductorDiameter; }
           set { m_ConductorDiameter = value; }
        }



        //   Attr Name:   OverrideTemp
        //   Attr Group:Temperature
        //   Alt Display Name:Override Temp
        //   Description:   Override Nominal Temperature
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverrideTemp;
        [Category("Temperature")]
        [Description("OverrideTemp")]
        public bool OverrideTemp
        {
           get { return m_OverrideTemp; }
           set { m_OverrideTemp = value; }
        }



        //   Attr Name:   Temperature
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Nom (°f)
        //   Description:   Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Temperature;
        [Category("Temperature")]
        [Description("Temperature")]
        public double Temperature
        {
           get { return m_Temperature; }
           set { m_Temperature = value; }
        }



        //   Attr Name:   TempMin
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Min (°f)
        //   Description:   Minimum Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   32
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TempMin;
        [Category("Temperature")]
        [Description("TempMin")]
        public double TempMin
        {
           get { return m_TempMin; }
           set { m_TempMin = value; }
        }



        //   Attr Name:   TempMax
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Max (°f)
        //   Description:   Maximum Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   212
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TempMax;
        [Category("Temperature")]
        [Description("TempMax")]
        public double TempMax
        {
           get { return m_TempMax; }
           set { m_TempMax = value; }
        }



        //   Attr Name:   PoundsPerInch
        //   Attr Group:Phys Const
        //   Alt Display Name:Span Weight (lbs/ft)
        //   Description:   The weight per unit of running length
        //   Displayed Units:   store as POUNDS PER IN display as POUNDS PER FT or KILOGRAMS PER METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0076
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoundsPerInch;
        [Category("Phys Const")]
        [Description("PoundsPerInch")]
        public double PoundsPerInch
        {
           get { return m_PoundsPerInch; }
           set { m_PoundsPerInch = value; }
        }



        //   Attr Name:   ModulusOfElasticity
        //   Attr Group:Phys Const
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   ModulusOfElasticity
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   11200000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ModulusOfElasticity;
        [Category("Phys Const")]
        [Description("ModulusOfElasticity")]
        public double ModulusOfElasticity
        {
           get { return m_ModulusOfElasticity; }
           set { m_ModulusOfElasticity = value; }
        }



        //   Attr Name:   PercentSolid
        //   Attr Group:Phys Const
        //   Alt Display Name:Percent Solid
        //   Description:   Percent Solid
        //   Displayed Units:   store as PERCENT 0 TO 1 display as PERCENT 0 TO 100
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   0.75
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PercentSolid;
        [Category("Phys Const")]
        [Description("PercentSolid")]
        public double PercentSolid
        {
           get { return m_PercentSolid; }
           set { m_PercentSolid = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Phys Const
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000106
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Phys Const")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   CreepCoefficient
        //   Attr Group:Phys Const
        //   Alt Display Name:Creep Coef ((in/in)/lb)
        //   Description:   CreepCoefficient
        //   Displayed Units:   store as CREEP COEFFICIENT
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_CreepCoefficient;
        [Category("Phys Const")]
        [Description("CreepCoefficient")]
        public double CreepCoefficient
        {
           get { return m_CreepCoefficient; }
           set { m_CreepCoefficient = value; }
        }



        //   Attr Name:   IceAccumulationFactor
        //   Attr Group:Tension Sag
        //   Alt Display Name:Ice Accum. Factor
        //   Description:   Ice Accumulation Factor for Tension Calculations
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###
        //   Attribute Type:   FLOAT
        //   Default Value:   0.75
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_IceAccumulationFactor;
        [Category("Tension Sag")]
        [Description("IceAccumulationFactor")]
        public double IceAccumulationFactor
        {
           get { return m_IceAccumulationFactor; }
           set { m_IceAccumulationFactor = value; }
        }



        //   Attr Name:   WindTensionFactor
        //   Attr Group:Tension Sag
        //   Description:   Wind Factor for Tension Calculations
        //   Displayed Units:   INVERTED
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   -1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindTensionFactor;
        [Category("Tension Sag")]
        [Description("WindTensionFactor")]
        public double WindTensionFactor
        {
           get { return m_WindTensionFactor; }
           set { m_WindTensionFactor = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Tension Sag
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Tension Sag")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   VerticalOffset
        //   Attr Group:Phys Const
        //   Alt Display Name:Vertical Offset (in)
        //   Description:   Vertical Offset in Inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_VerticalOffset;
        [Category("Phys Const")]
        [Description("VerticalOffset")]
        public double VerticalOffset
        {
           get { return m_VerticalOffset; }
           set { m_VerticalOffset = value; }
        }



        //   Attr Name:   HorizontalOffset
        //   Attr Group:Phys Const
        //   Alt Display Name:Horizontal Offset (in)
        //   Description:   Horizontal Offset in Inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_HorizontalOffset;
        [Category("Phys Const")]
        [Description("HorizontalOffset")]
        public double HorizontalOffset
        {
           get { return m_HorizontalOffset; }
           set { m_HorizontalOffset = value; }
        }



        //   Attr Name:   StopAtTap
        //   Attr Group:Phys Const
        //   Alt Display Name:Stop at Tap
        //   Description:   Stop at Tap
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private bool m_StopAtTap;
        [Category("Phys Const")]
        [Description("StopAtTap")]
        public bool StopAtTap
        {
           get { return m_StopAtTap; }
           set { m_StopAtTap = value; }
        }



        //   Attr Name:   HasInlineBox
        //   Attr Group:Junction
        //   Alt Display Name:Inline Junction
        //   Description:   Flag set to indicate whther or not a span has an inline junction box on it
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private bool m_HasInlineBox;
        [Category("Junction")]
        [Description("HasInlineBox")]
        public bool HasInlineBox
        {
           get { return m_HasInlineBox; }
           set { m_HasInlineBox = value; }
        }



        //   Attr Name:   BoxOffset
        //   Attr Group:Junction
        //   Alt Display Name:Junc Box Offset (in)
        //   Description:   The distance down a line where a junction box is located
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   15.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BoxOffset;
        [Category("Junction")]
        [Description("BoxOffset")]
        public double BoxOffset
        {
           get { return m_BoxOffset; }
           set { m_BoxOffset = value; }
        }



        //   Attr Name:   BoxLength
        //   Attr Group:Junction
        //   Alt Display Name:Junc Box Len (in)
        //   Description:   Junction box length in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   20.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BoxLength;
        [Category("Junction")]
        [Description("BoxLength")]
        public double BoxLength
        {
           get { return m_BoxLength; }
           set { m_BoxLength = value; }
        }



        //   Attr Name:   BoxDiameter
        //   Attr Group:Junction
        //   Alt Display Name:Junc Box Diam (in)
        //   Description:   Junction box diameter in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   8.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BoxDiameter;
        [Category("Junction")]
        [Description("BoxDiameter")]
        public double BoxDiameter
        {
           get { return m_BoxDiameter; }
           set { m_BoxDiameter = value; }
        }



        //   Attr Name:   BoxWeight
        //   Attr Group:Junction
        //   Alt Display Name:Junc Box Weight (lbs)
        //   Description:   Junction box weight in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   5.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_BoxWeight;
        [Category("Junction")]
        [Description("BoxWeight")]
        public double BoxWeight
        {
           get { return m_BoxWeight; }
           set { m_BoxWeight = value; }
        }



        //   Attr Name:   HasDripLoop
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop
        //   Description:   Flag set to indicate whther or not a span has a drip loop on it
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private bool m_HasDripLoop;
        [Category("DripLoop")]
        [Description("HasDripLoop")]
        public bool HasDripLoop
        {
           get { return m_HasDripLoop; }
           set { m_HasDripLoop = value; }
        }



        //   Attr Name:   DripLoopOffset
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop Offset (in)
        //   Description:   The distance down a line where a drip loop is located
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   2.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_DripLoopOffset;
        [Category("DripLoop")]
        [Description("DripLoopOffset")]
        public double DripLoopOffset
        {
           get { return m_DripLoopOffset; }
           set { m_DripLoopOffset = value; }
        }



        //   Attr Name:   DripLoopLength
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop Len (in)
        //   Description:   Drip loop length in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   20.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_DripLoopLength;
        [Category("DripLoop")]
        [Description("DripLoopLength")]
        public double DripLoopLength
        {
           get { return m_DripLoopLength; }
           set { m_DripLoopLength = value; }
        }



        //   Attr Name:   DripLoopHeight
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop Height (in)
        //   Description:   Drip loop height in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   10.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_DripLoopHeight;
        [Category("DripLoop")]
        [Description("DripLoopHeight")]
        public double DripLoopHeight
        {
           get { return m_DripLoopHeight; }
           set { m_DripLoopHeight = value; }
        }



        //   Attr Name:   Modifier
        //   Attr Group:Standard
        //   Description:   Special type modifier.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   None
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Overlashed  (Overlashed)
        //        Bundled  (Bundled)
        //        Corrugated  (Corrugated)
        //        Flexpipe  (Flexpipe)
        //        Irregular  (Irregular)
        //        None  (None)
        //        (See Note)  ((See Note))
        public enum Modifier_val
        {
           [Description("Drop")]
           Drop,    //Drop
           [Description("Overlashed")]
           Overlashed,    //Overlashed
           [Description("Bundled")]
           Bundled,    //Bundled
           [Description("Corrugated")]
           Corrugated,    //Corrugated
           [Description("Flexpipe")]
           Flexpipe,    //Flexpipe
           [Description("Irregular")]
           Irregular,    //Irregular
           [Description("None")]
           None,    //None
           [Description("(See Note)")]
           _See_Note_     //(See Note)
        }
        private Modifier_val m_Modifier;
        [Category("Standard")]
        [Description("Modifier")]
        public Modifier_val Modifier
        {
           get
           { return m_Modifier; }
           set
           { m_Modifier = value; }
        }

        public Modifier_val String_to_Modifier_val(string pKey)
        {
           switch (pKey)
           {
                case "Drop":
                   return Modifier_val.Drop;    //Drop
                case "Overlashed":
                   return Modifier_val.Overlashed;    //Overlashed
                case "Bundled":
                   return Modifier_val.Bundled;    //Bundled
                case "Corrugated":
                   return Modifier_val.Corrugated;    //Corrugated
                case "Flexpipe":
                   return Modifier_val.Flexpipe;    //Flexpipe
                case "Irregular":
                   return Modifier_val.Irregular;    //Irregular
                case "None":
                   return Modifier_val.None;    //None
                case "(See Note)":
                   return Modifier_val._See_Note_;    //(See Note)
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Modifier_val_to_String(Modifier_val pKey)
        {
           switch (pKey)
           {
                case Modifier_val.Drop:
                   return "Drop";    //Drop
                case Modifier_val.Overlashed:
                   return "Overlashed";    //Overlashed
                case Modifier_val.Bundled:
                   return "Bundled";    //Bundled
                case Modifier_val.Corrugated:
                   return "Corrugated";    //Corrugated
                case Modifier_val.Flexpipe:
                   return "Flexpipe";    //Flexpipe
                case Modifier_val.Irregular:
                   return "Irregular";    //Irregular
                case Modifier_val.None:
                   return "None";    //None
                case Modifier_val._See_Note_:
                   return "(See Note)";    //(See Note)
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: SpanBundle
   // Mirrors: PPLSpanBundle : PPLElement
   //--------------------------------------------------------------------------------------------
   public class SpanBundle : ElementBase
   {

      public static string gXMLkey = "SpanBundle";
      public override string XMLkey() { return gXMLkey; }

      public SpanBundle(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_CoordinateX = 0;
               m_CoordinateZ = 0;
               m_Owner = "<Undefined>";
               m_Type = "Bundle";
               m_Construction = Construction_val.Overlashed;
               m_BundleIceMode = BundleIceMode_val.Individual;
               m_BundleWindMode = BundleWindMode_val.Individual;
               m_CoordinateA = 0;
               m_SpanDistanceInInches = 600;
               m_SpanEndHeightDelta = 0;
               m_MidspanDeflection = 0;
               m_Tension_Type = Tension_Type_val.Static;
               m_Tension = 0;
               m_TensionTable = new ValTable("Tension;0,500;");
               m_SlackTension = 10;
               m_RatedStrength = 5000;
               m_ConductorDiameter = 0.5;
               m_OverrideTemp = false;
               m_Temperature = 65;
               m_TempMin = 32;
               m_TempMax = 212;
               m_PoundsPerInch = 0.0076;
               m_ModulusOfElasticity = 11200000;
               m_PercentSolid = 0.75;
               m_ThermalCoefficient = 1.06E-05;
               m_CreepCoefficient = 0;
               m_IceAccumulationFactor = 0.75;
               m_WindTensionFactor = -1;
               m_WindDragCoef = 0;
               m_VerticalOffset = 0;
               m_HorizontalOffset = 0;
               m_StopAtTap = false;
               m_HasInlineBox = false;
               m_BoxOffset = 15;
               m_BoxLength = 20;
               m_BoxDiameter = 8;
               m_BoxWeight = 5;
               m_HasDripLoop = false;
               m_DripLoopOffset = 2;
               m_DripLoopLength = 20;
               m_DripLoopHeight = 10;
               m_Modifier = Modifier_val.None;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Span) return true;
         if(pChildCandidate is Clearance) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Description:   The X coordinate relative to parent.  SnapToParent will set the value
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Description:   The Z coordinate relative to parent.  SnapToParent will set the value
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Alt Display Name:Description
        //   Description:   The name, type, material, code, or other designation assigned to this span type by the owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Bundle
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Type;
        [Category("Standard")]
        [Description("Type")]
        public string Type
        {
           get { return m_Type; }
           set { m_Type = value; }
        }



        //   Attr Name:   Construction
        //   Attr Group:Standard
        //   Alt Display Name:Bundle Type
        //   Description:   Bundle Construction
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Overlashed
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Spacers  (Spacers)
        //        Bonded  (Bonded)
        //        Twist/Braid  (Twist/Braid)
        //        Wrapped  (Wrapped)
        //        Other  (Other)
        public enum Construction_val
        {
           [Description("Overlashed")]
           Overlashed,    //Overlashed
           [Description("Spacers")]
           Spacers,    //Spacers
           [Description("Bonded")]
           Bonded,    //Bonded
           [Description("Twist/Braid")]
           Twist_Braid,    //Twist/Braid
           [Description("Wrapped")]
           Wrapped,    //Wrapped
           [Description("Other")]
           Other     //Other
        }
        private Construction_val m_Construction;
        [Category("Standard")]
        [Description("Construction")]
        public Construction_val Construction
        {
           get
           { return m_Construction; }
           set
           { m_Construction = value; }
        }

        public Construction_val String_to_Construction_val(string pKey)
        {
           switch (pKey)
           {
                case "Overlashed":
                   return Construction_val.Overlashed;    //Overlashed
                case "Spacers":
                   return Construction_val.Spacers;    //Spacers
                case "Bonded":
                   return Construction_val.Bonded;    //Bonded
                case "Twist/Braid":
                   return Construction_val.Twist_Braid;    //Twist/Braid
                case "Wrapped":
                   return Construction_val.Wrapped;    //Wrapped
                case "Other":
                   return Construction_val.Other;    //Other
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Construction_val_to_String(Construction_val pKey)
        {
           switch (pKey)
           {
                case Construction_val.Overlashed:
                   return "Overlashed";    //Overlashed
                case Construction_val.Spacers:
                   return "Spacers";    //Spacers
                case Construction_val.Bonded:
                   return "Bonded";    //Bonded
                case Construction_val.Twist_Braid:
                   return "Twist/Braid";    //Twist/Braid
                case Construction_val.Wrapped:
                   return "Wrapped";    //Wrapped
                case Construction_val.Other:
                   return "Other";    //Other
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   BundleIceMode
        //   Attr Group:Bundle Ice/Wind
        //   Alt Display Name:Bundle Ice Mode
        //   Description:   Geometry for ice accumulation
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Individual
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Min Circle  (Min Circle)
        //        Convex Hull  (Convex Hull)
        //        Concave Hull  (Concave Hull)
        public enum BundleIceMode_val
        {
           [Description("Individual")]
           Individual,    //Individual
           [Description("Min Circle")]
           Min_Circle,    //Min Circle
           [Description("Convex Hull")]
           Convex_Hull,    //Convex Hull
           [Description("Concave Hull")]
           Concave_Hull     //Concave Hull
        }
        private BundleIceMode_val m_BundleIceMode;
        [Category("Bundle Ice/Wind")]
        [Description("BundleIceMode")]
        public BundleIceMode_val BundleIceMode
        {
           get
           { return m_BundleIceMode; }
           set
           { m_BundleIceMode = value; }
        }

        public BundleIceMode_val String_to_BundleIceMode_val(string pKey)
        {
           switch (pKey)
           {
                case "Individual":
                   return BundleIceMode_val.Individual;    //Individual
                case "Min Circle":
                   return BundleIceMode_val.Min_Circle;    //Min Circle
                case "Convex Hull":
                   return BundleIceMode_val.Convex_Hull;    //Convex Hull
                case "Concave Hull":
                   return BundleIceMode_val.Concave_Hull;    //Concave Hull
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string BundleIceMode_val_to_String(BundleIceMode_val pKey)
        {
           switch (pKey)
           {
                case BundleIceMode_val.Individual:
                   return "Individual";    //Individual
                case BundleIceMode_val.Min_Circle:
                   return "Min Circle";    //Min Circle
                case BundleIceMode_val.Convex_Hull:
                   return "Convex Hull";    //Convex Hull
                case BundleIceMode_val.Concave_Hull:
                   return "Concave Hull";    //Concave Hull
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   BundleWindMode
        //   Attr Group:Bundle Ice/Wind
        //   Alt Display Name:Bundle Wind Mode
        //   Description:   Geometry for wind area
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Individual
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Min Circle  (Min Circle)
        //        Convex Hull  (Convex Hull)
        //        Concave Hull  (Concave Hull)
        public enum BundleWindMode_val
        {
           [Description("Individual")]
           Individual,    //Individual
           [Description("Min Circle")]
           Min_Circle,    //Min Circle
           [Description("Convex Hull")]
           Convex_Hull,    //Convex Hull
           [Description("Concave Hull")]
           Concave_Hull     //Concave Hull
        }
        private BundleWindMode_val m_BundleWindMode;
        [Category("Bundle Ice/Wind")]
        [Description("BundleWindMode")]
        public BundleWindMode_val BundleWindMode
        {
           get
           { return m_BundleWindMode; }
           set
           { m_BundleWindMode = value; }
        }

        public BundleWindMode_val String_to_BundleWindMode_val(string pKey)
        {
           switch (pKey)
           {
                case "Individual":
                   return BundleWindMode_val.Individual;    //Individual
                case "Min Circle":
                   return BundleWindMode_val.Min_Circle;    //Min Circle
                case "Convex Hull":
                   return BundleWindMode_val.Convex_Hull;    //Convex Hull
                case "Concave Hull":
                   return BundleWindMode_val.Concave_Hull;    //Concave Hull
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string BundleWindMode_val_to_String(BundleWindMode_val pKey)
        {
           switch (pKey)
           {
                case BundleWindMode_val.Individual:
                   return "Individual";    //Individual
                case BundleWindMode_val.Min_Circle:
                   return "Min Circle";    //Min Circle
                case BundleWindMode_val.Convex_Hull:
                   return "Convex Hull";    //Convex Hull
                case BundleWindMode_val.Concave_Hull:
                   return "Concave Hull";    //Concave Hull
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The relative angle between this span its proximal connection at it's parent insulator or other holding structure
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   SpanDistanceInInches
        //   Attr Group:Standard
        //   Alt Display Name:Span Length (ft)
        //   Description:   The horizontal component of the distance between the proximal and distal ends of the span
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   600.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_SpanDistanceInInches;
        [Category("Standard")]
        [Description("SpanDistanceInInches")]
        public double SpanDistanceInInches
        {
           get { return m_SpanDistanceInInches; }
           set { m_SpanDistanceInInches = value; }
        }



        //   Attr Name:   SpanEndHeightDelta
        //   Attr Group:Standard
        //   Alt Display Name:End Drop/Rise (ft)
        //   Description:   The vertical component of the distance between the proximal and distal ends of the span
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_SpanEndHeightDelta;
        [Category("Standard")]
        [Description("SpanEndHeightDelta")]
        public double SpanEndHeightDelta
        {
           get { return m_SpanEndHeightDelta; }
           set { m_SpanEndHeightDelta = value; }
        }



        //   Attr Name:   MidspanDeflection
        //   Attr Group:Tension Sag
        //   Alt Display Name:Span Sag (ft)
        //   Description:   The vertical deflection between the proximal end of the span an the maximum deflection point of the catenary curve
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_MidspanDeflection;
        [Category("Tension Sag")]
        [Description("MidspanDeflection")]
        public double MidspanDeflection
        {
           get { return m_MidspanDeflection; }
           set { m_MidspanDeflection = value; }
        }



        //   Attr Name:   Tension Type
        //   Attr Group:Tension Sag
        //   Description:   Is the tension value calculated or static
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Static
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Slack  (The tension is a static slack constant supplied by the operator or other data source)
        //        Table  (The tension is a table supplied by the operator or other data source)
        //        Sag to Tension  (The tension is calculated based on the values entered for sag, weight, and LoadCase)
        //        Tension to Sag  (The tension is based on initial stringing tension and LoadCase)
        public enum Tension_Type_val
        {
           [Description("Static")]
           Static,    //The tension is a static normal constant supplied by the operator or other data source
           [Description("Slack")]
           Slack,    //The tension is a static slack constant supplied by the operator or other data source
           [Description("Table")]
           Table,    //The tension is a table supplied by the operator or other data source
           [Description("Sag to Tension")]
           Sag_to_Tension,    //The tension is calculated based on the values entered for sag, weight, and LoadCase
           [Description("Tension to Sag")]
           Tension_to_Sag     //The tension is based on initial stringing tension and LoadCase
        }
        private Tension_Type_val m_Tension_Type;
        [Category("Tension Sag")]
        [Description("Tension Type")]
        public Tension_Type_val Tension_Type
        {
           get
           { return m_Tension_Type; }
           set
           { m_Tension_Type = value; }
        }

        public Tension_Type_val String_to_Tension_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Static":
                   return Tension_Type_val.Static;    //The tension is a static normal constant supplied by the operator or other data source
                case "Slack":
                   return Tension_Type_val.Slack;    //The tension is a static slack constant supplied by the operator or other data source
                case "Table":
                   return Tension_Type_val.Table;    //The tension is a table supplied by the operator or other data source
                case "Sag to Tension":
                   return Tension_Type_val.Sag_to_Tension;    //The tension is calculated based on the values entered for sag, weight, and LoadCase
                case "Tension to Sag":
                   return Tension_Type_val.Tension_to_Sag;    //The tension is based on initial stringing tension and LoadCase
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Tension_Type_val_to_String(Tension_Type_val pKey)
        {
           switch (pKey)
           {
                case Tension_Type_val.Static:
                   return "Static";    //The tension is a static normal constant supplied by the operator or other data source
                case Tension_Type_val.Slack:
                   return "Slack";    //The tension is a static slack constant supplied by the operator or other data source
                case Tension_Type_val.Table:
                   return "Table";    //The tension is a table supplied by the operator or other data source
                case Tension_Type_val.Sag_to_Tension:
                   return "Sag to Tension";    //The tension is calculated based on the values entered for sag, weight, and LoadCase
                case Tension_Type_val.Tension_to_Sag:
                   return "Tension to Sag";    //The tension is based on initial stringing tension and LoadCase
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Tension
        //   Attr Group:Tension Sag
        //   Alt Display Name:Tension (lbs)
        //   Description:   The tension value used only when "Tension Type" is "Static"
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Tension;
        [Category("Tension Sag")]
        [Description("Tension")]
        public double Tension
        {
           get { return m_Tension; }
           set { m_Tension = value; }
        }



        //   Attr Name:   TensionTable
        //   Attr Group:Tension Sag
        //   Alt Display Name:Tension Table
        //   Description:   The tension values table used only when "Tension Type" is "Table"
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   TENSION_TABLE
        //   Default Value:   Tension;0,500;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_TensionTable = new ValTable();
        [Category("Tension Sag")]
        [Description("TensionTable")]
        public ValTable TensionTable
        {
           get { return m_TensionTable; }
           set { m_TensionTable = value; }
        }



        //   Attr Name:   SlackTension
        //   Attr Group:Tension Sag
        //   Alt Display Name:Slack Tension (lbs)
        //   Description:   The tension value used only when "Tension Type" is "Slack"
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   10.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SlackTension;
        [Category("Tension Sag")]
        [Description("SlackTension")]
        public double SlackTension
        {
           get { return m_SlackTension; }
           set { m_SlackTension = value; }
        }



        //   Attr Name:   RatedStrength
        //   Attr Group:Tension Sag
        //   Alt Display Name:Msgr Rated Strength (lbs)
        //   Description:   The rated strength in pounds.
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   5000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RatedStrength;
        [Category("Tension Sag")]
        [Description("RatedStrength")]
        public double RatedStrength
        {
           get { return m_RatedStrength; }
           set { m_RatedStrength = value; }
        }



        //   Attr Name:   ConductorDiameter
        //   Attr Group:Standard
        //   Alt Display Name:Msgr Diam (in)
        //   Description:   The conductor diameter in inches including the insulation.
        //   Displayed Units:   store as INCHES display as INCHES or MILLIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ConductorDiameter;
        [Category("Standard")]
        [Description("ConductorDiameter")]
        public double ConductorDiameter
        {
           get { return m_ConductorDiameter; }
           set { m_ConductorDiameter = value; }
        }



        //   Attr Name:   OverrideTemp
        //   Attr Group:Temperature
        //   Alt Display Name:Override Temp
        //   Description:   Override Nominal Temperature
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverrideTemp;
        [Category("Temperature")]
        [Description("OverrideTemp")]
        public bool OverrideTemp
        {
           get { return m_OverrideTemp; }
           set { m_OverrideTemp = value; }
        }



        //   Attr Name:   Temperature
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Nom (°f)
        //   Description:   Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   65
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Temperature;
        [Category("Temperature")]
        [Description("Temperature")]
        public double Temperature
        {
           get { return m_Temperature; }
           set { m_Temperature = value; }
        }



        //   Attr Name:   TempMin
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Min (°f)
        //   Description:   Minimum Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   32
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TempMin;
        [Category("Temperature")]
        [Description("TempMin")]
        public double TempMin
        {
           get { return m_TempMin; }
           set { m_TempMin = value; }
        }



        //   Attr Name:   TempMax
        //   Attr Group:Temperature
        //   Alt Display Name:Temp Max (°f)
        //   Description:   Maximum Temperature
        //   Displayed Units:   store as FAHRENHEIT display as FAHRENHEIT or CELSIUS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   212
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TempMax;
        [Category("Temperature")]
        [Description("TempMax")]
        public double TempMax
        {
           get { return m_TempMax; }
           set { m_TempMax = value; }
        }



        //   Attr Name:   PoundsPerInch
        //   Attr Group:Phys Const
        //   Alt Display Name:Msgr Span Weight (lbs/ft)
        //   Description:   The weight per unit of running length
        //   Displayed Units:   store as POUNDS PER IN display as POUNDS PER FT or KILOGRAMS PER METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0076
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoundsPerInch;
        [Category("Phys Const")]
        [Description("PoundsPerInch")]
        public double PoundsPerInch
        {
           get { return m_PoundsPerInch; }
           set { m_PoundsPerInch = value; }
        }



        //   Attr Name:   ModulusOfElasticity
        //   Attr Group:Phys Const
        //   Alt Display Name:Msgr Modulus of Elasticity (psi)
        //   Description:   ModulusOfElasticity
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   11200000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ModulusOfElasticity;
        [Category("Phys Const")]
        [Description("ModulusOfElasticity")]
        public double ModulusOfElasticity
        {
           get { return m_ModulusOfElasticity; }
           set { m_ModulusOfElasticity = value; }
        }



        //   Attr Name:   PercentSolid
        //   Attr Group:Phys Const
        //   Alt Display Name:Msgr Percent Solid
        //   Description:   Percent Solid
        //   Displayed Units:   store as PERCENT 0 TO 1 display as PERCENT 0 TO 100
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   0.75
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PercentSolid;
        [Category("Phys Const")]
        [Description("PercentSolid")]
        public double PercentSolid
        {
           get { return m_PercentSolid; }
           set { m_PercentSolid = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Phys Const
        //   Alt Display Name:Msgr Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000106
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Phys Const")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   CreepCoefficient
        //   Attr Group:Phys Const
        //   Alt Display Name:Msgr Creep Coef ((in/in)/lb)
        //   Description:   CreepCoefficient
        //   Displayed Units:   store as CREEP COEFFICIENT
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_CreepCoefficient;
        [Category("Phys Const")]
        [Description("CreepCoefficient")]
        public double CreepCoefficient
        {
           get { return m_CreepCoefficient; }
           set { m_CreepCoefficient = value; }
        }



        //   Attr Name:   IceAccumulationFactor
        //   Attr Group:Tension Sag
        //   Alt Display Name:Ice Accum. Factor
        //   Description:   Ice Accumulation Factor for Tension Calculations
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###
        //   Attribute Type:   FLOAT
        //   Default Value:   0.75
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_IceAccumulationFactor;
        [Category("Tension Sag")]
        [Description("IceAccumulationFactor")]
        public double IceAccumulationFactor
        {
           get { return m_IceAccumulationFactor; }
           set { m_IceAccumulationFactor = value; }
        }



        //   Attr Name:   WindTensionFactor
        //   Attr Group:Tension Sag
        //   Description:   Wind Factor for Tension Calculations
        //   Displayed Units:   INVERTED
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   -1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindTensionFactor;
        [Category("Tension Sag")]
        [Description("WindTensionFactor")]
        public double WindTensionFactor
        {
           get { return m_WindTensionFactor; }
           set { m_WindTensionFactor = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Tension Sag
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Tension Sag")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   VerticalOffset
        //   Attr Group:Phys Const
        //   Alt Display Name:Vertical Offset (in)
        //   Description:   Vertical Offset in Inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_VerticalOffset;
        [Category("Phys Const")]
        [Description("VerticalOffset")]
        public double VerticalOffset
        {
           get { return m_VerticalOffset; }
           set { m_VerticalOffset = value; }
        }



        //   Attr Name:   HorizontalOffset
        //   Attr Group:Phys Const
        //   Alt Display Name:Horizontal Offset (in)
        //   Description:   Horizontal Offset in Inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_HorizontalOffset;
        [Category("Phys Const")]
        [Description("HorizontalOffset")]
        public double HorizontalOffset
        {
           get { return m_HorizontalOffset; }
           set { m_HorizontalOffset = value; }
        }



        //   Attr Name:   StopAtTap
        //   Attr Group:Phys Const
        //   Alt Display Name:Stop at Tap
        //   Description:   Stop at Tap
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private bool m_StopAtTap;
        [Category("Phys Const")]
        [Description("StopAtTap")]
        public bool StopAtTap
        {
           get { return m_StopAtTap; }
           set { m_StopAtTap = value; }
        }



        //   Attr Name:   HasInlineBox
        //   Attr Group:Standard
        //   Alt Display Name:Inline Junction
        //   Description:   Flag set to indicate whther or not a span has an inline junction box on it
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private bool m_HasInlineBox;
        [Category("Standard")]
        [Description("HasInlineBox")]
        public bool HasInlineBox
        {
           get { return m_HasInlineBox; }
           set { m_HasInlineBox = value; }
        }



        //   Attr Name:   BoxOffset
        //   Attr Group:Standard
        //   Alt Display Name:Junc Box Offset (in)
        //   Description:   The distance down a line where a junction box is located
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   15.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_BoxOffset;
        [Category("Standard")]
        [Description("BoxOffset")]
        public double BoxOffset
        {
           get { return m_BoxOffset; }
           set { m_BoxOffset = value; }
        }



        //   Attr Name:   BoxLength
        //   Attr Group:Standard
        //   Alt Display Name:Junc Box Len (in)
        //   Description:   Junction box length in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   20.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_BoxLength;
        [Category("Standard")]
        [Description("BoxLength")]
        public double BoxLength
        {
           get { return m_BoxLength; }
           set { m_BoxLength = value; }
        }



        //   Attr Name:   BoxDiameter
        //   Attr Group:Standard
        //   Alt Display Name:Junc Box Diam (in)
        //   Description:   Junction box diameter in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   8.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_BoxDiameter;
        [Category("Standard")]
        [Description("BoxDiameter")]
        public double BoxDiameter
        {
           get { return m_BoxDiameter; }
           set { m_BoxDiameter = value; }
        }



        //   Attr Name:   BoxWeight
        //   Attr Group:Standard
        //   Alt Display Name:Junc Box Weight (lbs)
        //   Description:   Junction box weight in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   5.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_BoxWeight;
        [Category("Standard")]
        [Description("BoxWeight")]
        public double BoxWeight
        {
           get { return m_BoxWeight; }
           set { m_BoxWeight = value; }
        }



        //   Attr Name:   HasDripLoop
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop
        //   Description:   Flag set to indicate whther or not a span has a drip loop on it
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private bool m_HasDripLoop;
        [Category("DripLoop")]
        [Description("HasDripLoop")]
        public bool HasDripLoop
        {
           get { return m_HasDripLoop; }
           set { m_HasDripLoop = value; }
        }



        //   Attr Name:   DripLoopOffset
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop Offset (in)
        //   Description:   The distance down a line where a drip loop is located
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   2.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_DripLoopOffset;
        [Category("DripLoop")]
        [Description("DripLoopOffset")]
        public double DripLoopOffset
        {
           get { return m_DripLoopOffset; }
           set { m_DripLoopOffset = value; }
        }



        //   Attr Name:   DripLoopLength
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop Len (in)
        //   Description:   Drip loop length in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   20.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_DripLoopLength;
        [Category("DripLoop")]
        [Description("DripLoopLength")]
        public double DripLoopLength
        {
           get { return m_DripLoopLength; }
           set { m_DripLoopLength = value; }
        }



        //   Attr Name:   DripLoopHeight
        //   Attr Group:DripLoop
        //   Alt Display Name:Drip Loop Height (in)
        //   Description:   Drip loop height in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   10.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_DripLoopHeight;
        [Category("DripLoop")]
        [Description("DripLoopHeight")]
        public double DripLoopHeight
        {
           get { return m_DripLoopHeight; }
           set { m_DripLoopHeight = value; }
        }



        //   Attr Name:   Modifier
        //   Attr Group:Standard
        //   Description:   Special type modifier.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   None
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Overlashed  (Overlashed)
        //        Bundled  (Bundled)
        //        Corrugated  (Corrugated)
        //        Flexpipe  (Flexpipe)
        //        Irregular  (Irregular)
        //        None  (None)
        //        (See Note)  ((See Note))
        public enum Modifier_val
        {
           [Description("Drop")]
           Drop,    //Drop
           [Description("Overlashed")]
           Overlashed,    //Overlashed
           [Description("Bundled")]
           Bundled,    //Bundled
           [Description("Corrugated")]
           Corrugated,    //Corrugated
           [Description("Flexpipe")]
           Flexpipe,    //Flexpipe
           [Description("Irregular")]
           Irregular,    //Irregular
           [Description("None")]
           None,    //None
           [Description("(See Note)")]
           _See_Note_     //(See Note)
        }
        private Modifier_val m_Modifier;
        [Category("Standard")]
        [Description("Modifier")]
        public Modifier_val Modifier
        {
           get
           { return m_Modifier; }
           set
           { m_Modifier = value; }
        }

        public Modifier_val String_to_Modifier_val(string pKey)
        {
           switch (pKey)
           {
                case "Drop":
                   return Modifier_val.Drop;    //Drop
                case "Overlashed":
                   return Modifier_val.Overlashed;    //Overlashed
                case "Bundled":
                   return Modifier_val.Bundled;    //Bundled
                case "Corrugated":
                   return Modifier_val.Corrugated;    //Corrugated
                case "Flexpipe":
                   return Modifier_val.Flexpipe;    //Flexpipe
                case "Irregular":
                   return Modifier_val.Irregular;    //Irregular
                case "None":
                   return Modifier_val.None;    //None
                case "(See Note)":
                   return Modifier_val._See_Note_;    //(See Note)
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Modifier_val_to_String(Modifier_val pKey)
        {
           switch (pKey)
           {
                case Modifier_val.Drop:
                   return "Drop";    //Drop
                case Modifier_val.Overlashed:
                   return "Overlashed";    //Overlashed
                case Modifier_val.Bundled:
                   return "Bundled";    //Bundled
                case Modifier_val.Corrugated:
                   return "Corrugated";    //Corrugated
                case Modifier_val.Flexpipe:
                   return "Flexpipe";    //Flexpipe
                case Modifier_val.Irregular:
                   return "Irregular";    //Irregular
                case Modifier_val.None:
                   return "None";    //None
                case Modifier_val._See_Note_:
                   return "(See Note)";    //(See Note)
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Tap
   // Mirrors: PPLTap : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Tap : ElementBase
   {

      public static string gXMLkey = "Tap";
      public override string XMLkey() { return gXMLkey; }

      public Tap(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Flying Tap";
               m_Owner = "<Undefined>";
               m_CoordinateA = 0;
               m_OffsetInches = 60;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Span) return true;
         if(pChildCandidate is SpanBundle) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the tap
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Flying Tap
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Base Angle (°)
        //   Description:   Angle
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   OffsetInches
        //   Attr Group:Standard
        //   Alt Display Name:Offset (ft)
        //   Description:   Offset from start of pole
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERX
        //   Default Value:   60
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OffsetInches;
        [Category("Standard")]
        [Description("OffsetInches")]
        public double OffsetInches
        {
           get { return m_OffsetInches; }
           set { m_OffsetInches = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: PowerEquipment
   // Mirrors: PPLTransformer : PPLElement
   //--------------------------------------------------------------------------------------------
   public class PowerEquipment : ElementBase
   {

      public static string gXMLkey = "PowerEquipment";
      public override string XMLkey() { return gXMLkey; }

      public PowerEquipment(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_CoordinateX = 10;
               m_Description = "1PH- 25KVA";
               m_Owner = "<Undefined>";
               m_CoordinateZ = 420;
               m_CoordinateA = 0;
               m_Pole_Gap = 6;
               m_Type = Type_val.Transformer;
               m_Mount = Mount_val.Pole;
               m_Qty = 1;
               m_Array_Angle = 1.5707963267949;
               m_Rack_Spacing = 0;
               m_DiameterInInches = 22;
               m_HeightInInches = 39;
               m_DepthInInches = 39;
               m_Weight = 365;
               m_WindDragCoef = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Alt Display Name:Pole Radius at Height (in)
        //   Description:   Internal attribute which holds the radius of the pole at the given height.
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   10
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description or Transformer Catalog name
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   1PH- 25KVA
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Install Height (ft)
        //   Description:   The distance in inches from the butt of the pole to the center of the transformer can
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   420
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   Angle of bank relative to the parent structure (typically pole)
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   Pole Gap
        //   Attr Group:Standard
        //   Alt Display Name:Gap (in)
        //   Description:   Distance between pole and can in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   6
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Pole_Gap;
        [Category("Standard")]
        [Description("Pole Gap")]
        public double Pole_Gap
        {
           get { return m_Pole_Gap; }
           set { m_Pole_Gap = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   Equipment Type
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Transformer
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Regulator  (Voltage Regulator)
        //        Capacitor  (Capacitor)
        //        Switch  (Switch)
        //        Fuse  (Switch)
        //        Box  (Misc Box)
        public enum Type_val
        {
           [Description("Transformer")]
           Transformer,    //Transformer
           [Description("Regulator")]
           Regulator,    //Voltage Regulator
           [Description("Capacitor")]
           Capacitor,    //Capacitor
           [Description("Switch")]
           Switch,    //Switch
           [Description("Fuse")]
           Fuse,    //Switch
           [Description("Box")]
           Box     //Misc Box
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Transformer":
                   return Type_val.Transformer;    //Transformer
                case "Regulator":
                   return Type_val.Regulator;    //Voltage Regulator
                case "Capacitor":
                   return Type_val.Capacitor;    //Capacitor
                case "Switch":
                   return Type_val.Switch;    //Switch
                case "Fuse":
                   return Type_val.Fuse;    //Switch
                case "Box":
                   return Type_val.Box;    //Misc Box
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Transformer:
                   return "Transformer";    //Transformer
                case Type_val.Regulator:
                   return "Regulator";    //Voltage Regulator
                case Type_val.Capacitor:
                   return "Capacitor";    //Capacitor
                case Type_val.Switch:
                   return "Switch";    //Switch
                case Type_val.Fuse:
                   return "Fuse";    //Switch
                case Type_val.Box:
                   return "Box";    //Misc Box
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Mount
        //   Attr Group:Standard
        //   Description:   The type of mount used to install the equipment
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Rack  (Equipment are mounted on a rack which is mounted to the pole)
        public enum Mount_val
        {
           [Description("Pole")]
           Pole,    //Standard pole bank mount
           [Description("Rack")]
           Rack     //Equipment are mounted on a rack which is mounted to the pole
        }
        private Mount_val m_Mount;
        [Category("Standard")]
        [Description("Mount")]
        public Mount_val Mount
        {
           get
           { return m_Mount; }
           set
           { m_Mount = value; }
        }

        public Mount_val String_to_Mount_val(string pKey)
        {
           switch (pKey)
           {
                case "Pole":
                   return Mount_val.Pole;    //Standard pole bank mount
                case "Rack":
                   return Mount_val.Rack;    //Equipment are mounted on a rack which is mounted to the pole
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Mount_val_to_String(Mount_val pKey)
        {
           switch (pKey)
           {
                case Mount_val.Pole:
                   return "Pole";    //Standard pole bank mount
                case Mount_val.Rack:
                   return "Rack";    //Equipment are mounted on a rack which is mounted to the pole
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Qty
        //   Attr Group:Standard
        //   Alt Display Name:Unit Count
        //   Description:   Number of units
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   PLUSMINUS
        //   Default Value:   1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private int m_Qty;
        [Category("Standard")]
        [Description("Qty")]
        public int Qty
        {
           get { return m_Qty; }
           set { m_Qty = value; }
        }



        //   Attr Name:   Array Angle
        //   Attr Group:Standard
        //   Alt Display Name:Unit Spacing (°)
        //   Description:   Angle between cans
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   1.5707963267949
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Array_Angle;
        [Category("Standard")]
        [Description("Array Angle")]
        public double Array_Angle
        {
           get { return m_Array_Angle; }
           set { m_Array_Angle = value; }
        }



        //   Attr Name:   Rack Spacing
        //   Attr Group:Standard
        //   Alt Display Name:Rack Spacing (in)
        //   Description:   The distance between items on a rack
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Rack_Spacing;
        [Category("Standard")]
        [Description("Rack Spacing")]
        public double Rack_Spacing
        {
           get { return m_Rack_Spacing; }
           set { m_Rack_Spacing = value; }
        }



        //   Attr Name:   DiameterInInches
        //   Attr Group:Standard
        //   Alt Display Name:Unit Diameter/Width (in)
        //   Description:   The Diameter of the transformer can in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   22
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DiameterInInches;
        [Category("Standard")]
        [Description("DiameterInInches")]
        public double DiameterInInches
        {
           get { return m_DiameterInInches; }
           set { m_DiameterInInches = value; }
        }



        //   Attr Name:   HeightInInches
        //   Attr Group:Standard
        //   Alt Display Name:Unit Height (in)
        //   Description:   The Height of the transformer can in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   39
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_HeightInInches;
        [Category("Standard")]
        [Description("HeightInInches")]
        public double HeightInInches
        {
           get { return m_HeightInInches; }
           set { m_HeightInInches = value; }
        }



        //   Attr Name:   DepthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Unit Depth (in)
        //   Description:   The Depth of the transformer can in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   39
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DepthInInches;
        [Category("Standard")]
        [Description("DepthInInches")]
        public double DepthInInches
        {
           get { return m_DepthInInches; }
           set { m_DepthInInches = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Standard
        //   Alt Display Name:Unit Weight (lbs)
        //   Description:   The weight of each individual transformer can in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   365.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Standard")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Standard
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Standard")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Streetlight
   // Mirrors: PPLStreetlight : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Streetlight : ElementBase
   {

      public static string gXMLkey = "Streetlight";
      public override string XMLkey() { return gXMLkey; }

      public Streetlight(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_CoordinateX = 0;
               m_Description = "Streetlight";
               m_Owner = "<Undefined>";
               m_Type = Type_val.General;
               m_CoordinateZ = 300;
               m_CoordinateA = 0;
               m_LengthInInches = 96;
               m_ArmDiameterInInches = 3;
               m_ArmRiseInInches = 25;
               m_CanDiameterInInches = 22;
               m_CanHeightInInches = 9;
               m_Weight = 40;
               m_WindDragCoef = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Description:   The horizontal position of the base plate relative to the center of the pole in inches
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of streetlight
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Streetlight
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   General type of light to model.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   General
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Decorative  (Decorative)
        //        Spot Light  (Spot Light)
        //        Flood Light  (Flood Light)
        //        Traffic Signal  (Traffic Signal)
        public enum Type_val
        {
           [Description("General")]
           General,    //General
           [Description("Decorative")]
           Decorative,    //Decorative
           [Description("Spot Light")]
           Spot_Light,    //Spot Light
           [Description("Flood Light")]
           Flood_Light,    //Flood Light
           [Description("Traffic Signal")]
           Traffic_Signal     //Traffic Signal
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "General":
                   return Type_val.General;    //General
                case "Decorative":
                   return Type_val.Decorative;    //Decorative
                case "Spot Light":
                   return Type_val.Spot_Light;    //Spot Light
                case "Flood Light":
                   return Type_val.Flood_Light;    //Flood Light
                case "Traffic Signal":
                   return Type_val.Traffic_Signal;    //Traffic Signal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.General:
                   return "General";    //General
                case Type_val.Decorative:
                   return "Decorative";    //Decorative
                case Type_val.Spot_Light:
                   return "Spot Light";    //Spot Light
                case Type_val.Flood_Light:
                   return "Flood Light";    //Flood Light
                case Type_val.Traffic_Signal:
                   return "Traffic Signal";    //Traffic Signal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Install Height (ft)
        //   Description:   The distance from the center of the base plate to the butt of the pole in inches
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   300.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The angle of the streetlight relative to the parent structure in radians
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Arm Length (in)
        //   Description:   The horizontal length from the face of the pole to the tip of the streetlight
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   96.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Standard")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   ArmDiameterInInches
        //   Attr Group:Standard
        //   Alt Display Name:Arm Diameter (in)
        //   Description:   The diameter or thickness of the arm of the streetlight assembly
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ArmDiameterInInches;
        [Category("Standard")]
        [Description("ArmDiameterInInches")]
        public double ArmDiameterInInches
        {
           get { return m_ArmDiameterInInches; }
           set { m_ArmDiameterInInches = value; }
        }



        //   Attr Name:   ArmRiseInInches
        //   Attr Group:Standard
        //   Alt Display Name:Arm Rise (in)
        //   Description:   The horizontal length from the face of the pole to the tip of the streetlight
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   25.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ArmRiseInInches;
        [Category("Standard")]
        [Description("ArmRiseInInches")]
        public double ArmRiseInInches
        {
           get { return m_ArmRiseInInches; }
           set { m_ArmRiseInInches = value; }
        }



        //   Attr Name:   CanDiameterInInches
        //   Attr Group:Standard
        //   Alt Display Name:Can Diameter (in)
        //   Description:   The diameter or thickness of the light fixture at the tip of the streetlight assempbly
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   22.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_CanDiameterInInches;
        [Category("Standard")]
        [Description("CanDiameterInInches")]
        public double CanDiameterInInches
        {
           get { return m_CanDiameterInInches; }
           set { m_CanDiameterInInches = value; }
        }



        //   Attr Name:   CanHeightInInches
        //   Attr Group:Standard
        //   Alt Display Name:Can Height (in)
        //   Description:   The height of the can of the light fixture
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   9.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_CanHeightInInches;
        [Category("Standard")]
        [Description("CanHeightInInches")]
        public double CanHeightInInches
        {
           get { return m_CanHeightInInches; }
           set { m_CanHeightInInches = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Standard
        //   Alt Display Name:Equip Weight(lbs)
        //   Description:   Weight of the streetlight in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   40.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Standard")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Standard
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Standard")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: GuyBrace
   // Mirrors: PPLGuyBrace : PPLElement
   //--------------------------------------------------------------------------------------------
   public class GuyBrace : ElementBase
   {

      public static string gXMLkey = "GuyBrace";
      public override string XMLkey() { return gXMLkey; }

      public GuyBrace(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "";
               m_Owner = "<Undefined>";
               m_Type = Type_val.Down;
               m_CoordinateZ = 453;
               m_StrutHeightInInches = 405;
               m_StrutLengthInInches = 48;
               m_StrutDiameter = 1.25;
               m_StrutWeightPerIn = 0.189167;
               m_StrutCapacity = 2500;
               m_StrutBendingMomentMax = 800;
               m_StrutFixity = StrutFixity_val.Pinned;
               m_MergeStruts = false;
               m_DeltaHeightInInches = 0;
               m_Diameter = 0.5;
               m_PercentSolid = 1;
               m_PreTension = 700;
               m_VerticalOffset = 0;
               m_HorizontalOffset = 0;
               m_LateralOffset = 0;
               m_Tension_Mode = Tension_Mode_val.Calculated;
               m_Tension = 1500;
               m_RTS_Strength = 26900;
               m_PoundsPerInch = 0.03408;
               m_ModulusOfElasticity = 26000000;
               m_PoissonsRatio = 0.3;
               m_WindDragCoef = 0;
               m_ThermalCoefficient = 2.7E-06;
               m_GuyMaterial = "<Default>";
               m_StrutMaterial = "<Default>";
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Clearance) return true;
         if(pChildCandidate is Material) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Guy wire description
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   Type
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Down
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Span/Head  (Span Guy)
        //        Sidewalk  (Sidewalk Guy)
        //        Crossarm  (Crossarm Guy)
        //        Pushbrace  (Pushbrace)
        public enum Type_val
        {
           [Description("Down")]
           Down,    //Down Guy
           [Description("Span/Head")]
           Span_Head,    //Span Guy
           [Description("Sidewalk")]
           Sidewalk,    //Sidewalk Guy
           [Description("Crossarm")]
           Crossarm,    //Crossarm Guy
           [Description("Pushbrace")]
           Pushbrace     //Pushbrace
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Down":
                   return Type_val.Down;    //Down Guy
                case "Span/Head":
                   return Type_val.Span_Head;    //Span Guy
                case "Sidewalk":
                   return Type_val.Sidewalk;    //Sidewalk Guy
                case "Crossarm":
                   return Type_val.Crossarm;    //Crossarm Guy
                case "Pushbrace":
                   return Type_val.Pushbrace;    //Pushbrace
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Down:
                   return "Down";    //Down Guy
                case Type_val.Span_Head:
                   return "Span/Head";    //Span Guy
                case Type_val.Sidewalk:
                   return "Sidewalk";    //Sidewalk Guy
                case Type_val.Crossarm:
                   return "Crossarm";    //Crossarm Guy
                case Type_val.Pushbrace:
                   return "Pushbrace";    //Pushbrace
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Install Height (ft)
        //   Description:   
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   453
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   StrutHeightInInches
        //   Attr Group:Sidewalk
        //   Alt Display Name:Strut Height (ft)
        //   Description:   Height of the strut from groundline if it is a sidewalk guy
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   405
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrutHeightInInches;
        [Category("Sidewalk")]
        [Description("StrutHeightInInches")]
        public double StrutHeightInInches
        {
           get { return m_StrutHeightInInches; }
           set { m_StrutHeightInInches = value; }
        }



        //   Attr Name:   StrutLengthInInches
        //   Attr Group:Sidewalk
        //   Alt Display Name:Strut Length (ft)
        //   Description:   The length of the strut if it is a sidewalk guy
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   48
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrutLengthInInches;
        [Category("Sidewalk")]
        [Description("StrutLengthInInches")]
        public double StrutLengthInInches
        {
           get { return m_StrutLengthInInches; }
           set { m_StrutLengthInInches = value; }
        }



        //   Attr Name:   StrutDiameter
        //   Attr Group:Sidewalk
        //   Alt Display Name:Strut Diameter (in)
        //   Description:   The diameter of the strut
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   1.25
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrutDiameter;
        [Category("Sidewalk")]
        [Description("StrutDiameter")]
        public double StrutDiameter
        {
           get { return m_StrutDiameter; }
           set { m_StrutDiameter = value; }
        }



        //   Attr Name:   StrutWeightPerIn
        //   Attr Group:Sidewalk
        //   Alt Display Name:Strut Weight/Len (lbs/ft)
        //   Description:   The weight of the strut in lbs per unit length
        //   Displayed Units:   store as POUNDS PER IN display as POUNDS PER FT or KILOGRAMS PER METER
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.189167
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrutWeightPerIn;
        [Category("Sidewalk")]
        [Description("StrutWeightPerIn")]
        public double StrutWeightPerIn
        {
           get { return m_StrutWeightPerIn; }
           set { m_StrutWeightPerIn = value; }
        }



        //   Attr Name:   StrutCapacity
        //   Attr Group:Sidewalk
        //   Alt Display Name:Allowable Strut Load (lbs)
        //   Description:   The capacity of the strut in lbs
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   2500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrutCapacity;
        [Category("Sidewalk")]
        [Description("StrutCapacity")]
        public double StrutCapacity
        {
           get { return m_StrutCapacity; }
           set { m_StrutCapacity = value; }
        }



        //   Attr Name:   StrutBendingMomentMax
        //   Attr Group:Sidewalk
        //   Alt Display Name:Max Strut Moment Cap (ft-lbs)
        //   Description:   The bending moment capacity of the strut in ft-lbs
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   800
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_StrutBendingMomentMax;
        [Category("Sidewalk")]
        [Description("StrutBendingMomentMax")]
        public double StrutBendingMomentMax
        {
           get { return m_StrutBendingMomentMax; }
           set { m_StrutBendingMomentMax = value; }
        }



        //   Attr Name:   StrutFixity
        //   Attr Group:Sidewalk
        //   Alt Display Name:Strut Fixity
        //   Description:   The fixity of the strut
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Pinned
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Pinned  (Strut Pinned)
        public enum StrutFixity_val
        {
           [Description("Fixed")]
           Fixed,    //Strut Fixed
           [Description("Pinned")]
           Pinned     //Strut Pinned
        }
        private StrutFixity_val m_StrutFixity;
        [Category("Sidewalk")]
        [Description("StrutFixity")]
        public StrutFixity_val StrutFixity
        {
           get
           { return m_StrutFixity; }
           set
           { m_StrutFixity = value; }
        }

        public StrutFixity_val String_to_StrutFixity_val(string pKey)
        {
           switch (pKey)
           {
                case "Fixed":
                   return StrutFixity_val.Fixed;    //Strut Fixed
                case "Pinned":
                   return StrutFixity_val.Pinned;    //Strut Pinned
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string StrutFixity_val_to_String(StrutFixity_val pKey)
        {
           switch (pKey)
           {
                case StrutFixity_val.Fixed:
                   return "Fixed";    //Strut Fixed
                case StrutFixity_val.Pinned:
                   return "Pinned";    //Strut Pinned
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   MergeStruts
        //   Attr Group:Sidewalk
        //   Alt Display Name:Merge Like Struts
        //   Description:   Merge with like struts at same position,
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_MergeStruts;
        [Category("Sidewalk")]
        [Description("MergeStruts")]
        public bool MergeStruts
        {
           get { return m_MergeStruts; }
           set { m_MergeStruts = value; }
        }



        //   Attr Name:   DeltaHeightInInches
        //   Attr Group:Standard
        //   Alt Display Name:Span Guy DeltaHt (ft)
        //   Description:   Difference in height of the far end if it is a span guy
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_DeltaHeightInInches;
        [Category("Standard")]
        [Description("DeltaHeightInInches")]
        public double DeltaHeightInInches
        {
           get { return m_DeltaHeightInInches; }
           set { m_DeltaHeightInInches = value; }
        }



        //   Attr Name:   Diameter
        //   Attr Group:Standard
        //   Alt Display Name:Diameter (in)
        //   Description:   Guy wire diameter in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Diameter;
        [Category("Standard")]
        [Description("Diameter")]
        public double Diameter
        {
           get { return m_Diameter; }
           set { m_Diameter = value; }
        }



        //   Attr Name:   PercentSolid
        //   Attr Group:Standard
        //   Alt Display Name:Percent Solid
        //   Description:   Percent Solid
        //   Displayed Units:   store as PERCENT 0 TO 1 display as PERCENT 0 TO 100
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   1.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PercentSolid;
        [Category("Standard")]
        [Description("PercentSolid")]
        public double PercentSolid
        {
           get { return m_PercentSolid; }
           set { m_PercentSolid = value; }
        }



        //   Attr Name:   PreTension
        //   Attr Group:Standard
        //   Alt Display Name:Pre-tension (lbs)
        //   Description:   Pre-tension in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   700
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PreTension;
        [Category("Standard")]
        [Description("PreTension")]
        public double PreTension
        {
           get { return m_PreTension; }
           set { m_PreTension = value; }
        }



        //   Attr Name:   VerticalOffset
        //   Attr Group:Control
        //   Alt Display Name:Vertical Offset (in)
        //   Description:   Vertical Offset in Inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_VerticalOffset;
        [Category("Control")]
        [Description("VerticalOffset")]
        public double VerticalOffset
        {
           get { return m_VerticalOffset; }
           set { m_VerticalOffset = value; }
        }



        //   Attr Name:   HorizontalOffset
        //   Attr Group:Control
        //   Alt Display Name:Horizontal Offset (in)
        //   Description:   Horizontal Offset in Inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_HorizontalOffset;
        [Category("Control")]
        [Description("HorizontalOffset")]
        public double HorizontalOffset
        {
           get { return m_HorizontalOffset; }
           set { m_HorizontalOffset = value; }
        }



        //   Attr Name:   LateralOffset
        //   Attr Group:Control
        //   Alt Display Name:Lateral Offset (in)
        //   Description:   LateralOffset Offset in Inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LateralOffset;
        [Category("Control")]
        [Description("LateralOffset")]
        public double LateralOffset
        {
           get { return m_LateralOffset; }
           set { m_LateralOffset = value; }
        }



        //   Attr Name:   Tension Mode
        //   Attr Group:Control
        //   Description:   Tension mode
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Calculated
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Manual  (Manual)
        public enum Tension_Mode_val
        {
           [Description("Calculated")]
           Calculated,    //Calculated
           [Description("Manual")]
           Manual     //Manual
        }
        private Tension_Mode_val m_Tension_Mode;
        [Category("Control")]
        [Description("Tension Mode")]
        public Tension_Mode_val Tension_Mode
        {
           get
           { return m_Tension_Mode; }
           set
           { m_Tension_Mode = value; }
        }

        public Tension_Mode_val String_to_Tension_Mode_val(string pKey)
        {
           switch (pKey)
           {
                case "Calculated":
                   return Tension_Mode_val.Calculated;    //Calculated
                case "Manual":
                   return Tension_Mode_val.Manual;    //Manual
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Tension_Mode_val_to_String(Tension_Mode_val pKey)
        {
           switch (pKey)
           {
                case Tension_Mode_val.Calculated:
                   return "Calculated";    //Calculated
                case Tension_Mode_val.Manual:
                   return "Manual";    //Manual
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Tension
        //   Attr Group:Control
        //   Alt Display Name:Man Tension (lbs)
        //   Description:   Tension in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   1500
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Tension;
        [Category("Control")]
        [Description("Tension")]
        public double Tension
        {
           get { return m_Tension; }
           set { m_Tension = value; }
        }



        //   Attr Name:   RTS Strength
        //   Attr Group:Standard
        //   Alt Display Name:Strength (lbs)
        //   Description:   RTS Strength in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   26900
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RTS_Strength;
        [Category("Standard")]
        [Description("RTS Strength")]
        public double RTS_Strength
        {
           get { return m_RTS_Strength; }
           set { m_RTS_Strength = value; }
        }



        //   Attr Name:   PoundsPerInch
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Weight (lbs/ft)
        //   Description:   Linear weight in pounds per inch of length
        //   Displayed Units:   store as POUNDS PER IN display as POUNDS PER FT or KILOGRAMS PER METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.03408
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoundsPerInch;
        [Category("Phys. Consts")]
        [Description("PoundsPerInch")]
        public double PoundsPerInch
        {
           get { return m_PoundsPerInch; }
           set { m_PoundsPerInch = value; }
        }



        //   Attr Name:   ModulusOfElasticity
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   ModulusOfElasticity
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   26000000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ModulusOfElasticity;
        [Category("Phys. Consts")]
        [Description("ModulusOfElasticity")]
        public double ModulusOfElasticity
        {
           get { return m_ModulusOfElasticity; }
           set { m_ModulusOfElasticity = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.3
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Phys. Consts")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Phys. Consts")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Phys. Consts
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000027
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Phys. Consts")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   GuyMaterial
        //   Attr Group:Material Override
        //   Alt Display Name:Guy Material
        //   Description:   Guy Material
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   CHILD_MATERIAL_NAME
        //   Default Value:   <Default>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_GuyMaterial;
        [Category("Material Override")]
        [Description("GuyMaterial")]
        public string GuyMaterial
        {
           get { return m_GuyMaterial; }
           set { m_GuyMaterial = value; }
        }



        //   Attr Name:   StrutMaterial
        //   Attr Group:Material Override
        //   Alt Display Name:Strut Material
        //   Description:   Strut Material
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   CHILD_MATERIAL_NAME
        //   Default Value:   <Default>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_StrutMaterial;
        [Category("Material Override")]
        [Description("StrutMaterial")]
        public string StrutMaterial
        {
           get { return m_StrutMaterial; }
           set { m_StrutMaterial = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Riser
   // Mirrors: PPLRiser : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Riser : ElementBase
   {

      public static string gXMLkey = "Riser";
      public override string XMLkey() { return gXMLkey; }

      public Riser(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Riser";
               m_Owner = "<Undefined>";
               m_CoordinateA = 1.5707963267949;
               m_DiameterInInches = 3.5;
               m_OffsetInInches = 0;
               m_LengthAboveGLInInches = 300;
               m_PoundsPerInch = 0.0833333;
               m_Apply_Wind = false;
               m_WindDragCoef = 0;
               m_Wind_Shadowing = false;
               m_Self_Supporting = false;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the riser
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Riser
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Install Angle (°)
        //   Description:   Install Angle
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   1.5707963267949
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   DiameterInInches
        //   Attr Group:Standard
        //   Alt Display Name:Diameter (in)
        //   Description:   The diameter of the riser
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   FLOAT
        //   Default Value:   3.5
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DiameterInInches;
        [Category("Standard")]
        [Description("DiameterInInches")]
        public double DiameterInInches
        {
           get { return m_DiameterInInches; }
           set { m_DiameterInInches = value; }
        }



        //   Attr Name:   OffsetInInches
        //   Attr Group:Standard
        //   Alt Display Name:Standoff (in)
        //   Description:   The offset if the riser is on standoffs
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   TRACKERX
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_OffsetInInches;
        [Category("Standard")]
        [Description("OffsetInInches")]
        public double OffsetInInches
        {
           get { return m_OffsetInInches; }
           set { m_OffsetInInches = value; }
        }



        //   Attr Name:   LengthAboveGLInInches
        //   Attr Group:Standard
        //   Alt Display Name:Length AGL (ft)
        //   Description:   The length above GL
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERZ
        //   Default Value:   300
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthAboveGLInInches;
        [Category("Standard")]
        [Description("LengthAboveGLInInches")]
        public double LengthAboveGLInInches
        {
           get { return m_LengthAboveGLInInches; }
           set { m_LengthAboveGLInInches = value; }
        }



        //   Attr Name:   PoundsPerInch
        //   Attr Group:Load Analysis
        //   Alt Display Name:Weight (lbs/ft)
        //   Description:   The weight in pounds per inch of running length
        //   Displayed Units:   store as POUNDS PER IN display as POUNDS PER FT or KILOGRAMS PER METER
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0000
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0833333
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoundsPerInch;
        [Category("Load Analysis")]
        [Description("PoundsPerInch")]
        public double PoundsPerInch
        {
           get { return m_PoundsPerInch; }
           set { m_PoundsPerInch = value; }
        }



        //   Attr Name:   Apply Wind
        //   Attr Group:Load Analysis
        //   Description:   Apply wind to the riser?
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_Apply_Wind;
        [Category("Load Analysis")]
        [Description("Apply Wind")]
        public bool Apply_Wind
        {
           get { return m_Apply_Wind; }
           set { m_Apply_Wind = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Load Analysis
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Load Analysis")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   Wind Shadowing
        //   Attr Group:Load Analysis
        //   Description:   Does the pole shadow wind on the riser?
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_Wind_Shadowing;
        [Category("Load Analysis")]
        [Description("Wind Shadowing")]
        public bool Wind_Shadowing
        {
           get { return m_Wind_Shadowing; }
           set { m_Wind_Shadowing = value; }
        }



        //   Attr Name:   Self Supporting
        //   Attr Group:Load Analysis
        //   Description:   Is the riser vertically self supporting?
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_Self_Supporting;
        [Category("Load Analysis")]
        [Description("Self Supporting")]
        public bool Self_Supporting
        {
           get { return m_Self_Supporting; }
           set { m_Self_Supporting = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: GenericEquipment
   // Mirrors: PPLGenericEquipment : PPLElement
   //--------------------------------------------------------------------------------------------
   public class GenericEquipment : ElementBase
   {

      public static string gXMLkey = "GenericEquipment";
      public override string XMLkey() { return gXMLkey; }

      public GenericEquipment(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_CoordinateX = 10;
               m_Description = "Generic Equipment";
               m_Owner = "<Undefined>";
               m_CoordinateZ = 0;
               m_CoordinateZ_Rel = 0;
               m_CoordinateA = 0;
               m_Parent_Gap = 6;
               m_Shape = Shape_val.Box;
               m_Points = "12";
               m_DiameterOrWidthInInches = 12;
               m_DepthInInches = 12;
               m_HeightInInches = 12;
               m_Pitch = 0;
               m_Roll = 0;
               m_Yaw = 0;
               m_Weight = 10;
               m_WindDragCoef = 0;
               m_Color = "#FF4682B4";
               m_ShowLabel = false;
               m_TextColor = "#FFFFFF00";
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is GenericEquipment) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Description:   The distance in inches from the center of the parent pole to the point where the bracket is bolted to the pole
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   10
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of equipment
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Generic Equipment
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Height (ft)
        //   Description:   The distance in inches from the bottom of the parent (butt of the pole for example) to the center of the equipment
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateZ_Rel
        //   Attr Group:Standard
        //   Alt Display Name:Relative Height (in)
        //   Description:   The distance in inches from the bottom of the parent (butt of the pole for example) to the center of the equipment
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ_Rel;
        [Category("Standard")]
        [Description("CoordinateZ_Rel")]
        public double CoordinateZ_Rel
        {
           get { return m_CoordinateZ_Rel; }
           set { m_CoordinateZ_Rel = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   Angle of equipment relative to the parent structure (typically pole)
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   Parent Gap
        //   Attr Group:Standard
        //   Alt Display Name:Relative Gap (in)
        //   Description:   Distance between parent and equipment in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   6
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Parent_Gap;
        [Category("Standard")]
        [Description("Parent Gap")]
        public double Parent_Gap
        {
           get { return m_Parent_Gap; }
           set { m_Parent_Gap = value; }
        }



        //   Attr Name:   Shape
        //   Attr Group:Specifications
        //   Description:   Shape of equipment
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Box
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Cylinder  (Cylinder)
        //        Imported  (Imported)
        public enum Shape_val
        {
           [Description("Box")]
           Box,    //Box
           [Description("Cylinder")]
           Cylinder,    //Cylinder
           [Description("Imported")]
           Imported     //Imported
        }
        private Shape_val m_Shape;
        [Category("Specifications")]
        [Description("Shape")]
        public Shape_val Shape
        {
           get
           { return m_Shape; }
           set
           { m_Shape = value; }
        }

        public Shape_val String_to_Shape_val(string pKey)
        {
           switch (pKey)
           {
                case "Box":
                   return Shape_val.Box;    //Box
                case "Cylinder":
                   return Shape_val.Cylinder;    //Cylinder
                case "Imported":
                   return Shape_val.Imported;    //Imported
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Shape_val_to_String(Shape_val pKey)
        {
           switch (pKey)
           {
                case Shape_val.Box:
                   return "Box";    //Box
                case Shape_val.Cylinder:
                   return "Cylinder";    //Cylinder
                case Shape_val.Imported:
                   return "Imported";    //Imported
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Points
        //   Attr Group:Specifications
        //   Description:   The points defining the shape
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   SHAPE_DEFINITION
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Points;
        [Category("Specifications")]
        [Description("Points")]
        public string Points
        {
           get { return m_Points; }
           set { m_Points = value; }
        }



        //   Attr Name:   DiameterOrWidthInInches
        //   Attr Group:Specifications
        //   Alt Display Name:Unit Width/Diameter (in)
        //   Description:   The Diameter or Width in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DiameterOrWidthInInches;
        [Category("Specifications")]
        [Description("DiameterOrWidthInInches")]
        public double DiameterOrWidthInInches
        {
           get { return m_DiameterOrWidthInInches; }
           set { m_DiameterOrWidthInInches = value; }
        }



        //   Attr Name:   DepthInInches
        //   Attr Group:Specifications
        //   Alt Display Name:Unit Depth (in)
        //   Description:   The Depth in inches which is only used if the shape of the equipment is a box
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DepthInInches;
        [Category("Specifications")]
        [Description("DepthInInches")]
        public double DepthInInches
        {
           get { return m_DepthInInches; }
           set { m_DepthInInches = value; }
        }



        //   Attr Name:   HeightInInches
        //   Attr Group:Specifications
        //   Alt Display Name:Unit Height (in)
        //   Description:   The Height in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_HeightInInches;
        [Category("Specifications")]
        [Description("HeightInInches")]
        public double HeightInInches
        {
           get { return m_HeightInInches; }
           set { m_HeightInInches = value; }
        }



        //   Attr Name:   Pitch
        //   Attr Group:Standard
        //   Alt Display Name:Pitch (°)
        //   Description:   Pitch in radians.
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Pitch;
        [Category("Standard")]
        [Description("Pitch")]
        public double Pitch
        {
           get { return m_Pitch; }
           set { m_Pitch = value; }
        }



        //   Attr Name:   Roll
        //   Attr Group:Standard
        //   Alt Display Name:Roll (°)
        //   Description:   Roll in radians.
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Roll;
        [Category("Standard")]
        [Description("Roll")]
        public double Roll
        {
           get { return m_Roll; }
           set { m_Roll = value; }
        }



        //   Attr Name:   Yaw
        //   Attr Group:Standard
        //   Alt Display Name:Yaw (°)
        //   Description:   Yaw in radians.
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Yaw;
        [Category("Standard")]
        [Description("Yaw")]
        public double Yaw
        {
           get { return m_Yaw; }
           set { m_Yaw = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Specifications
        //   Alt Display Name:Unit Weight (lbs)
        //   Description:   The weight of the element in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   10.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Specifications")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Specifications
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Specifications")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   Color
        //   Attr Group:Specifications
        //   Alt Display Name:Display Color
        //   Description:   The color of the element
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   COLOR
        //   Default Value:   #FF4682B4
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Color;
        [Category("Specifications")]
        [Description("Color")]
        public string Color
        {
           get { return m_Color; }
           set { m_Color = value; }
        }



        //   Attr Name:   ShowLabel
        //   Attr Group:Specifications
        //   Alt Display Name:Show Label
        //   Description:   Indicates if the label is displayed
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_ShowLabel;
        [Category("Specifications")]
        [Description("ShowLabel")]
        public bool ShowLabel
        {
           get { return m_ShowLabel; }
           set { m_ShowLabel = value; }
        }



        //   Attr Name:   TextColor
        //   Attr Group:Specifications
        //   Alt Display Name:Label Color
        //   Description:   The color of the element's text
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   COLOR
        //   Default Value:   #FFFFFF00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_TextColor;
        [Category("Specifications")]
        [Description("TextColor")]
        public string TextColor
        {
           get { return m_TextColor; }
           set { m_TextColor = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: PoleRestoration
   // Mirrors: PPLRestoration : PPLElement
   //--------------------------------------------------------------------------------------------
   public class PoleRestoration : ElementBase
   {

      public static string gXMLkey = "PoleRestoration";
      public override string XMLkey() { return gXMLkey; }

      public PoleRestoration(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Type = Type_val.C2;
               m_Description = "Osmose";
               m_Owner = "<Undefined>";
               m_LengthInInches = 120;
               m_MomentCapacityTable = new ValTable("Moment;0,10000;");
               m_CoordinateZ = 160;
               m_CoordinateA = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   Type of restoration
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   C2
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        C2  (Osmose C2 Truss)
        //        ET  (Osmose ET Truss)
        //        FiberWrap  (Osmose FiberWrap)
        //        FiberWrap II  (Osmose FiberWrap II)
        //        Truss  (Other Truss)
        //        Wrap  (Other Pole Wrap)
        public enum Type_val
        {
           [Description("C")]
           C,    //Osmose C Truss
           [Description("C2")]
           C2,    //Osmose C2 Truss
           [Description("ET")]
           ET,    //Osmose ET Truss
           [Description("FiberWrap")]
           FiberWrap,    //Osmose FiberWrap
           [Description("FiberWrap II")]
           FiberWrap_II,    //Osmose FiberWrap II
           [Description("Truss")]
           Truss,    //Other Truss
           [Description("Wrap")]
           Wrap     //Other Pole Wrap
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "C":
                   return Type_val.C;    //Osmose C Truss
                case "C2":
                   return Type_val.C2;    //Osmose C2 Truss
                case "ET":
                   return Type_val.ET;    //Osmose ET Truss
                case "FiberWrap":
                   return Type_val.FiberWrap;    //Osmose FiberWrap
                case "FiberWrap II":
                   return Type_val.FiberWrap_II;    //Osmose FiberWrap II
                case "Truss":
                   return Type_val.Truss;    //Other Truss
                case "Wrap":
                   return Type_val.Wrap;    //Other Pole Wrap
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.C:
                   return "C";    //Osmose C Truss
                case Type_val.C2:
                   return "C2";    //Osmose C2 Truss
                case Type_val.ET:
                   return "ET";    //Osmose ET Truss
                case Type_val.FiberWrap:
                   return "FiberWrap";    //Osmose FiberWrap
                case Type_val.FiberWrap_II:
                   return "FiberWrap II";    //Osmose FiberWrap II
                case Type_val.Truss:
                   return "Truss";    //Other Truss
                case Type_val.Wrap:
                   return "Wrap";    //Other Pole Wrap
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the restoration
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Osmose
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   LengthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Length (ft)
        //   Description:   The length in iches
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   120.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LengthInInches;
        [Category("Standard")]
        [Description("LengthInInches")]
        public double LengthInInches
        {
           get { return m_LengthInInches; }
           set { m_LengthInInches = value; }
        }



        //   Attr Name:   MomentCapacityTable
        //   Attr Group:Standard
        //   Alt Display Name:Moment Added (ft-lb)
        //   Description:   The moment capacity table
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   MOMENT_TABLE
        //   Default Value:   Moment;0,10000;
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private ValTable m_MomentCapacityTable = new ValTable();
        [Category("Standard")]
        [Description("MomentCapacityTable")]
        public ValTable MomentCapacityTable
        {
           get { return m_MomentCapacityTable; }
           set { m_MomentCapacityTable = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Top Of Restoration (ft)
        //   Description:   Distance from the butt of the pole to the top of the truss or wrap
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   160.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The rotation angle around the center of the pole
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Clearance
   // Mirrors: PPLClearance : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Clearance : ElementBase
   {

      public static string gXMLkey = "Clearance";
      public override string XMLkey() { return gXMLkey; }

      public Clearance(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Clearance";
               m_Render = false;
               m_Mode = Mode_val.Automatic;
               m_SagMinimum = 0;
               m_SagNominal = 0;
               m_SagMaximum = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the clearance item
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Clearance
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Group
        //   Attr Group:Standard
        //   Alt Display Name:Clearance Group
        //   Description:   The clearance group
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        public enum Group_val
        {
        }
        private Group_val m_Group;
        [Category("Standard")]
        [Description("Group")]
        public Group_val Group
        {
           get
           { return m_Group; }
           set
           { m_Group = value; }
        }

        public Group_val String_to_Group_val(string pKey)
        {
           switch (pKey)
           {
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Group_val_to_String(Group_val pKey)
        {
           switch (pKey)
           {
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Render
        //   Attr Group:Standard
        //   Alt Display Name:Render in 3D view
        //   Description:   Render
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_Render;
        [Category("Standard")]
        [Description("Render")]
        public bool Render
        {
           get { return m_Render; }
           set { m_Render = value; }
        }



        //   Attr Name:   Mode
        //   Attr Group:Standard
        //   Alt Display Name:Population Mode
        //   Description:   The population
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Automatic
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Automatic  (Automatic)
        //        External  (External)
        public enum Mode_val
        {
           [Description("Manual")]
           Manual,    //Manual
           [Description("Automatic")]
           Automatic,    //Automatic
           [Description("External")]
           External     //External
        }
        private Mode_val m_Mode;
        [Category("Standard")]
        [Description("Mode")]
        public Mode_val Mode
        {
           get
           { return m_Mode; }
           set
           { m_Mode = value; }
        }

        public Mode_val String_to_Mode_val(string pKey)
        {
           switch (pKey)
           {
                case "Manual":
                   return Mode_val.Manual;    //Manual
                case "Automatic":
                   return Mode_val.Automatic;    //Automatic
                case "External":
                   return Mode_val.External;    //External
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Mode_val_to_String(Mode_val pKey)
        {
           switch (pKey)
           {
                case Mode_val.Manual:
                   return "Manual";    //Manual
                case Mode_val.Automatic:
                   return "Automatic";    //Automatic
                case Mode_val.External:
                   return "External";    //External
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   SagMinimum
        //   Attr Group:Standard
        //   Alt Display Name:Sag Minimum (ft)
        //   Description:   The vertical deflection minimum
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SagMinimum;
        [Category("Standard")]
        [Description("SagMinimum")]
        public double SagMinimum
        {
           get { return m_SagMinimum; }
           set { m_SagMinimum = value; }
        }



        //   Attr Name:   SagNominal
        //   Attr Group:Standard
        //   Alt Display Name:Sag Nominal (ft)
        //   Description:   The vertical deflection nominal
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SagNominal;
        [Category("Standard")]
        [Description("SagNominal")]
        public double SagNominal
        {
           get { return m_SagNominal; }
           set { m_SagNominal = value; }
        }



        //   Attr Name:   SagMaximum
        //   Attr Group:Standard
        //   Alt Display Name:Sag Maximum (ft)
        //   Description:   The vertical deflection maximum
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SagMaximum;
        [Category("Standard")]
        [Description("SagMaximum")]
        public double SagMaximum
        {
           get { return m_SagMaximum; }
           set { m_SagMaximum = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: SpanAddition
   // Mirrors: PPLSpanAddition : PPLElement
   //--------------------------------------------------------------------------------------------
   public class SpanAddition : ElementBase
   {

      public static string gXMLkey = "SpanAddition";
      public override string XMLkey() { return gXMLkey; }

      public SpanAddition(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Span Addition";
               m_Owner = "<Undefined>";
               m_Type = Type_val.Aviation_Ball;
               m_OffsetInches = 120;
               m_Size = 20;
               m_Weight = 7;
               m_WindDragCoef = 0;
               m_LoopOrientation = LoopOrientation_val.Vertical;
               m_LoopPosition = LoopPosition_val.Minus;
               m_LeadOne = false;
               m_LeadOnePosition = LeadOnePosition_val.Minus;
               m_LeadOneOffset = 80;
               m_LeadTwo = false;
               m_LeadTwoPosition = LeadTwoPosition_val.Plus;
               m_LeadTwoOffset = 80;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the span addition item
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Span Addition
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Alt Display Name:Addition Type
        //   Description:   The type of the span addition item
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Aviation Ball
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Splice  (Splice)
        //        Damper  (Damper)
        //        Aviation Ball  (Aviation Ball)
        //        Perch Stopper  (Perch Stopper)
        //        Maintenance Loop  (Maintenance Loop)
        //        Other  (Other)
        public enum Type_val
        {
           [Description("Cut-Out")]
           Cut_Out,    //Cut-Out
           [Description("Splice")]
           Splice,    //Splice
           [Description("Damper")]
           Damper,    //Damper
           [Description("Aviation Ball")]
           Aviation_Ball,    //Aviation Ball
           [Description("Perch Stopper")]
           Perch_Stopper,    //Perch Stopper
           [Description("Maintenance Loop")]
           Maintenance_Loop,    //Maintenance Loop
           [Description("Other")]
           Other     //Other
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Cut-Out":
                   return Type_val.Cut_Out;    //Cut-Out
                case "Splice":
                   return Type_val.Splice;    //Splice
                case "Damper":
                   return Type_val.Damper;    //Damper
                case "Aviation Ball":
                   return Type_val.Aviation_Ball;    //Aviation Ball
                case "Perch Stopper":
                   return Type_val.Perch_Stopper;    //Perch Stopper
                case "Maintenance Loop":
                   return Type_val.Maintenance_Loop;    //Maintenance Loop
                case "Other":
                   return Type_val.Other;    //Other
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Cut_Out:
                   return "Cut-Out";    //Cut-Out
                case Type_val.Splice:
                   return "Splice";    //Splice
                case Type_val.Damper:
                   return "Damper";    //Damper
                case Type_val.Aviation_Ball:
                   return "Aviation Ball";    //Aviation Ball
                case Type_val.Perch_Stopper:
                   return "Perch Stopper";    //Perch Stopper
                case Type_val.Maintenance_Loop:
                   return "Maintenance Loop";    //Maintenance Loop
                case Type_val.Other:
                   return "Other";    //Other
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   OffsetInches
        //   Attr Group:Standard
        //   Alt Display Name:Offset Dist (ft)
        //   Description:   Offset distance from pole
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   120.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_OffsetInches;
        [Category("Standard")]
        [Description("OffsetInches")]
        public double OffsetInches
        {
           get { return m_OffsetInches; }
           set { m_OffsetInches = value; }
        }



        //   Attr Name:   Size
        //   Attr Group:Standard
        //   Alt Display Name:Size (in)
        //   Description:   Size of the addition in the primary dimension
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   20.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Size;
        [Category("Standard")]
        [Description("Size")]
        public double Size
        {
           get { return m_Size; }
           set { m_Size = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Standard
        //   Alt Display Name:Weight (lbs)
        //   Description:   Weight of the addition
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   7.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Standard")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   WindDragCoef
        //   Attr Group:Standard
        //   Alt Display Name:Wind Drag Coef.
        //   Description:   Wind Drag Coefficient
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0###
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindDragCoef;
        [Category("Standard")]
        [Description("WindDragCoef")]
        public double WindDragCoef
        {
           get { return m_WindDragCoef; }
           set { m_WindDragCoef = value; }
        }



        //   Attr Name:   LoopOrientation
        //   Attr Group:Loop
        //   Alt Display Name:Loop Orientation
        //   Description:   The loop orientation
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Vertical
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Horizontal  (Horizontal)
        public enum LoopOrientation_val
        {
           [Description("Vertical")]
           Vertical,    //Vertical
           [Description("Horizontal")]
           Horizontal     //Horizontal
        }
        private LoopOrientation_val m_LoopOrientation;
        [Category("Loop")]
        [Description("LoopOrientation")]
        public LoopOrientation_val LoopOrientation
        {
           get
           { return m_LoopOrientation; }
           set
           { m_LoopOrientation = value; }
        }

        public LoopOrientation_val String_to_LoopOrientation_val(string pKey)
        {
           switch (pKey)
           {
                case "Vertical":
                   return LoopOrientation_val.Vertical;    //Vertical
                case "Horizontal":
                   return LoopOrientation_val.Horizontal;    //Horizontal
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string LoopOrientation_val_to_String(LoopOrientation_val pKey)
        {
           switch (pKey)
           {
                case LoopOrientation_val.Vertical:
                   return "Vertical";    //Vertical
                case LoopOrientation_val.Horizontal:
                   return "Horizontal";    //Horizontal
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   LoopPosition
        //   Attr Group:Loop
        //   Alt Display Name:Loop Position
        //   Description:   The loop position
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Minus
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Center  (Center)
        //        Minus  (Minus)
        public enum LoopPosition_val
        {
           [Description("Plus")]
           Plus,    //Plus
           [Description("Center")]
           Center,    //Center
           [Description("Minus")]
           Minus     //Minus
        }
        private LoopPosition_val m_LoopPosition;
        [Category("Loop")]
        [Description("LoopPosition")]
        public LoopPosition_val LoopPosition
        {
           get
           { return m_LoopPosition; }
           set
           { m_LoopPosition = value; }
        }

        public LoopPosition_val String_to_LoopPosition_val(string pKey)
        {
           switch (pKey)
           {
                case "Plus":
                   return LoopPosition_val.Plus;    //Plus
                case "Center":
                   return LoopPosition_val.Center;    //Center
                case "Minus":
                   return LoopPosition_val.Minus;    //Minus
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string LoopPosition_val_to_String(LoopPosition_val pKey)
        {
           switch (pKey)
           {
                case LoopPosition_val.Plus:
                   return "Plus";    //Plus
                case LoopPosition_val.Center:
                   return "Center";    //Center
                case LoopPosition_val.Minus:
                   return "Minus";    //Minus
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   LeadOne
        //   Attr Group:Loop
        //   Alt Display Name:Lead One
        //   Description:   Lead one
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_LeadOne;
        [Category("Loop")]
        [Description("LeadOne")]
        public bool LeadOne
        {
           get { return m_LeadOne; }
           set { m_LeadOne = value; }
        }



        //   Attr Name:   LeadOnePosition
        //   Attr Group:Loop
        //   Alt Display Name:Lead One Position
        //   Description:   The lead one position
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Minus
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Center  (Center)
        //        Minus  (Minus)
        public enum LeadOnePosition_val
        {
           [Description("Plus")]
           Plus,    //Plus
           [Description("Center")]
           Center,    //Center
           [Description("Minus")]
           Minus     //Minus
        }
        private LeadOnePosition_val m_LeadOnePosition;
        [Category("Loop")]
        [Description("LeadOnePosition")]
        public LeadOnePosition_val LeadOnePosition
        {
           get
           { return m_LeadOnePosition; }
           set
           { m_LeadOnePosition = value; }
        }

        public LeadOnePosition_val String_to_LeadOnePosition_val(string pKey)
        {
           switch (pKey)
           {
                case "Plus":
                   return LeadOnePosition_val.Plus;    //Plus
                case "Center":
                   return LeadOnePosition_val.Center;    //Center
                case "Minus":
                   return LeadOnePosition_val.Minus;    //Minus
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string LeadOnePosition_val_to_String(LeadOnePosition_val pKey)
        {
           switch (pKey)
           {
                case LeadOnePosition_val.Plus:
                   return "Plus";    //Plus
                case LeadOnePosition_val.Center:
                   return "Center";    //Center
                case LeadOnePosition_val.Minus:
                   return "Minus";    //Minus
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   LeadOneOffset
        //   Attr Group:Loop
        //   Alt Display Name:Lead One Offset (ft)
        //   Description:   Lead One offset distance from pole
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   80.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeadOneOffset;
        [Category("Loop")]
        [Description("LeadOneOffset")]
        public double LeadOneOffset
        {
           get { return m_LeadOneOffset; }
           set { m_LeadOneOffset = value; }
        }



        //   Attr Name:   LeadTwo
        //   Attr Group:Loop
        //   Alt Display Name:Lead Two
        //   Description:   Lead two
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_LeadTwo;
        [Category("Loop")]
        [Description("LeadTwo")]
        public bool LeadTwo
        {
           get { return m_LeadTwo; }
           set { m_LeadTwo = value; }
        }



        //   Attr Name:   LeadTwoPosition
        //   Attr Group:Loop
        //   Alt Display Name:Lead Two Position
        //   Description:   The lead one position
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Plus
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Center  (Center)
        //        Minus  (Minus)
        public enum LeadTwoPosition_val
        {
           [Description("Plus")]
           Plus,    //Plus
           [Description("Center")]
           Center,    //Center
           [Description("Minus")]
           Minus     //Minus
        }
        private LeadTwoPosition_val m_LeadTwoPosition;
        [Category("Loop")]
        [Description("LeadTwoPosition")]
        public LeadTwoPosition_val LeadTwoPosition
        {
           get
           { return m_LeadTwoPosition; }
           set
           { m_LeadTwoPosition = value; }
        }

        public LeadTwoPosition_val String_to_LeadTwoPosition_val(string pKey)
        {
           switch (pKey)
           {
                case "Plus":
                   return LeadTwoPosition_val.Plus;    //Plus
                case "Center":
                   return LeadTwoPosition_val.Center;    //Center
                case "Minus":
                   return LeadTwoPosition_val.Minus;    //Minus
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string LeadTwoPosition_val_to_String(LeadTwoPosition_val pKey)
        {
           switch (pKey)
           {
                case LeadTwoPosition_val.Plus:
                   return "Plus";    //Plus
                case LeadTwoPosition_val.Center:
                   return "Center";    //Center
                case LeadTwoPosition_val.Minus:
                   return "Minus";    //Minus
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   LeadTwoOffset
        //   Attr Group:Loop
        //   Alt Display Name:Lead Two Offset (ft)
        //   Description:   Loop top ffset distance from pole
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   80.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LeadTwoOffset;
        [Category("Loop")]
        [Description("LeadTwoOffset")]
        public double LeadTwoOffset
        {
           get { return m_LeadTwoOffset; }
           set { m_LeadTwoOffset = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: WoodPoleDamageOrDecay
   // Mirrors: PPLWoodPoleDamageOrDecay : PPLElement
   //--------------------------------------------------------------------------------------------
   public class WoodPoleDamageOrDecay : ElementBase
   {

      public static string gXMLkey = "WoodPoleDamageOrDecay";
      public override string XMLkey() { return gXMLkey; }

      public WoodPoleDamageOrDecay(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Type = Type_val.Void;
               m_Description = "";
               m_Owner = "<Undefined>";
               m_WidthInInches = 1;
               m_HeightInInches = 1;
               m_DepthInInches = 1;
               m_ShellThicknessInInches = 4;
               m_ReducedCircumference = 1;
               m_EntryWidthInInches = 1;
               m_CoordinateZ = 120;
               m_CoordinateA = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   Type of damage or decay
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Void
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Saw Cut  (Saw Cut)
        //        Mower Cut  (Mower Cut)
        //        Exposed Pocket  (Exposed Pocket)
        //        Enclosed Pocket  (Enclosed Pocket)
        //        Void  (Void)
        //        Heart Rot  (Heart Rot)
        //        Shell Reduction  (Shell Reduction)
        //        Woodpecker Hole  (Woodpecker Hole)
        //        Woodpecker Nest  (Woodpecker Nest)
        public enum Type_val
        {
           [Description("Vehicle Scrape")]
           Vehicle_Scrape,    //Vehicle Scrape
           [Description("Saw Cut")]
           Saw_Cut,    //Saw Cut
           [Description("Mower Cut")]
           Mower_Cut,    //Mower Cut
           [Description("Exposed Pocket")]
           Exposed_Pocket,    //Exposed Pocket
           [Description("Enclosed Pocket")]
           Enclosed_Pocket,    //Enclosed Pocket
           [Description("Void")]
           Void,    //Void
           [Description("Heart Rot")]
           Heart_Rot,    //Heart Rot
           [Description("Shell Reduction")]
           Shell_Reduction,    //Shell Reduction
           [Description("Woodpecker Hole")]
           Woodpecker_Hole,    //Woodpecker Hole
           [Description("Woodpecker Nest")]
           Woodpecker_Nest     //Woodpecker Nest
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Vehicle Scrape":
                   return Type_val.Vehicle_Scrape;    //Vehicle Scrape
                case "Saw Cut":
                   return Type_val.Saw_Cut;    //Saw Cut
                case "Mower Cut":
                   return Type_val.Mower_Cut;    //Mower Cut
                case "Exposed Pocket":
                   return Type_val.Exposed_Pocket;    //Exposed Pocket
                case "Enclosed Pocket":
                   return Type_val.Enclosed_Pocket;    //Enclosed Pocket
                case "Void":
                   return Type_val.Void;    //Void
                case "Heart Rot":
                   return Type_val.Heart_Rot;    //Heart Rot
                case "Shell Reduction":
                   return Type_val.Shell_Reduction;    //Shell Reduction
                case "Woodpecker Hole":
                   return Type_val.Woodpecker_Hole;    //Woodpecker Hole
                case "Woodpecker Nest":
                   return Type_val.Woodpecker_Nest;    //Woodpecker Nest
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Vehicle_Scrape:
                   return "Vehicle Scrape";    //Vehicle Scrape
                case Type_val.Saw_Cut:
                   return "Saw Cut";    //Saw Cut
                case Type_val.Mower_Cut:
                   return "Mower Cut";    //Mower Cut
                case Type_val.Exposed_Pocket:
                   return "Exposed Pocket";    //Exposed Pocket
                case Type_val.Enclosed_Pocket:
                   return "Enclosed Pocket";    //Enclosed Pocket
                case Type_val.Void:
                   return "Void";    //Void
                case Type_val.Heart_Rot:
                   return "Heart Rot";    //Heart Rot
                case Type_val.Shell_Reduction:
                   return "Shell Reduction";    //Shell Reduction
                case Type_val.Woodpecker_Hole:
                   return "Woodpecker Hole";    //Woodpecker Hole
                case Type_val.Woodpecker_Nest:
                   return "Woodpecker Nest";    //Woodpecker Nest
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Brief description of the damage
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   WidthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Width (in)
        //   Description:   The width in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   1.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WidthInInches;
        [Category("Standard")]
        [Description("WidthInInches")]
        public double WidthInInches
        {
           get { return m_WidthInInches; }
           set { m_WidthInInches = value; }
        }



        //   Attr Name:   HeightInInches
        //   Attr Group:Standard
        //   Alt Display Name:Height (in)
        //   Description:   The height in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   1.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_HeightInInches;
        [Category("Standard")]
        [Description("HeightInInches")]
        public double HeightInInches
        {
           get { return m_HeightInInches; }
           set { m_HeightInInches = value; }
        }



        //   Attr Name:   DepthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Depth (in)
        //   Description:   The depth in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   1.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DepthInInches;
        [Category("Standard")]
        [Description("DepthInInches")]
        public double DepthInInches
        {
           get { return m_DepthInInches; }
           set { m_DepthInInches = value; }
        }



        //   Attr Name:   ShellThicknessInInches
        //   Attr Group:Standard
        //   Alt Display Name:Shell Thick (in)
        //   Description:   Shell thickness in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   4.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ShellThicknessInInches;
        [Category("Standard")]
        [Description("ShellThicknessInInches")]
        public double ShellThicknessInInches
        {
           get { return m_ShellThicknessInInches; }
           set { m_ShellThicknessInInches = value; }
        }



        //   Attr Name:   ReducedCircumference
        //   Attr Group:Standard
        //   Alt Display Name:Reduced Circum (in)
        //   Description:   Shell reduced circumference in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   1
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ReducedCircumference;
        [Category("Standard")]
        [Description("ReducedCircumference")]
        public double ReducedCircumference
        {
           get { return m_ReducedCircumference; }
           set { m_ReducedCircumference = value; }
        }



        //   Attr Name:   EntryWidthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Entry Width (in)
        //   Description:   The entry width in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   1.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_EntryWidthInInches;
        [Category("Standard")]
        [Description("EntryWidthInInches")]
        public double EntryWidthInInches
        {
           get { return m_EntryWidthInInches; }
           set { m_EntryWidthInInches = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Location (ft)
        //   Description:   Distance from the butt of the pole to center of damage or decay
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   120.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The rotation angle around the center of the pole
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: CapacityAdjustment
   // Mirrors: PPLCapacityAdjustment : PPLElement
   //--------------------------------------------------------------------------------------------
   public class CapacityAdjustment : ElementBase
   {

      public static string gXMLkey = "CapacityAdjustment";
      public override string XMLkey() { return gXMLkey; }

      public CapacityAdjustment(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "";
               m_Owner = "<Undefined>";
               m_HeightInInches = 1;
               m_CoordinateZ = 120;
               m_CoordinateA = 0;
               m_MomentCapacity = 50000;
               m_BucklingCapacity = 5000;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Brief description of the damage
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   HeightInInches
        //   Attr Group:Standard
        //   Alt Display Name:Height (in)
        //   Description:   The height in inches
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   1.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_HeightInInches;
        [Category("Standard")]
        [Description("HeightInInches")]
        public double HeightInInches
        {
           get { return m_HeightInInches; }
           set { m_HeightInInches = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Location (ft)
        //   Description:   Distance from the butt of the pole to center of damage or decay
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   120.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The rotation angle around the center of the pole
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   MomentCapacity
        //   Attr Group:Standard
        //   Alt Display Name:Moment Cap (ft-lb)
        //   Description:   The  moment capacity
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   50000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MomentCapacity;
        [Category("Standard")]
        [Description("MomentCapacity")]
        public double MomentCapacity
        {
           get { return m_MomentCapacity; }
           set { m_MomentCapacity = value; }
        }



        //   Attr Name:   BucklingCapacity
        //   Attr Group:Standard
        //   Alt Display Name:Buckling Cap (lbs)
        //   Description:   The buckling capacity
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   FLOAT
        //   Default Value:   5000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BucklingCapacity;
        [Category("Standard")]
        [Description("BucklingCapacity")]
        public double BucklingCapacity
        {
           get { return m_BucklingCapacity; }
           set { m_BucklingCapacity = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: MultiPoleStructure
   // Mirrors: PPLMultiPoleStructure : PPLElement
   //--------------------------------------------------------------------------------------------
   public class MultiPoleStructure : ElementBase
   {

      public static string gXMLkey = "MultiPoleStructure";
      public override string XMLkey() { return gXMLkey; }

      public MultiPoleStructure(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Pole_Number = "Unset";
               m_Owner = "Pole";
               m_LineOfLead = 0;
               m_ReportingMode = ReportingMode_val.Active_Leg;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is WoodPole) return true;
         if(pChildCandidate is SteelPole) return true;
         if(pChildCandidate is ConcretePole) return true;
         if(pChildCandidate is CompositePole) return true;
         if(pChildCandidate is Crossarm) return true;
         if(pChildCandidate is LoadCase) return true;
         if(pChildCandidate is LatticeSection) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Pole Number
        //   Attr Group:Standard
        //   Description:   Structure ID
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Pole_Number;
        [Category("Standard")]
        [Description("Pole Number")]
        public string Pole_Number
        {
           get { return m_Pole_Number; }
           set { m_Pole_Number = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   LineOfLead
        //   Attr Group:Standard
        //   Alt Display Name:Line of Lead (°)
        //   Description:   The overall line of lead of the entire pole assembly
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LineOfLead;
        [Category("Standard")]
        [Description("LineOfLead")]
        public double LineOfLead
        {
           get { return m_LineOfLead; }
           set { m_LineOfLead = value; }
        }



        //   Attr Name:   ReportingMode
        //   Attr Group:Standard
        //   Alt Display Name:Reporting Mode
        //   Description:   Reporting Mode
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Active Leg
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Worst Leg  (Worst Leg)
        public enum ReportingMode_val
        {
           [Description("Active Leg")]
           Active_Leg,    //Active Leg
           [Description("Worst Leg")]
           Worst_Leg     //Worst Leg
        }
        private ReportingMode_val m_ReportingMode;
        [Category("Standard")]
        [Description("ReportingMode")]
        public ReportingMode_val ReportingMode
        {
           get
           { return m_ReportingMode; }
           set
           { m_ReportingMode = value; }
        }

        public ReportingMode_val String_to_ReportingMode_val(string pKey)
        {
           switch (pKey)
           {
                case "Active Leg":
                   return ReportingMode_val.Active_Leg;    //Active Leg
                case "Worst Leg":
                   return ReportingMode_val.Worst_Leg;    //Worst Leg
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string ReportingMode_val_to_String(ReportingMode_val pKey)
        {
           switch (pKey)
           {
                case ReportingMode_val.Active_Leg:
                   return "Active Leg";    //Active Leg
                case ReportingMode_val.Worst_Leg:
                   return "Worst Leg";    //Worst Leg
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: LatticeStructure
   // Mirrors: PPLLatticeStructure : PPLElement
   //--------------------------------------------------------------------------------------------
   public class LatticeStructure : ElementBase
   {

      public static string gXMLkey = "LatticeStructure";
      public override string XMLkey() { return gXMLkey; }

      public LatticeStructure(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Pole_Number = "Unset";
               m_Owner = "Pole";
               m_LineOfLead = 0;
               m_NodeRenderDiam = 12;
               m_BeamRenderDiam = 6;
               m_RenderLoads = false;
               m_GravitationalAccellerationX = 0;
               m_GravitationalAccellerationY = 0;
               m_GravitationalAccellerationZ = 2.68116666666667;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is LatticeSection) return true;
         if(pChildCandidate is LoadCase) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Pole Number
        //   Attr Group:Standard
        //   Description:   Structure ID
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Unset
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Pole_Number;
        [Category("Standard")]
        [Description("Pole Number")]
        public string Pole_Number
        {
           get { return m_Pole_Number; }
           set { m_Pole_Number = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Pole
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   LineOfLead
        //   Attr Group:Standard
        //   Alt Display Name:Line of Lead (°)
        //   Description:   The overall line of lead of the entire pole assembly
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_LineOfLead;
        [Category("Standard")]
        [Description("LineOfLead")]
        public double LineOfLead
        {
           get { return m_LineOfLead; }
           set { m_LineOfLead = value; }
        }



        //   Attr Name:   NodeRenderDiam
        //   Attr Group:Standard
        //   Alt Display Name:Node Render (in)
        //   Description:   Node render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_NodeRenderDiam;
        [Category("Standard")]
        [Description("NodeRenderDiam")]
        public double NodeRenderDiam
        {
           get { return m_NodeRenderDiam; }
           set { m_NodeRenderDiam = value; }
        }



        //   Attr Name:   BeamRenderDiam
        //   Attr Group:Standard
        //   Alt Display Name:Beam Render (in)
        //   Description:   Beam render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   6
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BeamRenderDiam;
        [Category("Standard")]
        [Description("BeamRenderDiam")]
        public double BeamRenderDiam
        {
           get { return m_BeamRenderDiam; }
           set { m_BeamRenderDiam = value; }
        }



        //   Attr Name:   RenderLoads
        //   Attr Group:Standard
        //   Alt Display Name:Render Loads
        //   Description:   Render loads
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_RenderLoads;
        [Category("Standard")]
        [Description("RenderLoads")]
        public bool RenderLoads
        {
           get { return m_RenderLoads; }
           set { m_RenderLoads = value; }
        }



        //   Attr Name:   GravitationalAccellerationX
        //   Attr Group:Gravity
        //   Alt Display Name:Grav Accel X (ft/s/s)
        //   Description:   GravitationalAccellerationX
        //   Displayed Units:   store as INCHES PERSEC PERSEC display as FEET PERSEC PERSEC or METERS PERSEC PERSEC
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_GravitationalAccellerationX;
        [Category("Gravity")]
        [Description("GravitationalAccellerationX")]
        public double GravitationalAccellerationX
        {
           get { return m_GravitationalAccellerationX; }
           set { m_GravitationalAccellerationX = value; }
        }



        //   Attr Name:   GravitationalAccellerationY
        //   Attr Group:Gravity
        //   Alt Display Name:Grav Accel Y (ft/s/s)
        //   Description:   GravitationalAccellerationY
        //   Displayed Units:   store as INCHES PERSEC PERSEC display as FEET PERSEC PERSEC or METERS PERSEC PERSEC
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_GravitationalAccellerationY;
        [Category("Gravity")]
        [Description("GravitationalAccellerationY")]
        public double GravitationalAccellerationY
        {
           get { return m_GravitationalAccellerationY; }
           set { m_GravitationalAccellerationY = value; }
        }



        //   Attr Name:   GravitationalAccellerationZ
        //   Attr Group:Gravity
        //   Alt Display Name:Grav Accel Z (ft/s/s)
        //   Description:   GravitationalAccellerationZ
        //   Displayed Units:   store as INCHES PERSEC PERSEC display as FEET PERSEC PERSEC or METERS PERSEC PERSEC
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   FLOAT
        //   Default Value:   2.68116666666667
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_GravitationalAccellerationZ;
        [Category("Gravity")]
        [Description("GravitationalAccellerationZ")]
        public double GravitationalAccellerationZ
        {
           get { return m_GravitationalAccellerationZ; }
           set { m_GravitationalAccellerationZ = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: LatticeSection
   // Mirrors: PPLLatticeSection : PPLElement
   //--------------------------------------------------------------------------------------------
   public class LatticeSection : ElementBase
   {

      public static string gXMLkey = "LatticeSection";
      public override string XMLkey() { return gXMLkey; }

      public LatticeSection(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Section";
               m_Name = "";
               m_CoordinateZ = 0;
               m_Width = 0;
               m_Depth = 0;
               m_Height = 0;
               m_OverrideRendering = false;
               m_NodeRenderDiam = 20;
               m_BeamRenderDiam = 12;
               m_RenderLoads = false;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is LatticeGroup) return true;
         if(pChildCandidate is Node) return true;
         if(pChildCandidate is Beam) return true;
         if(pChildCandidate is Material) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the section
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Section
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the section
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Z (ft)
        //   Description:   Section Z relative to groundline
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   Width
        //   Attr Group:Standard
        //   Alt Display Name:Width X (ft)
        //   Description:   Distance between min and max X
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Width;
        [Category("Standard")]
        [Description("Width")]
        public double Width
        {
           get { return m_Width; }
           set { m_Width = value; }
        }



        //   Attr Name:   Depth
        //   Attr Group:Standard
        //   Alt Display Name:Depth Y (ft)
        //   Description:   Distance between min and max Y
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Depth;
        [Category("Standard")]
        [Description("Depth")]
        public double Depth
        {
           get { return m_Depth; }
           set { m_Depth = value; }
        }



        //   Attr Name:   Height
        //   Attr Group:Standard
        //   Alt Display Name:Height Z (ft)
        //   Description:   Distance between min and max Z
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Height;
        [Category("Standard")]
        [Description("Height")]
        public double Height
        {
           get { return m_Height; }
           set { m_Height = value; }
        }



        //   Attr Name:   OverrideRendering
        //   Attr Group:Rendering
        //   Alt Display Name:Override Rendering
        //   Description:   Indicates if the rendering is controlled by this section
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverrideRendering;
        [Category("Rendering")]
        [Description("OverrideRendering")]
        public bool OverrideRendering
        {
           get { return m_OverrideRendering; }
           set { m_OverrideRendering = value; }
        }



        //   Attr Name:   NodeRenderDiam
        //   Attr Group:Rendering
        //   Alt Display Name:Node Render (in)
        //   Description:   Node render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   20
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_NodeRenderDiam;
        [Category("Rendering")]
        [Description("NodeRenderDiam")]
        public double NodeRenderDiam
        {
           get { return m_NodeRenderDiam; }
           set { m_NodeRenderDiam = value; }
        }



        //   Attr Name:   BeamRenderDiam
        //   Attr Group:Rendering
        //   Alt Display Name:Beam Render (in)
        //   Description:   Beam render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BeamRenderDiam;
        [Category("Rendering")]
        [Description("BeamRenderDiam")]
        public double BeamRenderDiam
        {
           get { return m_BeamRenderDiam; }
           set { m_BeamRenderDiam = value; }
        }



        //   Attr Name:   RenderLoads
        //   Attr Group:Rendering
        //   Alt Display Name:Render Loads
        //   Description:   Render loads
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_RenderLoads;
        [Category("Rendering")]
        [Description("RenderLoads")]
        public bool RenderLoads
        {
           get { return m_RenderLoads; }
           set { m_RenderLoads = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: LatticeGroup
   // Mirrors: PPLLatticeGroup : PPLElement
   //--------------------------------------------------------------------------------------------
   public class LatticeGroup : ElementBase
   {

      public static string gXMLkey = "LatticeGroup";
      public override string XMLkey() { return gXMLkey; }

      public LatticeGroup(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Group";
               m_Name = "<tbd>";
               m_Width = 0;
               m_Depth = 0;
               m_Height = 0;
               m_OverrideRendering = false;
               m_NodeRenderDiam = 20;
               m_BeamRenderDiam = 12;
               m_RenderLoads = false;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Node) return true;
         if(pChildCandidate is Beam) return true;
         if(pChildCandidate is Material) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the group
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Group
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the group
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <tbd>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   Width
        //   Attr Group:Standard
        //   Alt Display Name:Width X (ft)
        //   Description:   Distance between min and max X
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Width;
        [Category("Standard")]
        [Description("Width")]
        public double Width
        {
           get { return m_Width; }
           set { m_Width = value; }
        }



        //   Attr Name:   Depth
        //   Attr Group:Standard
        //   Alt Display Name:Depth Y (ft)
        //   Description:   Distance between min and max Y
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Depth;
        [Category("Standard")]
        [Description("Depth")]
        public double Depth
        {
           get { return m_Depth; }
           set { m_Depth = value; }
        }



        //   Attr Name:   Height
        //   Attr Group:Standard
        //   Alt Display Name:Height Z (ft)
        //   Description:   Distance between min and max Z
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Height;
        [Category("Standard")]
        [Description("Height")]
        public double Height
        {
           get { return m_Height; }
           set { m_Height = value; }
        }



        //   Attr Name:   OverrideRendering
        //   Attr Group:Rendering
        //   Alt Display Name:Override Rendering
        //   Description:   Indicates if the rendering is controlled by this section
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverrideRendering;
        [Category("Rendering")]
        [Description("OverrideRendering")]
        public bool OverrideRendering
        {
           get { return m_OverrideRendering; }
           set { m_OverrideRendering = value; }
        }



        //   Attr Name:   NodeRenderDiam
        //   Attr Group:Rendering
        //   Alt Display Name:Node Render (in)
        //   Description:   Node render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   20
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_NodeRenderDiam;
        [Category("Rendering")]
        [Description("NodeRenderDiam")]
        public double NodeRenderDiam
        {
           get { return m_NodeRenderDiam; }
           set { m_NodeRenderDiam = value; }
        }



        //   Attr Name:   BeamRenderDiam
        //   Attr Group:Rendering
        //   Alt Display Name:Beam Render (in)
        //   Description:   Beam render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BeamRenderDiam;
        [Category("Rendering")]
        [Description("BeamRenderDiam")]
        public double BeamRenderDiam
        {
           get { return m_BeamRenderDiam; }
           set { m_BeamRenderDiam = value; }
        }



        //   Attr Name:   RenderLoads
        //   Attr Group:Rendering
        //   Alt Display Name:Render Loads
        //   Description:   Render loads
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_RenderLoads;
        [Category("Rendering")]
        [Description("RenderLoads")]
        public bool RenderLoads
        {
           get { return m_RenderLoads; }
           set { m_RenderLoads = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Material
   // Mirrors: PPLMaterial : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Material : ElementBase
   {

      public static string gXMLkey = "Material";
      public override string XMLkey() { return gXMLkey; }

      public Material(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Material";
               m_Name = "<tbd>";
               m_YoungsModulus = 20000000;
               m_PoissonsRatio = 0.3;
               m_Density = 0.0347222222222222;
               m_ThermalCoefficient = 1.06E-05;
               m_ShearAreaY = 8;
               m_ShearAreaZ = 8;
               m_ShearModulus = 8000000;
               m_ShearStrengthY = 45000;
               m_ShearStrengthZ = 45000;
               m_BucklingStrength = 45000;
               m_TensionStrength = 45000;
               m_MomentCapacityY = 2000;
               m_MomentCapacityZ = 2000;
               m_MomentCapacityX = 2000;
               m_Area = 11;
               m_DimensionY = 6;
               m_DimensionZ = 6;
               m_IcePerimiter = 6;
               m_WindArea = 11;
               m_Iyy = 400;
               m_Izz = 400;
               m_Jxx = 200;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the material
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Material
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the material
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <tbd>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   YoungsModulus
        //   Attr Group:Constants
        //   Alt Display Name:Modulus of Elasticity (psi)
        //   Description:   YoungsModulus
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   20000000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_YoungsModulus;
        [Category("Constants")]
        [Description("YoungsModulus")]
        public double YoungsModulus
        {
           get { return m_YoungsModulus; }
           set { m_YoungsModulus = value; }
        }



        //   Attr Name:   PoissonsRatio
        //   Attr Group:Constants
        //   Alt Display Name:Poisson's Ratio
        //   Description:   Poisson's Ratio
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.0####
        //   Attribute Type:   FLOAT
        //   Default Value:   0.3
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_PoissonsRatio;
        [Category("Constants")]
        [Description("PoissonsRatio")]
        public double PoissonsRatio
        {
           get { return m_PoissonsRatio; }
           set { m_PoissonsRatio = value; }
        }



        //   Attr Name:   Density
        //   Attr Group:Constants
        //   Alt Display Name:Density (lb/ft^3)
        //   Description:   Density for the material in lbs per cubic inch
        //   Displayed Units:   store as POUNDS PER CUBIC INCH display as POUNDS PER CUBIC FOOT or KILOGRAMS PER CUBIC METER
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0347222222222222
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Density;
        [Category("Constants")]
        [Description("Density")]
        public double Density
        {
           get { return m_Density; }
           set { m_Density = value; }
        }



        //   Attr Name:   ThermalCoefficient
        //   Attr Group:Constants
        //   Alt Display Name:Thermal Coef ((in/in)/°f)
        //   Description:   ThermalCoefficient
        //   Displayed Units:   store as THERMAL COEFFICIENT
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0000106
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ThermalCoefficient;
        [Category("Constants")]
        [Description("ThermalCoefficient")]
        public double ThermalCoefficient
        {
           get { return m_ThermalCoefficient; }
           set { m_ThermalCoefficient = value; }
        }



        //   Attr Name:   ShearAreaY
        //   Attr Group:Shear
        //   Alt Display Name:Shear Area Y (in^2)
        //   Description:   Shear Area Y
        //   Displayed Units:   store as IN2 display as IN2 or CM2
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   8
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ShearAreaY;
        [Category("Shear")]
        [Description("ShearAreaY")]
        public double ShearAreaY
        {
           get { return m_ShearAreaY; }
           set { m_ShearAreaY = value; }
        }



        //   Attr Name:   ShearAreaZ
        //   Attr Group:Shear
        //   Alt Display Name:Shear Area Z (in^2)
        //   Description:   Shear Area Z
        //   Displayed Units:   store as IN2 display as IN2 or CM2
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00##
        //   Attribute Type:   FLOAT
        //   Default Value:   8
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ShearAreaZ;
        [Category("Shear")]
        [Description("ShearAreaZ")]
        public double ShearAreaZ
        {
           get { return m_ShearAreaZ; }
           set { m_ShearAreaZ = value; }
        }



        //   Attr Name:   ShearModulus
        //   Attr Group:Shear
        //   Alt Display Name:Shear Modulus (psi)
        //   Description:   Shear Modulus
        //   Displayed Units:   store as PSI display as PSI or KILOPASCAL
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   8000000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ShearModulus;
        [Category("Shear")]
        [Description("ShearModulus")]
        public double ShearModulus
        {
           get { return m_ShearModulus; }
           set { m_ShearModulus = value; }
        }



        //   Attr Name:   ShearStrengthY
        //   Attr Group:Shear
        //   Alt Display Name:Shear Strength Y (lbs)
        //   Description:   The shear strength of the beam
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   45000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ShearStrengthY;
        [Category("Shear")]
        [Description("ShearStrengthY")]
        public double ShearStrengthY
        {
           get { return m_ShearStrengthY; }
           set { m_ShearStrengthY = value; }
        }



        //   Attr Name:   ShearStrengthZ
        //   Attr Group:Shear
        //   Alt Display Name:Shear Strength Z (lbs)
        //   Description:   The shear strength of the beam
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   45000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ShearStrengthZ;
        [Category("Shear")]
        [Description("ShearStrengthZ")]
        public double ShearStrengthZ
        {
           get { return m_ShearStrengthZ; }
           set { m_ShearStrengthZ = value; }
        }



        //   Attr Name:   BucklingStrength
        //   Attr Group:Capacity
        //   Alt Display Name:Buckling Strength (lbs)
        //   Description:   The buckling strength of the beam
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   45000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BucklingStrength;
        [Category("Capacity")]
        [Description("BucklingStrength")]
        public double BucklingStrength
        {
           get { return m_BucklingStrength; }
           set { m_BucklingStrength = value; }
        }



        //   Attr Name:   TensionStrength
        //   Attr Group:Capacity
        //   Alt Display Name:Tensile Strength (lbs)
        //   Description:   The tensile strength of the beam
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   45000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_TensionStrength;
        [Category("Capacity")]
        [Description("TensionStrength")]
        public double TensionStrength
        {
           get { return m_TensionStrength; }
           set { m_TensionStrength = value; }
        }



        //   Attr Name:   MomentCapacityY
        //   Attr Group:Capacity
        //   Alt Display Name:Moment Capacity Y (ft-lbs)
        //   Description:   Total allowable bending moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   2000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MomentCapacityY;
        [Category("Capacity")]
        [Description("MomentCapacityY")]
        public double MomentCapacityY
        {
           get { return m_MomentCapacityY; }
           set { m_MomentCapacityY = value; }
        }



        //   Attr Name:   MomentCapacityZ
        //   Attr Group:Capacity
        //   Alt Display Name:Moment Capacity Z (ft-lbs)
        //   Description:   Total allowable bending moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   2000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MomentCapacityZ;
        [Category("Capacity")]
        [Description("MomentCapacityZ")]
        public double MomentCapacityZ
        {
           get { return m_MomentCapacityZ; }
           set { m_MomentCapacityZ = value; }
        }



        //   Attr Name:   MomentCapacityX
        //   Attr Group:Capacity
        //   Alt Display Name:Torque Capacity (ft-lbs)
        //   Description:   Total allowable bending moment
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   2000
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MomentCapacityX;
        [Category("Capacity")]
        [Description("MomentCapacityX")]
        public double MomentCapacityX
        {
           get { return m_MomentCapacityX; }
           set { m_MomentCapacityX = value; }
        }



        //   Attr Name:   Area
        //   Attr Group:Dimensions
        //   Alt Display Name:Area (in^2)
        //   Description:   Cross sectional area
        //   Displayed Units:   store as IN2 display as IN2 or CM2
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   11
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Area;
        [Category("Dimensions")]
        [Description("Area")]
        public double Area
        {
           get { return m_Area; }
           set { m_Area = value; }
        }



        //   Attr Name:   DimensionY
        //   Attr Group:Dimensions
        //   Alt Display Name:Dimension Y (in)
        //   Description:   Dimension in the Y axis
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   6
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DimensionY;
        [Category("Dimensions")]
        [Description("DimensionY")]
        public double DimensionY
        {
           get { return m_DimensionY; }
           set { m_DimensionY = value; }
        }



        //   Attr Name:   DimensionZ
        //   Attr Group:Dimensions
        //   Alt Display Name:Dimension Z (in)
        //   Description:   Dimension in the Z axis
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   6
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_DimensionZ;
        [Category("Dimensions")]
        [Description("DimensionZ")]
        public double DimensionZ
        {
           get { return m_DimensionZ; }
           set { m_DimensionZ = value; }
        }



        //   Attr Name:   IcePerimiter
        //   Attr Group:Dimensions
        //   Alt Display Name:Ice Perimiter (in)
        //   Description:   Ice accumulation perimiter (effective)
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   6
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_IcePerimiter;
        [Category("Dimensions")]
        [Description("IcePerimiter")]
        public double IcePerimiter
        {
           get { return m_IcePerimiter; }
           set { m_IcePerimiter = value; }
        }



        //   Attr Name:   WindArea
        //   Attr Group:Dimensions
        //   Alt Display Name:Wind Area (in^2/ft)
        //   Description:   Wind area per unit length
        //   Displayed Units:   store as SQINPERIN display as SQINPERFOOT or SQCMPERMETER
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   11
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WindArea;
        [Category("Dimensions")]
        [Description("WindArea")]
        public double WindArea
        {
           get { return m_WindArea; }
           set { m_WindArea = value; }
        }



        //   Attr Name:   Iyy
        //   Attr Group:Moments
        //   Alt Display Name:Ixx - 2nd MOA (in^4)
        //   Description:   Second moment of area in the horizontal axis
        //   Displayed Units:   store as IN4 display as IN4 or CM4
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   400
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Iyy;
        [Category("Moments")]
        [Description("Iyy")]
        public double Iyy
        {
           get { return m_Iyy; }
           set { m_Iyy = value; }
        }



        //   Attr Name:   Izz
        //   Attr Group:Moments
        //   Alt Display Name:Izz - 2nd MOA (in^4)
        //   Description:   Second moment of area in the vertical axis
        //   Displayed Units:   store as IN4 display as IN4 or CM4
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   400
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Izz;
        [Category("Moments")]
        [Description("Izz")]
        public double Izz
        {
           get { return m_Izz; }
           set { m_Izz = value; }
        }



        //   Attr Name:   Jxx
        //   Attr Group:Moments
        //   Alt Display Name:Jyy - 2nd MOA (in^4)
        //   Description:   Second moment of area in the radial axis
        //   Displayed Units:   store as IN4 display as IN4 or CM4
        //   User Level Required:   Administrative access only
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   200
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Jxx;
        [Category("Moments")]
        [Description("Jxx")]
        public double Jxx
        {
           get { return m_Jxx; }
           set { m_Jxx = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Node
   // Mirrors: PPLNode : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Node : ElementBase
   {

      public static string gXMLkey = "Node";
      public override string XMLkey() { return gXMLkey; }

      public Node(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Node";
               m_Name = "<tbd>";
               m_CoordinateX = 0;
               m_CoordinateY = 0;
               m_CoordinateZ = 0;
               m_NodeRadius = 0;
               m_ShearStrength = 45000;
               m_MergeNode = false;
               m_MergeTollerance = 2;
               m_OverrideRendering = false;
               m_NodeRenderDiam = 20;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is NodeConstraint) return true;
         if(pChildCandidate is Insulator) return true;
         if(pChildCandidate is NodeLoad) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the node
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Node
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the node
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <tbd>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Alt Display Name:X Coord (ft)
        //   Description:   Node X relative to center
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   CoordinateY
        //   Attr Group:Standard
        //   Alt Display Name:Y Coord (ft)
        //   Description:   Node Y relative to center
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateY;
        [Category("Standard")]
        [Description("CoordinateY")]
        public double CoordinateY
        {
           get { return m_CoordinateY; }
           set { m_CoordinateY = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Z Coord (ft)
        //   Description:   Node Z relative to section
        //   Displayed Units:   store as INCHES display as FEET or METERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   NodeRadius
        //   Attr Group:Parameters
        //   Alt Display Name:Node Radius (in)
        //   Description:   Node radius (plate size)
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_NodeRadius;
        [Category("Parameters")]
        [Description("NodeRadius")]
        public double NodeRadius
        {
           get { return m_NodeRadius; }
           set { m_NodeRadius = value; }
        }



        //   Attr Name:   ShearStrength
        //   Attr Group:Parameters
        //   Alt Display Name:Shear Strength (lbs)
        //   Description:   The shear strength of the node
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0
        //   Attribute Type:   FLOAT
        //   Default Value:   45000
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_ShearStrength;
        [Category("Parameters")]
        [Description("ShearStrength")]
        public double ShearStrength
        {
           get { return m_ShearStrength; }
           set { m_ShearStrength = value; }
        }



        //   Attr Name:   MergeNode
        //   Attr Group:Merge Nodes
        //   Alt Display Name:Merge Nodes
        //   Description:   Indicates if the node is to be merged with proximal nodes
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   Yes
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_MergeNode;
        [Category("Merge Nodes")]
        [Description("MergeNode")]
        public bool MergeNode
        {
           get { return m_MergeNode; }
           set { m_MergeNode = value; }
        }



        //   Attr Name:   MergeTollerance
        //   Attr Group:Merge Nodes
        //   Alt Display Name:Merge Tollerance (in)
        //   Description:   Max distance node is to be merged with proximal nodes
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.000
        //   Attribute Type:   FLOAT
        //   Default Value:   2
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MergeTollerance;
        [Category("Merge Nodes")]
        [Description("MergeTollerance")]
        public double MergeTollerance
        {
           get { return m_MergeTollerance; }
           set { m_MergeTollerance = value; }
        }



        //   Attr Name:   OverrideRendering
        //   Attr Group:Rendering
        //   Alt Display Name:Override Rendering
        //   Description:   Indicates if the rendering is controlled by this section
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverrideRendering;
        [Category("Rendering")]
        [Description("OverrideRendering")]
        public bool OverrideRendering
        {
           get { return m_OverrideRendering; }
           set { m_OverrideRendering = value; }
        }



        //   Attr Name:   NodeRenderDiam
        //   Attr Group:Rendering
        //   Alt Display Name:Node Render (in)
        //   Description:   Node render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   20
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_NodeRenderDiam;
        [Category("Rendering")]
        [Description("NodeRenderDiam")]
        public double NodeRenderDiam
        {
           get { return m_NodeRenderDiam; }
           set { m_NodeRenderDiam = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: NodeJunction
   // Mirrors: PPLJunctionNode : PPLElement
   //--------------------------------------------------------------------------------------------
   public class NodeJunction : ElementBase
   {

      public static string gXMLkey = "NodeJunction";
      public override string XMLkey() { return gXMLkey; }

      public NodeJunction(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Junction";
               m_Owner = "<Undefined>";
               m_Node = "<node>";
               m_CoordinateZ = 300;
               m_CoordinateA = 0;
               m_Side = Side_val.Inline;
               m_CoordinateX = 0;
               m_WidthInInches = 3;
               m_Weight = 1;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the insulator.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Junction
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Owner
        //   Attr Group:Standard
        //   Description:   Owner
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <Undefined>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Owner;
        [Category("Standard")]
        [Description("Owner")]
        public string Owner
        {
           get { return m_Owner; }
           set { m_Owner = value; }
        }



        //   Attr Name:   Node
        //   Attr Group:Standard
        //   Description:   Node
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <node>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private string m_Node;
        [Category("Standard")]
        [Description("Node")]
        public string Node
        {
           get { return m_Node; }
           set { m_Node = value; }
        }



        //   Attr Name:   CoordinateZ
        //   Attr Group:Standard
        //   Alt Display Name:Install Height (ft)
        //   Description:   The Z coordinate relative to the parent.  This value is frequently set by SnapToParent
        //   Displayed Units:   store as HEIGHT from BUTT in INCHES display as HEIGHT from GL in FEET or METERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   300.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateZ;
        [Category("Standard")]
        [Description("CoordinateZ")]
        public double CoordinateZ
        {
           get { return m_CoordinateZ; }
           set { m_CoordinateZ = value; }
        }



        //   Attr Name:   CoordinateA
        //   Attr Group:Standard
        //   Alt Display Name:Rotation (°)
        //   Description:   The rotation of the insulator / span holder relative to its parent.  If the orientation is non-zero the stalk with lean alone this axis
        //   Displayed Units:   store as RADIANS display as DEGREES
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERA
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateA;
        [Category("Standard")]
        [Description("CoordinateA")]
        public double CoordinateA
        {
           get { return m_CoordinateA; }
           set { m_CoordinateA = value; }
        }



        //   Attr Name:   Side
        //   Attr Group:Standard
        //   Alt Display Name:Position
        //   Description:   The junction position.
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Inline
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        //   Enum Values:
        //        Street  (Street)
        //        Field  (Field)
        //        Inline  (Inline)
        //        Front  (Front)
        //        Back  (Back)
        //        Both  (Both)
        public enum Side_val
        {
           [Description("Tip")]
           Tip,    //Tip
           [Description("Street")]
           Street,    //Street
           [Description("Field")]
           Field,    //Field
           [Description("Inline")]
           Inline,    //Inline
           [Description("Front")]
           Front,    //Front
           [Description("Back")]
           Back,    //Back
           [Description("Both")]
           Both     //Both
        }
        private Side_val m_Side;
        [Category("Standard")]
        [Description("Side")]
        public Side_val Side
        {
           get
           { return m_Side; }
           set
           { m_Side = value; }
        }

        public Side_val String_to_Side_val(string pKey)
        {
           switch (pKey)
           {
                case "Tip":
                   return Side_val.Tip;    //Tip
                case "Street":
                   return Side_val.Street;    //Street
                case "Field":
                   return Side_val.Field;    //Field
                case "Inline":
                   return Side_val.Inline;    //Inline
                case "Front":
                   return Side_val.Front;    //Front
                case "Back":
                   return Side_val.Back;    //Back
                case "Both":
                   return Side_val.Both;    //Both
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Side_val_to_String(Side_val pKey)
        {
           switch (pKey)
           {
                case Side_val.Tip:
                   return "Tip";    //Tip
                case Side_val.Street:
                   return "Street";    //Street
                case Side_val.Field:
                   return "Field";    //Field
                case Side_val.Inline:
                   return "Inline";    //Inline
                case Side_val.Front:
                   return "Front";    //Front
                case Side_val.Back:
                   return "Back";    //Back
                case Side_val.Both:
                   return "Both";    //Both
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   CoordinateX
        //   Attr Group:Standard
        //   Alt Display Name:Horizontal Offset (in)
        //   Description:   Distance from the center of the parent.  In the case of a crossarm this is the position along the arm.  In the case of poles this is typically set by SnapToParent
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERX
        //   Default Value:   0.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   No
        private double m_CoordinateX;
        [Category("Standard")]
        [Description("CoordinateX")]
        public double CoordinateX
        {
           get { return m_CoordinateX; }
           set { m_CoordinateX = value; }
        }



        //   Attr Name:   WidthInInches
        //   Attr Group:Standard
        //   Alt Display Name:Unit Width (in)
        //   Description:   The effective width for wind area of the junction bolt
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   3.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_WidthInInches;
        [Category("Standard")]
        [Description("WidthInInches")]
        public double WidthInInches
        {
           get { return m_WidthInInches; }
           set { m_WidthInInches = value; }
        }



        //   Attr Name:   Weight
        //   Attr Group:Standard
        //   Alt Display Name:Unit Weight (lbs)
        //   Description:   Weight of the junction in pounds
        //   Displayed Units:   store as POUNDS display as POUNDS or KILOGRAMS
        //   User Level Required:   All user levels may access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   1.00
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Weight;
        [Category("Standard")]
        [Description("Weight")]
        public double Weight
        {
           get { return m_Weight; }
           set { m_Weight = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: NodeConstraint
   // Mirrors: PPLNodeConstraint : PPLElement
   //--------------------------------------------------------------------------------------------
   public class NodeConstraint : ElementBase
   {

      public static string gXMLkey = "NodeConstraint";
      public override string XMLkey() { return gXMLkey; }

      public NodeConstraint(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Constraint";
               m_Name = "";
               m_LateralConstraints = LateralConstraints_val.X_Y_Z;
               m_RotationConstraints = RotationConstraints_val.XX_YY_ZZ;
               m_RotationHinges = RotationHinges_val.None;
               m_SettleHeaveX = 0;
               m_SettleHeaveY = 0;
               m_SettleHeaveZ = 0;
               m_RackingXX = 0;
               m_RackingYY = 0;
               m_RackingZZ = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the constraint
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Constraint
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the constraint
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   LateralConstraints
        //   Attr Group:Standard
        //   Alt Display Name:Lateral Constraints
        //   Description:   Lateral Constraints
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   X,Y,Z
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        X,Y,Z  (X,Y,Z)
        //        X  (X)
        //        Y  (Y)
        //        Z  (Z)
        //        X,Y  (X,Y)
        //        X,Z  (X,Z)
        //        Y,Z  (Y,Z)
        public enum LateralConstraints_val
        {
           [Description("None")]
           None,    //None
           [Description("X,Y,Z")]
           X_Y_Z,    //X,Y,Z
           [Description("X")]
           X,    //X
           [Description("Y")]
           Y,    //Y
           [Description("Z")]
           Z,    //Z
           [Description("X,Y")]
           X_Y,    //X,Y
           [Description("X,Z")]
           X_Z,    //X,Z
           [Description("Y,Z")]
           Y_Z     //Y,Z
        }
        private LateralConstraints_val m_LateralConstraints;
        [Category("Standard")]
        [Description("LateralConstraints")]
        public LateralConstraints_val LateralConstraints
        {
           get
           { return m_LateralConstraints; }
           set
           { m_LateralConstraints = value; }
        }

        public LateralConstraints_val String_to_LateralConstraints_val(string pKey)
        {
           switch (pKey)
           {
                case "None":
                   return LateralConstraints_val.None;    //None
                case "X,Y,Z":
                   return LateralConstraints_val.X_Y_Z;    //X,Y,Z
                case "X":
                   return LateralConstraints_val.X;    //X
                case "Y":
                   return LateralConstraints_val.Y;    //Y
                case "Z":
                   return LateralConstraints_val.Z;    //Z
                case "X,Y":
                   return LateralConstraints_val.X_Y;    //X,Y
                case "X,Z":
                   return LateralConstraints_val.X_Z;    //X,Z
                case "Y,Z":
                   return LateralConstraints_val.Y_Z;    //Y,Z
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string LateralConstraints_val_to_String(LateralConstraints_val pKey)
        {
           switch (pKey)
           {
                case LateralConstraints_val.None:
                   return "None";    //None
                case LateralConstraints_val.X_Y_Z:
                   return "X,Y,Z";    //X,Y,Z
                case LateralConstraints_val.X:
                   return "X";    //X
                case LateralConstraints_val.Y:
                   return "Y";    //Y
                case LateralConstraints_val.Z:
                   return "Z";    //Z
                case LateralConstraints_val.X_Y:
                   return "X,Y";    //X,Y
                case LateralConstraints_val.X_Z:
                   return "X,Z";    //X,Z
                case LateralConstraints_val.Y_Z:
                   return "Y,Z";    //Y,Z
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   RotationConstraints
        //   Attr Group:Standard
        //   Alt Display Name:Rotation Constraints
        //   Description:   Rotation Constraints
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   XX,YY,ZZ
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        XX,YY,ZZ  (XX,YY,ZZ)
        //        XX  (XX)
        //        YY  (YY)
        //        ZZ  (ZZ)
        //        XX,YY  (XX,YY)
        //        XX,ZZ  (XX,ZZ)
        //        YY,ZZ  (YY,ZZ)
        public enum RotationConstraints_val
        {
           [Description("None")]
           None,    //None
           [Description("XX,YY,ZZ")]
           XX_YY_ZZ,    //XX,YY,ZZ
           [Description("XX")]
           XX,    //XX
           [Description("YY")]
           YY,    //YY
           [Description("ZZ")]
           ZZ,    //ZZ
           [Description("XX,YY")]
           XX_YY,    //XX,YY
           [Description("XX,ZZ")]
           XX_ZZ,    //XX,ZZ
           [Description("YY,ZZ")]
           YY_ZZ     //YY,ZZ
        }
        private RotationConstraints_val m_RotationConstraints;
        [Category("Standard")]
        [Description("RotationConstraints")]
        public RotationConstraints_val RotationConstraints
        {
           get
           { return m_RotationConstraints; }
           set
           { m_RotationConstraints = value; }
        }

        public RotationConstraints_val String_to_RotationConstraints_val(string pKey)
        {
           switch (pKey)
           {
                case "None":
                   return RotationConstraints_val.None;    //None
                case "XX,YY,ZZ":
                   return RotationConstraints_val.XX_YY_ZZ;    //XX,YY,ZZ
                case "XX":
                   return RotationConstraints_val.XX;    //XX
                case "YY":
                   return RotationConstraints_val.YY;    //YY
                case "ZZ":
                   return RotationConstraints_val.ZZ;    //ZZ
                case "XX,YY":
                   return RotationConstraints_val.XX_YY;    //XX,YY
                case "XX,ZZ":
                   return RotationConstraints_val.XX_ZZ;    //XX,ZZ
                case "YY,ZZ":
                   return RotationConstraints_val.YY_ZZ;    //YY,ZZ
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string RotationConstraints_val_to_String(RotationConstraints_val pKey)
        {
           switch (pKey)
           {
                case RotationConstraints_val.None:
                   return "None";    //None
                case RotationConstraints_val.XX_YY_ZZ:
                   return "XX,YY,ZZ";    //XX,YY,ZZ
                case RotationConstraints_val.XX:
                   return "XX";    //XX
                case RotationConstraints_val.YY:
                   return "YY";    //YY
                case RotationConstraints_val.ZZ:
                   return "ZZ";    //ZZ
                case RotationConstraints_val.XX_YY:
                   return "XX,YY";    //XX,YY
                case RotationConstraints_val.XX_ZZ:
                   return "XX,ZZ";    //XX,ZZ
                case RotationConstraints_val.YY_ZZ:
                   return "YY,ZZ";    //YY,ZZ
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   RotationHinges
        //   Attr Group:Standard
        //   Alt Display Name:Rotation Hinges
        //   Description:   Rotation Hinges
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   None
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        XX,YY,ZZ  (XX,YY,ZZ)
        //        XX  (XX)
        //        YY  (YY)
        //        ZZ  (ZZ)
        //        XX,YY  (XX,YY)
        //        XX,ZZ  (XX,ZZ)
        //        YY,ZZ  (YY,ZZ)
        public enum RotationHinges_val
        {
           [Description("None")]
           None,    //None
           [Description("XX,YY,ZZ")]
           XX_YY_ZZ,    //XX,YY,ZZ
           [Description("XX")]
           XX,    //XX
           [Description("YY")]
           YY,    //YY
           [Description("ZZ")]
           ZZ,    //ZZ
           [Description("XX,YY")]
           XX_YY,    //XX,YY
           [Description("XX,ZZ")]
           XX_ZZ,    //XX,ZZ
           [Description("YY,ZZ")]
           YY_ZZ     //YY,ZZ
        }
        private RotationHinges_val m_RotationHinges;
        [Category("Standard")]
        [Description("RotationHinges")]
        public RotationHinges_val RotationHinges
        {
           get
           { return m_RotationHinges; }
           set
           { m_RotationHinges = value; }
        }

        public RotationHinges_val String_to_RotationHinges_val(string pKey)
        {
           switch (pKey)
           {
                case "None":
                   return RotationHinges_val.None;    //None
                case "XX,YY,ZZ":
                   return RotationHinges_val.XX_YY_ZZ;    //XX,YY,ZZ
                case "XX":
                   return RotationHinges_val.XX;    //XX
                case "YY":
                   return RotationHinges_val.YY;    //YY
                case "ZZ":
                   return RotationHinges_val.ZZ;    //ZZ
                case "XX,YY":
                   return RotationHinges_val.XX_YY;    //XX,YY
                case "XX,ZZ":
                   return RotationHinges_val.XX_ZZ;    //XX,ZZ
                case "YY,ZZ":
                   return RotationHinges_val.YY_ZZ;    //YY,ZZ
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string RotationHinges_val_to_String(RotationHinges_val pKey)
        {
           switch (pKey)
           {
                case RotationHinges_val.None:
                   return "None";    //None
                case RotationHinges_val.XX_YY_ZZ:
                   return "XX,YY,ZZ";    //XX,YY,ZZ
                case RotationHinges_val.XX:
                   return "XX";    //XX
                case RotationHinges_val.YY:
                   return "YY";    //YY
                case RotationHinges_val.ZZ:
                   return "ZZ";    //ZZ
                case RotationHinges_val.XX_YY:
                   return "XX,YY";    //XX,YY
                case RotationHinges_val.XX_ZZ:
                   return "XX,ZZ";    //XX,ZZ
                case RotationHinges_val.YY_ZZ:
                   return "YY,ZZ";    //YY,ZZ
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   SettleHeaveX
        //   Attr Group:Standard
        //   Alt Display Name:Settle Heave X (in)
        //   Description:   Prescribed lateral motion in the X axis
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SettleHeaveX;
        [Category("Standard")]
        [Description("SettleHeaveX")]
        public double SettleHeaveX
        {
           get { return m_SettleHeaveX; }
           set { m_SettleHeaveX = value; }
        }



        //   Attr Name:   SettleHeaveY
        //   Attr Group:Standard
        //   Alt Display Name:Settle Heave Y (in)
        //   Description:   Prescribed lateral motion in the Y axis
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SettleHeaveY;
        [Category("Standard")]
        [Description("SettleHeaveY")]
        public double SettleHeaveY
        {
           get { return m_SettleHeaveY; }
           set { m_SettleHeaveY = value; }
        }



        //   Attr Name:   SettleHeaveZ
        //   Attr Group:Standard
        //   Alt Display Name:Settle Heave Z (in)
        //   Description:   Prescribed lateral motion in the Z axis
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_SettleHeaveZ;
        [Category("Standard")]
        [Description("SettleHeaveZ")]
        public double SettleHeaveZ
        {
           get { return m_SettleHeaveZ; }
           set { m_SettleHeaveZ = value; }
        }



        //   Attr Name:   RackingXX
        //   Attr Group:Standard
        //   Alt Display Name:Racking XX (°)
        //   Description:   Prescribed rotation in the XX axis
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RackingXX;
        [Category("Standard")]
        [Description("RackingXX")]
        public double RackingXX
        {
           get { return m_RackingXX; }
           set { m_RackingXX = value; }
        }



        //   Attr Name:   RackingYY
        //   Attr Group:Standard
        //   Alt Display Name:Racking YY (°)
        //   Description:   Prescribed rotation in the YY axis
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RackingYY;
        [Category("Standard")]
        [Description("RackingYY")]
        public double RackingYY
        {
           get { return m_RackingYY; }
           set { m_RackingYY = value; }
        }



        //   Attr Name:   RackingZZ
        //   Attr Group:Standard
        //   Alt Display Name:Racking ZZ (°)
        //   Description:   Prescribed rotation in the ZZ axis
        //   Displayed Units:   store as RADIANS display as DEGREES SIGNED
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_RackingZZ;
        [Category("Standard")]
        [Description("RackingZZ")]
        public double RackingZZ
        {
           get { return m_RackingZZ; }
           set { m_RackingZZ = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: NodeLoad
   // Mirrors: PPLNodeLoad : PPLElement
   //--------------------------------------------------------------------------------------------
   public class NodeLoad : ElementBase
   {

      public static string gXMLkey = "NodeLoad";
      public override string XMLkey() { return gXMLkey; }

      public NodeLoad(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Applied Load";
               m_Name = "";
               m_LoadX = 0;
               m_LoadY = 0;
               m_LoadZ = 0;
               m_MomentX = 0;
               m_MomentY = 0;
               m_MomentZ = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the 
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Applied Load
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the load
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   LoadX
        //   Attr Group:Standard
        //   Alt Display Name:Load X lbs
        //   Description:   Applied load in the X axis
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LoadX;
        [Category("Standard")]
        [Description("LoadX")]
        public double LoadX
        {
           get { return m_LoadX; }
           set { m_LoadX = value; }
        }



        //   Attr Name:   LoadY
        //   Attr Group:Standard
        //   Alt Display Name:Load Y lbs
        //   Description:   Applied load in the Y axis
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LoadY;
        [Category("Standard")]
        [Description("LoadY")]
        public double LoadY
        {
           get { return m_LoadY; }
           set { m_LoadY = value; }
        }



        //   Attr Name:   LoadZ
        //   Attr Group:Standard
        //   Alt Display Name:Load Z lbs
        //   Description:   Applied load in the Z axis
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LoadZ;
        [Category("Standard")]
        [Description("LoadZ")]
        public double LoadZ
        {
           get { return m_LoadZ; }
           set { m_LoadZ = value; }
        }



        //   Attr Name:   MomentX
        //   Attr Group:Standard
        //   Alt Display Name:Moment X (ft-lb)
        //   Description:   Applied Moment in the XX axis
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MomentX;
        [Category("Standard")]
        [Description("MomentX")]
        public double MomentX
        {
           get { return m_MomentX; }
           set { m_MomentX = value; }
        }



        //   Attr Name:   MomentY
        //   Attr Group:Standard
        //   Alt Display Name:Moment Y (ft-lb)
        //   Description:   Applied Moment in the YY axis
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MomentY;
        [Category("Standard")]
        [Description("MomentY")]
        public double MomentY
        {
           get { return m_MomentY; }
           set { m_MomentY = value; }
        }



        //   Attr Name:   MomentZ
        //   Attr Group:Standard
        //   Alt Display Name:Moment Z (ft-lb)
        //   Description:   Applied Moment in the ZZ axis
        //   Displayed Units:   store as FTLBS display as FTLBS or NEWTONMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_MomentZ;
        [Category("Standard")]
        [Description("MomentZ")]
        public double MomentZ
        {
           get { return m_MomentZ; }
           set { m_MomentZ = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: Beam
   // Mirrors: PPLBeam : PPLElement
   //--------------------------------------------------------------------------------------------
   public class Beam : ElementBase
   {

      public static string gXMLkey = "Beam";
      public override string XMLkey() { return gXMLkey; }

      public Beam(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Beam Element";
               m_Name = "<tbd>";
               m_Node1 = "<node 1>";
               m_Node2 = "<node 1>";
               m_Material = "<material>";
               m_Mode = Mode_val.Standard;
               m_BeamType = BeamType_val.Frame;
               m_OverrideRendering = false;
               m_BeamRenderDiam = 12;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is BeamLoad) return true;
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the beam
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Beam Element
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the beam
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <tbd>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   Node1
        //   Attr Group:Standard
        //   Alt Display Name:Node 1
        //   Description:   Node 1
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <node 1>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Node1;
        [Category("Standard")]
        [Description("Node1")]
        public string Node1
        {
           get { return m_Node1; }
           set { m_Node1 = value; }
        }



        //   Attr Name:   Node2
        //   Attr Group:Standard
        //   Alt Display Name:Node 2
        //   Description:   Node 2
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <node 1>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Node2;
        [Category("Standard")]
        [Description("Node2")]
        public string Node2
        {
           get { return m_Node2; }
           set { m_Node2 = value; }
        }



        //   Attr Name:   Material
        //   Attr Group:Standard
        //   Description:   Material
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   <material>
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Material;
        [Category("Standard")]
        [Description("Material")]
        public string Material
        {
           get { return m_Material; }
           set { m_Material = value; }
        }



        //   Attr Name:   Mode
        //   Attr Group:Standard
        //   Description:   Capacity Mode
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Standard
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Compression Only  (Compression Only)
        //        Tension Only  (Tension Only)
        public enum Mode_val
        {
           [Description("Standard")]
           Standard,    //Standard
           [Description("Compression Only")]
           Compression_Only,    //Compression Only
           [Description("Tension Only")]
           Tension_Only     //Tension Only
        }
        private Mode_val m_Mode;
        [Category("Standard")]
        [Description("Mode")]
        public Mode_val Mode
        {
           get
           { return m_Mode; }
           set
           { m_Mode = value; }
        }

        public Mode_val String_to_Mode_val(string pKey)
        {
           switch (pKey)
           {
                case "Standard":
                   return Mode_val.Standard;    //Standard
                case "Compression Only":
                   return Mode_val.Compression_Only;    //Compression Only
                case "Tension Only":
                   return Mode_val.Tension_Only;    //Tension Only
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Mode_val_to_String(Mode_val pKey)
        {
           switch (pKey)
           {
                case Mode_val.Standard:
                   return "Standard";    //Standard
                case Mode_val.Compression_Only:
                   return "Compression Only";    //Compression Only
                case Mode_val.Tension_Only:
                   return "Tension Only";    //Tension Only
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   BeamType
        //   Attr Group:Standard
        //   Alt Display Name:Type
        //   Description:   Beam Type
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Frame
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Truss  (Truss)
        public enum BeamType_val
        {
           [Description("Frame")]
           Frame,    //Frame
           [Description("Truss")]
           Truss     //Truss
        }
        private BeamType_val m_BeamType;
        [Category("Standard")]
        [Description("BeamType")]
        public BeamType_val BeamType
        {
           get
           { return m_BeamType; }
           set
           { m_BeamType = value; }
        }

        public BeamType_val String_to_BeamType_val(string pKey)
        {
           switch (pKey)
           {
                case "Frame":
                   return BeamType_val.Frame;    //Frame
                case "Truss":
                   return BeamType_val.Truss;    //Truss
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string BeamType_val_to_String(BeamType_val pKey)
        {
           switch (pKey)
           {
                case BeamType_val.Frame:
                   return "Frame";    //Frame
                case BeamType_val.Truss:
                   return "Truss";    //Truss
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   OverrideRendering
        //   Attr Group:Rendering
        //   Alt Display Name:Override Rendering
        //   Description:   Indicates if the rendering is controlled by this section
        //   User Level Required:   All user levels may access this attribute
        //   Attribute Type:   BOOLEAN
        //   Default Value:   No
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private bool m_OverrideRendering;
        [Category("Rendering")]
        [Description("OverrideRendering")]
        public bool OverrideRendering
        {
           get { return m_OverrideRendering; }
           set { m_OverrideRendering = value; }
        }



        //   Attr Name:   BeamRenderDiam
        //   Attr Group:Rendering
        //   Alt Display Name:Beam Render (in)
        //   Description:   Node render diameter
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00
        //   Attribute Type:   TRACKERZ
        //   Default Value:   12
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_BeamRenderDiam;
        [Category("Rendering")]
        [Description("BeamRenderDiam")]
        public double BeamRenderDiam
        {
           get { return m_BeamRenderDiam; }
           set { m_BeamRenderDiam = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }


   //--------------------------------------------------------------------------------------------
   //   Class: BeamLoad
   // Mirrors: PPLBeamLoad : PPLElement
   //--------------------------------------------------------------------------------------------
   public class BeamLoad : ElementBase
   {

      public static string gXMLkey = "BeamLoad";
      public override string XMLkey() { return gXMLkey; }

      public BeamLoad(bool pInitialize = false)
      {
          if(pInitialize)
          {
               m_Description = "Applied Load";
               m_Name = "";
               m_Type = Type_val.Uniform;
               m_LoadX = 0;
               m_LoadY = 0;
               m_LoadZ = 0;
               m_Offset = 0;
               m_WorkingDataStore = "";
          }
      }

      public override bool IsLegalChild(ElementBase pChildCandidate)
      {
         if(pChildCandidate is Notes) return true;
         if(pChildCandidate is LinkedURI) return true;
         return false;
      }



        //   Attr Name:   Description
        //   Attr Group:Standard
        //   Description:   Description of the 
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   Applied Load
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Description;
        [Category("Standard")]
        [Description("Description")]
        public string Description
        {
           get { return m_Description; }
           set { m_Description = value; }
        }



        //   Attr Name:   Name
        //   Attr Group:Standard
        //   Description:   Name of the load
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private string m_Name;
        [Category("Standard")]
        [Description("Name")]
        public string Name
        {
           get { return m_Name; }
           set { m_Name = value; }
        }



        //   Attr Name:   Type
        //   Attr Group:Standard
        //   Description:   Type of the load
        //   User Level Required:   Limited users can NOT access this attribute
        //   Attribute Type:   ENUMERATED
        //   Default Value:   Uniform
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        //   Enum Values:
        //        Point  (Point)
        public enum Type_val
        {
           [Description("Uniform")]
           Uniform,    //Uniform
           [Description("Point")]
           Point     //Point
        }
        private Type_val m_Type;
        [Category("Standard")]
        [Description("Type")]
        public Type_val Type
        {
           get
           { return m_Type; }
           set
           { m_Type = value; }
        }

        public Type_val String_to_Type_val(string pKey)
        {
           switch (pKey)
           {
                case "Uniform":
                   return Type_val.Uniform;    //Uniform
                case "Point":
                   return Type_val.Point;    //Point
                default:
                   break;
           }
           throw new Exception("string does not match enum value");
        }

        public string Type_val_to_String(Type_val pKey)
        {
           switch (pKey)
           {
                case Type_val.Uniform:
                   return "Uniform";    //Uniform
                case Type_val.Point:
                   return "Point";    //Point
                default:
                   break;
           }
           throw new Exception("enum value unexpected");
        }



        //   Attr Name:   LoadX
        //   Attr Group:Standard
        //   Alt Display Name:Load X lbs
        //   Description:   Applied load in the X axis
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LoadX;
        [Category("Standard")]
        [Description("LoadX")]
        public double LoadX
        {
           get { return m_LoadX; }
           set { m_LoadX = value; }
        }



        //   Attr Name:   LoadY
        //   Attr Group:Standard
        //   Alt Display Name:Load Y lbs
        //   Description:   Applied load in the Y axis
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LoadY;
        [Category("Standard")]
        [Description("LoadY")]
        public double LoadY
        {
           get { return m_LoadY; }
           set { m_LoadY = value; }
        }



        //   Attr Name:   LoadZ
        //   Attr Group:Standard
        //   Alt Display Name:Load Z lbs
        //   Description:   Applied load in the Z axis
        //   Displayed Units:   store as POUNDS display as POUNDS or NEWTONS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.00###E+0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_LoadZ;
        [Category("Standard")]
        [Description("LoadZ")]
        public double LoadZ
        {
           get { return m_LoadZ; }
           set { m_LoadZ = value; }
        }



        //   Attr Name:   Offset
        //   Attr Group:Standard
        //   Alt Display Name:Offset (in)
        //   Description:   Offset from node 1
        //   Displayed Units:   store as INCHES display as INCHES or CENTIMETERS
        //   User Level Required:   Limited users can NOT access this attribute
        //   Format Expression:   0.0
        //   Attribute Type:   FLOAT
        //   Default Value:   0.0
        //   ReadOnly Value:   No
        //   Visible in Data Entry Panel:   Yes
        //   Include When Substituting:   Yes
        private double m_Offset;
        [Category("Standard")]
        [Description("Offset")]
        public double Offset
        {
           get { return m_Offset; }
           set { m_Offset = value; }
        }



        //   Attr Name:   WorkingDataStore
        //   Attr Group:Standard
        //   Description:   Working Data
        //   User Level Required:   Administrative access only
        //   Attribute Type:   STRING
        //   Default Value:   
        //   ReadOnly Value:   Yes
        //   Visible in Data Entry Panel:   No
        //   Include When Substituting:   No
        private string m_WorkingDataStore;
        [Category("Standard")]
        [Description("WorkingDataStore")]
        public string WorkingDataStore
        {
           get { return m_WorkingDataStore; }
           set { m_WorkingDataStore = value; }
        }

   }

    public class ElementClassFactory
    {
         public static ElementBase Build(String pKey)
         {
             if(pKey == "Scene") return new Scene(true);
             if(pKey == "LoadCase") return new LoadCase(true);
             if(pKey == "Notes") return new Notes(true);
             if(pKey == "LinkedURI") return new LinkedURI(true);
             if(pKey == "PoleInfoPoint") return new PoleInfoPoint(true);
             if(pKey == "PoleSegment") return new PoleSegment(true);
             if(pKey == "WoodPole") return new WoodPole(true);
             if(pKey == "SteelPole") return new SteelPole(true);
             if(pKey == "ConcretePole") return new ConcretePole(true);
             if(pKey == "CompositePole") return new CompositePole(true);
             if(pKey == "SegmentedPole") return new SegmentedPole(true);
             if(pKey == "Anchor") return new Anchor(true);
             if(pKey == "Crossarm") return new Crossarm(true);
             if(pKey == "Insulator") return new Insulator(true);
             if(pKey == "Span") return new Span(true);
             if(pKey == "SpanBundle") return new SpanBundle(true);
             if(pKey == "Tap") return new Tap(true);
             if(pKey == "PowerEquipment") return new PowerEquipment(true);
             if(pKey == "Streetlight") return new Streetlight(true);
             if(pKey == "GuyBrace") return new GuyBrace(true);
             if(pKey == "Riser") return new Riser(true);
             if(pKey == "GenericEquipment") return new GenericEquipment(true);
             if(pKey == "PoleRestoration") return new PoleRestoration(true);
             if(pKey == "Clearance") return new Clearance(true);
             if(pKey == "SpanAddition") return new SpanAddition(true);
             if(pKey == "WoodPoleDamageOrDecay") return new WoodPoleDamageOrDecay(true);
             if(pKey == "CapacityAdjustment") return new CapacityAdjustment(true);
             if(pKey == "MultiPoleStructure") return new MultiPoleStructure(true);
             if(pKey == "LatticeStructure") return new LatticeStructure(true);
             if(pKey == "LatticeSection") return new LatticeSection(true);
             if(pKey == "LatticeGroup") return new LatticeGroup(true);
             if(pKey == "Material") return new Material(true);
             if(pKey == "Node") return new Node(true);
             if(pKey == "NodeJunction") return new NodeJunction(true);
             if(pKey == "NodeConstraint") return new NodeConstraint(true);
             if(pKey == "NodeLoad") return new NodeLoad(true);
             if(pKey == "Beam") return new Beam(true);
             if(pKey == "BeamLoad") return new BeamLoad(true);
             return null;
         }
    }

    public class EnumValsList
    {
        public static string EnumToString(string pKey)
        {
           switch(pKey)
           {
           case "NESC": return "NESC";
           case "Linear": return "Linear";
           case "Fixed": return "Fixed";
           case "Advanced": return "Advanced";
           case "Cholesky_Decomposition": return "Cholesky Decomposition";
           case "Medium": return "Medium";
           case "WindType_2007": return "2007";
           case "B": return "B";
           case "Unknown": return "Unknown";
           case "At_Installation": return "At Installation";
           case "N_A": return "N/A";
           case "Tip": return "Tip";
           case "Auto": return "Auto";
           case "Rule_250B": return "Rule 250B";
           case "Standard": return "Standard";
           case "Load": return "Load";
           case "GO_95": return "GO 95";
           case "ASCE": return "ASCE";
           case "CSA": return "CSA";
           case "AS_NZS_7000": return "AS/NZS 7000";
           case "Deflection_1_Iteration_P_Delta": return "1 Iteration P-Δ";
           case "Deflection_2nd_Order_P_Delta": return "2nd Order P-Δ";
           case "Pinned": return "Pinned";
           case "Legacy": return "Legacy";
           case "Conjugate_Gradient": return "Conjugate Gradient";
           case "Light": return "Light";
           case "Medium_A": return "Medium A";
           case "Medium_B": return "Medium B";
           case "Heavy": return "Heavy";
           case "Severe": return "Severe";
           case "Manual": return "Manual";
           case "Warm_Island": return "Warm Island";
           case "Special": return "Special";
           case "Unset": return "Unset";
           case "WindType_1997": return "1997";
           case "WindType_2002": return "2002";
           case "WindType_2012": return "2012";
           case "A": return "A";
           case "F": return "F";
           case "C": return "C";
           case "Construction_Grade_1": return "1";
           case "Construction_Grade_2": return "2";
           case "Construction_Grade_3": return "3";
           case "None": return "None";
           case "At_Crossing": return "At Crossing";
           case "At_Replacement": return "At Replacement";
           case "D": return "D";
           case "Actual": return "Actual";
           case "Yes": return "Yes";
           case "No": return "No";
           case "Rule_250B_Alternate": return "Rule 250B Alternate";
           case "Rule_250C": return "Rule 250C";
           case "Rule_250D": return "Rule 250D";
           case "Percent_BCH": return "Percent BCH";
           case "Tip_Deflection": return "Tip Deflection";
           case "Wind": return "Wind";
           case "Relative": return "Relative";
           case "Normal": return "Normal";
           case "High_Priority": return "High Priority";
           case "TBD_Initial": return "TBD Initial";
           case "TBD_Complete": return "TBD Complete";
           case "TBD_Accepted": return "TBD Accepted";
           case "External": return "External";
           case "Built_in": return "Built in";
           case "Simple": return "Simple";
           case "Round": return "Round";
           case "Top": return "Top";
           case "Bottom": return "Bottom";
           case "Polygonal": return "Polygonal";
           case "Height": return "Height";
           case "Radius": return "Radius";
           case "Area": return "Area";
           case "Area_Squared": return "Area Squared";
           case "By_Specs": return "By Specs";
           case "Tangent": return "Tangent";
           case "Angle": return "Angle";
           case "Deadend": return "Deadend";
           case "Junction": return "Junction";
           case "Measured": return "Measured";
           case "Class_0": return "Class 0";
           case "Class_1": return "Class 1";
           case "Class_2": return "Class 2";
           case "Class_3": return "Class 3";
           case "Class_4": return "Class 4";
           case "Class_5": return "Class 5";
           case "Class_6": return "Class 6";
           case "Class_7": return "Class 7";
           case "Class_8": return "Class 8";
           case "Unsset": return "Unsset";
           case "Pedestal": return "Pedestal";
           case "NESC_C2_2007": return "NESC C2-2007";
           case "CSA_C22_3_No__1_10": return "CSA C22.3 No. 1-10";
           case "Embedded": return "Embedded";
           case "Automatic": return "Automatic";
           case "Superposition": return "Superposition";
           case "Wood": return "Wood";
           case "Offset": return "Offset";
           case "Pole_Extension": return "Pole Extension";
           case "Full_Gull": return "Full Gull";
           case "Half_Gull": return "Half Gull";
           case "Standoff": return "Standoff";
           case "Double": return "Double";
           case "Single": return "Single";
           case "Interaction": return "Interaction";
           case "Worst_Axis": return "Worst Axis";
           case "Steel": return "Steel";
           case "Composite": return "Composite";
           case "Other": return "Other";
           case "Pin": return "Pin";
           case "Inline": return "Inline";
           case "_Default_": return "<Default>";
           case "Clamped": return "Clamped";
           case "Post": return "Post";
           case "Davit": return "Davit";
           case "Spool": return "Spool";
           case "Underhung": return "Underhung";
           case "Suspension": return "Suspension";
           case "J_Hook": return "J-Hook";
           case "Bolt": return "Bolt";
           case "Extension": return "Extension";
           case "Street": return "Street";
           case "Field": return "Field";
           case "Front": return "Front";
           case "Back": return "Back";
           case "Both": return "Both";
           case "Split": return "Split";
           case "Sheds_1": return "1";
           case "Sheds_2": return "2";
           case "Sheds_3": return "3";
           case "Sheds_4": return "4";
           case "Sheds_5": return "5";
           case "Sheds_6": return "6";
           case "Sheds_7": return "7";
           case "Sheds_8": return "8";
           case "Free": return "Free";
           case "Primary": return "Primary";
           case "Static": return "Static";
           case "Secondary": return "Secondary";
           case "Service": return "Service";
           case "Neutral": return "Neutral";
           case "Telco": return "Telco";
           case "CATV": return "CATV";
           case "Fiber": return "Fiber";
           case "Sub_Transmission": return "Sub-Transmission";
           case "Slack": return "Slack";
           case "Table": return "Table";
           case "Sag_to_Tension": return "Sag to Tension";
           case "Tension_to_Sag": return "Tension to Sag";
           case "Drop": return "Drop";
           case "Overlashed": return "Overlashed";
           case "Bundled": return "Bundled";
           case "Corrugated": return "Corrugated";
           case "Flexpipe": return "Flexpipe";
           case "Irregular": return "Irregular";
           case "_See_Note_": return "(See Note)";
           case "Individual": return "Individual";
           case "Spacers": return "Spacers";
           case "Bonded": return "Bonded";
           case "Twist_Braid": return "Twist/Braid";
           case "Wrapped": return "Wrapped";
           case "Min_Circle": return "Min Circle";
           case "Convex_Hull": return "Convex Hull";
           case "Concave_Hull": return "Concave Hull";
           case "Transformer": return "Transformer";
           case "Pole": return "Pole";
           case "Regulator": return "Regulator";
           case "Capacitor": return "Capacitor";
           case "Switch": return "Switch";
           case "Fuse": return "Fuse";
           case "Box": return "Box";
           case "Rack": return "Rack";
           case "General": return "General";
           case "Decorative": return "Decorative";
           case "Spot_Light": return "Spot Light";
           case "Flood_Light": return "Flood Light";
           case "Traffic_Signal": return "Traffic Signal";
           case "Down": return "Down";
           case "Calculated": return "Calculated";
           case "Span_Head": return "Span/Head";
           case "Sidewalk": return "Sidewalk";
           case "Crossarm": return "Crossarm";
           case "Pushbrace": return "Pushbrace";
           case "Cylinder": return "Cylinder";
           case "Imported": return "Imported";
           case "C2": return "C2";
           case "ET": return "ET";
           case "FiberWrap": return "FiberWrap";
           case "FiberWrap_II": return "FiberWrap II";
           case "Truss": return "Truss";
           case "Wrap": return "Wrap";
           case "Aviation_Ball": return "Aviation Ball";
           case "Vertical": return "Vertical";
           case "Minus": return "Minus";
           case "Plus": return "Plus";
           case "Cut_Out": return "Cut-Out";
           case "Splice": return "Splice";
           case "Damper": return "Damper";
           case "Perch_Stopper": return "Perch Stopper";
           case "Maintenance_Loop": return "Maintenance Loop";
           case "Horizontal": return "Horizontal";
           case "Center": return "Center";
           case "Void": return "Void";
           case "Vehicle_Scrape": return "Vehicle Scrape";
           case "Saw_Cut": return "Saw Cut";
           case "Mower_Cut": return "Mower Cut";
           case "Exposed_Pocket": return "Exposed Pocket";
           case "Enclosed_Pocket": return "Enclosed Pocket";
           case "Heart_Rot": return "Heart Rot";
           case "Shell_Reduction": return "Shell Reduction";
           case "Woodpecker_Hole": return "Woodpecker Hole";
           case "Woodpecker_Nest": return "Woodpecker Nest";
           case "Active_Leg": return "Active Leg";
           case "Worst_Leg": return "Worst Leg";
           case "X_Y_Z": return "X,Y,Z";
           case "XX_YY_ZZ": return "XX,YY,ZZ";
           case "X": return "X";
           case "Y": return "Y";
           case "Z": return "Z";
           case "X_Y": return "X,Y";
           case "X_Z": return "X,Z";
           case "Y_Z": return "Y,Z";
           case "XX": return "XX";
           case "YY": return "YY";
           case "ZZ": return "ZZ";
           case "XX_YY": return "XX,YY";
           case "XX_ZZ": return "XX,ZZ";
           case "YY_ZZ": return "YY,ZZ";
           case "Frame": return "Frame";
           case "Compression_Only": return "Compression Only";
           case "Tension_Only": return "Tension Only";
           case "Uniform": return "Uniform";
           case "Point": return "Point";
           }
           return String.Empty;
        }
    }
}
