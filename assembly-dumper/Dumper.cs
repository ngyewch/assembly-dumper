using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace assemblydumper
{
	public class Dumper
	{

		public void Introspect(string inputPath, string outputDirectory)
		{
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
			Assembly assembly = Assembly.ReflectionOnlyLoadFrom(inputPath);
			ICollection<string> lines = new LinkedList<string>();
			Dump(lines, assembly, 0);
			Directory.CreateDirectory(outputDirectory);

			string outputSourceDirectory = Path.Combine(outputDirectory, "src");
			Directory.CreateDirectory(outputSourceDirectory);
			string outputPath = Path.Combine(outputSourceDirectory, Path.GetFileNameWithoutExtension(inputPath) + ".cs");
			SaveFile(lines, outputPath);
		}

		private Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
		{
			string baseDirectory = Path.GetDirectoryName(args.RequestingAssembly.Location);
			string assemblyPath = Path.Combine(baseDirectory, new AssemblyName(args.Name).Name + ".dll");
			if (File.Exists(assemblyPath))
			{
				return Assembly.ReflectionOnlyLoadFrom(assemblyPath);
			}
			return Assembly.ReflectionOnlyLoad(args.Name);
		}

		private void Dump(ICollection<string> lines, Assembly assembly, int indent)
		{
			Append(lines, String.Format("// {0}", assembly.FullName), indent);
			Type[] types = assembly.GetTypes();
			SortedList<string, string> namespaceSet = new SortedList<string, string>();
			foreach (Type type in types)
			{
				if (!namespaceSet.ContainsKey(type.Namespace))
				{
					namespaceSet.Add(type.Namespace, type.Namespace);
				}
			}
			foreach (KeyValuePair<string, string> entry in namespaceSet)
			{
				string ns = entry.Value;
				if (ns != null)
				{
					Append(lines, String.Format("namespace {0} {{", ns), indent);
					indent++;
				}
				for (int i = 0; i < types.Length; i++)
				{
					System.Type type = types[i];
					if (!type.IsNested)
					{
						Dump(lines, type, indent);
					}
				}
				if (ns != null)
				{
					indent--;
					Append(lines, "}", indent);
					Append(lines, "", indent);
				}
			}
		}

		private void Dump(ICollection<string> lines, System.Type type, int indent)
		{
			if (!type.IsVisible)
			{
				return;
			}
			ICollection<string> infoList = new LinkedList<string>();
			if (type.IsArray)
			{
				infoList.Add("array");
			}
			if (type.IsByRef)
			{
				infoList.Add("byRef");
			}
			if (type.IsCOMObject)
			{
				infoList.Add("COMObject");
			}
			if (type.IsContextful)
			{
				infoList.Add("contextful");
			}
			if (type.IsGenericParameter)
			{
				infoList.Add("genericParameter");
			}
			if (type.IsGenericType)
			{
				infoList.Add("genericType");
			}
			if (type.IsGenericTypeDefinition)
			{
				infoList.Add("genericTypeDefinition");
			}
			if (type.IsImport)
			{
				infoList.Add("import");
			}
			if (type.IsMarshalByRef)
			{
				infoList.Add("marshalByRef");
			}
			if (type.IsPointer)
			{
				infoList.Add("pointer");
			}
			if (type.IsPrimitive)
			{
				infoList.Add("primitive");
			}
			if (type.IsSerializable)
			{
				infoList.Add("serializable");
			}
			if (type.IsSpecialName)
			{
				infoList.Add("specialName");
			}
			if (type.IsValueType)
			{
				infoList.Add("valueType");
			}
			if (infoList.Count > 0)
			{
				Append(lines, String.Format("// {0} attributes={1}", type.DeclaringType, Join(infoList, ",")), indent);
			}

			ICollection<string> prefixes = new LinkedList<string>();
			if (type.IsPublic)
			{
				prefixes.Add("public");
			}
			if (type.IsAbstract && !type.IsInterface)
			{
				prefixes.Add("abstract");
			}
			if (type.IsSealed)
			{
				prefixes.Add("sealed");
			}
			if (type.IsClass)
			{
				prefixes.Add("class");
				Append(lines, String.Format("{0} {1}{2} {{", Join(prefixes, " "), GetClassName(type.Namespace, type), GetExtends(type)), indent);
				foreach (PropertyInfo propertyInfo in type.GetProperties())
				{
					if (propertyInfo.DeclaringType == type)
					{
						Dump(lines, propertyInfo, indent + 1);
					}
				}
				foreach (MethodInfo methodInfo in type.GetMethods())
				{
					if ((methodInfo.DeclaringType == type) && methodInfo.IsPublic && !methodInfo.Name.StartsWith("get_") && !methodInfo.Name.StartsWith("set_"))
					{
						Dump(lines, methodInfo, indent + 1);
					}
				}
				foreach (System.Type subType in type.Assembly.GetTypes())
				{
					if (subType.DeclaringType == type)
					{
						Dump(lines, subType, indent + 1);
					}
				}
				Append(lines, "}", indent);
			}
			else if (type.IsInterface)
			{
				prefixes.Add("interface");
				Append(lines, String.Format("{0} {1}{2} {{", Join(prefixes, " "), GetClassName(type.Namespace, type), GetExtends(type)), indent);
				foreach (PropertyInfo propertyInfo in type.GetProperties())
				{
					if (propertyInfo.DeclaringType == type)
					{
						Dump(lines, propertyInfo, indent + 1);
					}
				}
				foreach (MethodInfo methodInfo in type.GetMethods())
				{
					if ((methodInfo.DeclaringType == type) && methodInfo.IsPublic && !methodInfo.Name.StartsWith("get_") && !methodInfo.Name.StartsWith("set_"))
					{
						Dump(lines, methodInfo, indent + 1);
					}
				}
				foreach (System.Type subType in type.Assembly.GetTypes())
				{
					if (subType.DeclaringType == type)
					{
						Dump(lines, subType, indent + 1);
					}
				}
				Append(lines, "}", indent);
			}
			else if (type.IsEnum)
			{
				prefixes.Add("enum");
				Append(lines, String.Format("{0} {1} {{", Join(prefixes, " "), type.Name), indent);
				Append(lines, "}", indent);
			}
			else
			{
				Append(lines, "// Unknown " + type, indent);
			}
		}

		private void Dump(ICollection<string> lines, PropertyInfo propertyInfo, int indent)
		{
			ICollection<string> accessors = new LinkedList<string>();
			if (propertyInfo.CanRead)
			{
				accessors.Add("get;");
			}
			if (propertyInfo.CanWrite)
			{
				accessors.Add("set;");
			}
			Append(lines, String.Format("public {0} {1} {{ {2} }}", GetClassName(propertyInfo.DeclaringType.Namespace, propertyInfo.PropertyType), propertyInfo.Name, Join(accessors, " ")), indent);
		}

		private void Dump(ICollection<string> lines, MethodInfo methodInfo, int indent)
		{
			ICollection<string> parameters = new LinkedList<string>();
			foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
			{
				parameters.Add(GetClassName(methodInfo.DeclaringType.Namespace, parameterInfo.ParameterType) + " " + parameterInfo.Name);
			}
			Append(lines, String.Format("public {0} {1}({2}) {{ }}", GetClassName(methodInfo.DeclaringType.Namespace, methodInfo.ReturnType), methodInfo.Name, Join(parameters, ", ")), indent);
		}

		private string GetClassName(string currentNamespace, System.Type type)
		{
			string className = ResolveClassName(currentNamespace, type);
			ICollection<string> args = new LinkedList<string>();
			if (type.IsGenericTypeDefinition)
			{
				if (type.GetGenericArguments().Length > 0)
				{
					foreach (System.Type arg in type.GetGenericArguments())
					{
						args.Add(arg.Name);
					}
					return className + "<" + Join(args, ", ") + ">";
				}
			}
			else if (type.GetGenericArguments().Length > 0)
			{
				foreach (System.Type arg in type.GetGenericArguments())
				{
					args.Add(ResolveClassName(currentNamespace, arg));
				}
				if (className == "System.Nullable")
				{
					return Join(args, ", ") + "?";
				}
				else
				{
					return className + "<" + Join(args, ", ") + ">";
				}
			}

			return className;
		}

		private string ResolveClassName(string currentNamespace, System.Type type)
		{
			string typeName;
			if (type.IsPrimitive)
			{
				if (type.FullName == "System.Boolean")
				{
					typeName = "bool";
				}
				else if (type.FullName == "System.Double")
				{
					typeName = "double";
				}
				else if (type.FullName == "System.Int32")
				{
					typeName = "int";
				}
				else if (type.FullName == "System.Int64")
				{
					typeName = "long";
				}
				else if (type.FullName == "System.Single")
				{
					typeName = "float";
				}
				else
				{
					typeName = "/* primitive */" + type.FullName;
				}
			}
			else if (type.FullName == "System.Object")
			{
				typeName = "object";
			}
			else if (type.FullName == "System.String")
			{
				typeName = "string";
			}
			else if (type.FullName == "System.Void")
			{
				typeName = "void";
			}
			else if (type.Namespace == currentNamespace)
			{
				typeName = type.Name;
			}
			else
			{
				typeName = type.FullName;
			}
			if (typeName != null)
			{
				int p = typeName.LastIndexOf('`');
				if (p >= 0)
				{
					typeName = typeName.Substring(0, p);
				}
			}
			return typeName;
		}

		private string GetExtends(System.Type type)
		{
			ICollection<string> extends = new LinkedList<string>();
			if ((type.BaseType != null) && (type.BaseType.FullName != "System.Object"))
			{
				System.Type subType = type.BaseType;
				extends.Add(GetClassName(type.Namespace, subType));
			}
			foreach (System.Type subType in type.GetInterfaces())
			{
				extends.Add(GetClassName(type.Namespace, subType));
			}
			if (extends.Count > 0)
			{
				return " : " + Join(extends, ", ");
			}
			else
			{
				return "";
			}
		}

		private string GetIndent(int indent)
		{
			string s = "";
			for (int i = 0; i < indent; i++)
			{
				s += "    ";
			}
			return s;
		}

		private void Append(ICollection<string> lines, string line, int indent)
		{
			lines.Add(GetIndent(indent) + line + "\n");
		}

		private string Join(ICollection<string> strings, string delimiter)
		{
			string sb = "";
			bool first = true;
			foreach (string s in strings)
			{
				if (!first)
				{
					sb += delimiter;
				}
				else
				{
					first = false;
				}
				sb += s;
			}
			return sb;
		}

		private void SaveFile(ICollection<string> lines, string targetPath)
		{
			StreamWriter writer = new StreamWriter(targetPath, false, new System.Text.UTF8Encoding(true), 8192);
			foreach (string line in lines)
			{
				writer.Write(line);
			}
			writer.Close();
		}
	}
}