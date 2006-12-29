using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using libsecondlife;
using System.Security.Cryptography;
using System.Text;

namespace libsecondlife
{
	public class LLSD {
		public class LLSDParseException : Exception {
			public LLSDParseException(string message) : base(message) { }
		}

		public class LLSDSerializeException : Exception {
			public LLSDSerializeException(string message) : base(message) { }
		}


		private static void SkipWS(XmlTextReader reader) {
			while(reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace) reader.Read();
		}

		public static object LLSDDeserialize(byte[] b) {
			return LLSDDeserialize(new MemoryStream(b,false));
		}

		
		public static object LLSDDeserialize(Stream st) {
			XmlTextReader reader = new XmlTextReader(st);
			reader.Read(); SkipWS(reader);
			if(reader.NodeType != XmlNodeType.Element || reader.LocalName != "llsd") {
				throw new LLSDParseException("Expected <llsd>");
			}
			reader.Read();
			object ret = LLSDParseOne(reader);
			SkipWS(reader);
			if(reader.NodeType != XmlNodeType.EndElement || reader.LocalName != "llsd")
				throw new LLSDParseException("Expected </llsd>");
			return ret;
		}

		public static byte[] LLSDSerialize(object obj) {
			StringWriter sw = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(sw);
			writer.Formatting = Formatting.None;
			writer.WriteStartElement("","llsd","");
			LLSDWriteOne(writer,obj);
			writer.WriteEndElement();
			writer.Close();
			return Encoding.UTF8.GetBytes(sw.ToString());
		}

		public static void LLSDWriteOne(XmlTextWriter writer, object obj) {
			if(obj == null) {
				writer.WriteStartElement("","undef","");
				writer.WriteEndElement();
				return;
			}

			Type t = obj.GetType();
			if(t == typeof(string)) {
				writer.WriteStartElement("","string","");
				writer.WriteString((string)obj);
				writer.WriteEndElement();
   			} else if(t == typeof(long)) {
				writer.WriteStartElement("","integer","");
				writer.WriteString(obj.ToString());
				writer.WriteEndElement();
   			} else if(t == typeof(double)) {
				writer.WriteStartElement("","real","");
				writer.WriteString(obj.ToString());
				writer.WriteEndElement();
   			} else if(t == typeof(bool)) {
				bool b = (bool) obj;
				writer.WriteStartElement("","boolean","");
				if(b)
					writer.WriteString("1");
				else writer.WriteString("0");
				writer.WriteEndElement();
			} else if(t == typeof(LLUUID)) {
				LLUUID u = (LLUUID) obj;
				writer.WriteStartElement("","uuid","");
				writer.WriteString(u.ToStringHyphenated());
				writer.WriteEndElement();
			} else if(t == typeof(Hashtable)) {
				Hashtable h = (Hashtable) obj;
				writer.WriteStartElement("","map","");
				foreach(string key in h.Keys) {
					writer.WriteStartElement("","key","");
					writer.WriteString(key);
					writer.WriteEndElement();
					LLSDWriteOne(writer,h[key]);
				}
				writer.WriteEndElement();
			} else if(t == typeof(ArrayList)) {
				ArrayList a = (ArrayList) obj;
				writer.WriteStartElement("","array","");
				foreach(object item in a) {
					LLSDWriteOne(writer,item);
				}
				writer.WriteEndElement();
   			} else if(t == typeof(byte[])) {
				byte[] b = (byte[]) obj;
				writer.WriteStartElement("","binary","");
				writer.WriteStartAttribute("","encoding","");
				writer.WriteString("base64");
				writer.WriteEndAttribute();
				char[] tmp = new char[b.Length*2]; // too much
				int i = Convert.ToBase64CharArray(b,0,b.Length,tmp,0);
				Array.Resize(ref tmp,i);
				writer.WriteString(new String(tmp));
				writer.WriteEndElement();
				
			} else {
				throw new LLSDSerializeException("Unknown type "+t.Name);
			}
		}

		public static object LLSDParseOne(XmlTextReader reader) {
			SkipWS(reader);
			if(reader.NodeType != XmlNodeType.Element) 
				throw new LLSDParseException("Expected an element");
			string dtype = reader.LocalName; object ret = null;
			
			switch(dtype) {
				case "undef": {
					reader.Read(); SkipWS(reader); ret = null; break;
				}
				case "boolean": {
					reader.Read();
					string s = reader.ReadString().Trim();
					if(s == "" || s == "false" || s == "0") {
						ret = false;
					} else if(s == "true" || s == "1") {
						ret = true;
					} else {
						throw new LLSDParseException("Bad boolean value "+s);
					}
					break;
				}
				case "integer": {
					reader.Read();
					ret = Convert.ToInt64(reader.ReadString().Trim());
					break;
				}
				case "real": {
					reader.Read();
					ret = Convert.ToDouble(reader.ReadString().Trim());
					break;
				}
				case "uuid": {
					reader.Read();
					ret = new LLUUID(reader.ReadString().Trim());
					break;
				}
				case "string": {
					reader.Read();
					ret = reader.ReadString();
					break;
				}
				case "binary": {
					if(reader.GetAttribute("encoding") != null &&
					   reader.GetAttribute("encoding")!="base64")
						throw new LLSDParseException("Unknown encoding: "+
							reader.GetAttribute("encoding"));
					reader.Read();
					FromBase64Transform b64 = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces);
					byte[] inp = Encoding.ASCII.GetBytes(reader.ReadString());
					ret = b64.TransformFinalBlock(inp,0,inp.Length);
					break;
				}
				case "date": {
					reader.Read();
					throw new Exception("LLSD TODO: date");
					break;
				}
				case "map": {
					return LLSDParseMap(reader);
				}
				case "array": {
					return LLSDParseArray(reader);
				}
				default:
					throw new LLSDParseException("Unknown element <"+dtype+">");
			}
			if(reader.NodeType != XmlNodeType.EndElement || reader.LocalName != dtype) {
				throw new LLSDParseException("Expected </"+dtype+">");
			}
			reader.Read();
			return ret;
		}

		public static Hashtable LLSDParseMap(XmlTextReader reader) {
			if(reader.NodeType != XmlNodeType.Element || reader.LocalName != "map")
				throw new LLSDParseException("Expected <map>");
			reader.Read();

			Hashtable ret = new Hashtable();

			while(true) {
				SkipWS(reader);
				if(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "map") {
					reader.Read(); break;
				}
				if(reader.NodeType != XmlNodeType.Element || reader.LocalName != "key")
					throw new LLSDParseException("Expected <key>");
				string key = reader.ReadString();
				if(reader.NodeType != XmlNodeType.EndElement || reader.LocalName != "key")
					throw new LLSDParseException("Expected </key>");
				reader.Read();
				object val = LLSDParseOne(reader);
				ret[key] = val;
			}
			return ret; // TODO
		}

		public static ArrayList LLSDParseArray(XmlTextReader reader) {
			if(reader.NodeType != XmlNodeType.Element || reader.LocalName != "array")
				throw new LLSDParseException("Expected <array>");
			reader.Read();

			ArrayList ret = new ArrayList();

			while(true) {
				SkipWS(reader);
				if(reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "array") {
					reader.Read(); break;
				}
				ret.Insert(ret.Count,LLSDParseOne(reader));
			}
			return ret; // TODO
		}

		private static string GetSpaces(int count) {
			StringBuilder b = new StringBuilder();
			for(int i = 0; i < count;i++) b.Append(" ");
			return b.ToString();
		}

		public static String LLSDDump(object obj, int indent) {
			if(obj == null) {
				return GetSpaces(indent) + "- undef\n";
			} else if(obj.GetType() == typeof(string)) {
				return GetSpaces(indent) + "- string \"" + (string)obj + "\"\n";
			} else if(obj.GetType() == typeof(long)) {
				return GetSpaces(indent) + "- integer " + obj.ToString() + "\n";
			} else if(obj.GetType() == typeof(double)) {
				return GetSpaces(indent) + "- float " + obj.ToString() + "\n";
			} else if(obj.GetType() == typeof(LLUUID)) {
				return GetSpaces(indent) + "- uuid " + ((LLUUID)obj).ToStringHyphenated() + "\n";
			} else if(obj.GetType() == typeof(Hashtable)) {
				StringBuilder ret = new StringBuilder();
				ret.Append(GetSpaces(indent) + "- map\n");
				Hashtable map = (Hashtable)obj;
				foreach(string key in map.Keys) {
					ret.Append(GetSpaces(indent+2) + "- key \"" + key + "\"\n");
					ret.Append(LLSDDump(map[key],indent+3));
				}
				return ret.ToString();
			} else if(obj.GetType() == typeof(ArrayList)) {
				StringBuilder ret = new StringBuilder();
				ret.Append(GetSpaces(indent) + "- array\n");
				ArrayList list = (ArrayList)obj;
				foreach(object item in list) {
					ret.Append(LLSDDump(item,indent+2));
				}
				return ret.ToString();
			} else if(obj.GetType() == typeof(byte[])) {
				return GetSpaces(indent) + "- binary\n" + Helpers.FieldToHexDump((byte[])obj,"")+"\n";
			} else {
				return GetSpaces(indent) + "- unknown type "+obj.GetType().Name+"\n";
			}
		}
	}
}