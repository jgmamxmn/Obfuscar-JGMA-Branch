#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace Obfuscar
{
    interface IMapWriter
    {
        void WriteMap(ObfuscationMap map);
    }

    // JGMA added this 2021-01-12
    class JsonMapWriter : TsvMapWriter
    {
        public JsonMapWriter(TextWriter writer) : base(writer) { }
        protected override void DoWrite(List<Entry> MyEntries, TextWriter MyWriter)
        {
            MyWriter.Write(Newtonsoft.Json.JsonConvert.SerializeObject(MyEntries));
        }
    }

    class TsvMapWriter : IMapWriter, IDisposable
    {
        private readonly TextWriter writer1;

        public TsvMapWriter(TextWriter writer)
        {
            this.writer1 = writer;
        }

        public class Entry
        {
            /// <summary>
            /// Original name of the entry
            /// </summary>
            public string CleartextName;
            /// <summary>
            /// Obfuscated name, with non-ANSI characters translated to hex in format [0x0]
            /// </summary>
            public string ObfuscatedName;
            /// <summary>
            /// For local-scope variables, this sets out the method in which they reside
            /// </summary>
            public string CleartextContext;
            /// <summary>
            /// For local-scope variables, this sets out the method in which they reside
            /// </summary>
            public string ObfuscatedContext;
            /// <summary>
            /// e.g. 'method', 'class', 'resource', 'property', 'field'
            /// </summary>
            public string EntryType;
            /// <summary>
            /// e.g. 'renamed', 'skipped'
            /// </summary>
            public string Status;

            /// <summary>
            /// We don't use this (yet)
            /// </summary>
            public string Method_Params;
            /// <summary>
            /// We don't use this (yet)
            /// </summary>
            public string Field_Type;

            public Entry(string cleartextName, string obfuscatedName_IncludingUnicode, string cleartextContext, string obfuscatedContext_IncludingUnicode, string entryType, string status)
            {
                CleartextName = cleartextName;
                ObfuscatedName = TsvMapWriter.ProcessName(obfuscatedName_IncludingUnicode);
                CleartextContext = cleartextContext;
                ObfuscatedContext = TsvMapWriter.ProcessName(obfuscatedContext_IncludingUnicode);
                EntryType = entryType;
                Status = status;
            }
        }

        public List<Entry> Entries = new List<Entry>();

        public void WriteMap(ObfuscationMap map)
        {
            // JGMA: Previously this showed the renamed items first, then the skipped ones. We don't care - just show everything together.

            //writer.WriteLine("Renamed Types:");

            foreach (ObfuscatedClass classInfo in map.ClassMap.Values)
            {
                //if (classInfo.Status == ObfuscationStatus.Renamed)
                    DumpClass(classInfo);
            }

            /*writer.WriteLine();
            writer.WriteLine("Skipped Types:");

            foreach (ObfuscatedClass classInfo in map.ClassMap.Values)
            {
                // now print the stuff we skipped
                if (classInfo.Status == ObfuscationStatus.Skipped)
                    DumpClass(classInfo);
            }

            writer.WriteLine();*/
            //writer.WriteLine("Renamed Resources:");
            //writer.WriteLine();

            foreach (ObfuscatedThing info in map.Resources)
            {
                if (info.Status == ObfuscationStatus.Renamed)
                {
                    //writer.WriteLine("{0}\t{1}\tRenamed", ProcessName(info.StatusText), info.Name);
                    Entries.Add(new Entry(info.Name, info.StatusText, "", "", "resource", "renamed"));
                }
                else
                {
                    //writer.WriteLine("{0}\t{1}\tSkipped", info.Name, info.Name);
                    Entries.Add(new Entry(info.Name, info.Name, "", "", "resource", "skipped"));
                }
            }

            //writer.WriteLine();
            /*writer.WriteLine("Skipped Resources:");
            writer.WriteLine();

            foreach (ObfuscatedThing info in map.Resources)
            {
                if (info.Status == ObfuscationStatus.Skipped)
                    writer.WriteLine("{0} ({1})", info.Name, info.StatusText);
            }*/

            DoWrite(Entries, writer1);
        }

        protected virtual void DoWrite(List<Entry> MyEntries, TextWriter MyWriter)
        {
            MyWriter.WriteLine("CleartextContext\tObfuscatedContext\tCleartextName\tObfuscatedName\tEntryType\tStatus");

            foreach (var E in MyEntries)
            {
                StringBuilder SB = new StringBuilder();
                SB.Append(E.CleartextContext).Append("\t").Append(E.ObfuscatedContext).Append("\t")
                    .Append(E.CleartextName).Append("\t").Append(E.ObfuscatedName).Append("\t").Append(E.EntryType).Append("\t").Append(E.Status);
                MyWriter.WriteLine(SB.ToString());
            }
        }

        /// <summary>
        /// Uses same logic as MxmnLib.ctxPostError.DeobfuscateErrorString
        /// </summary>
        /// <param name="inName"></param>
        public static string ProcessName(string inName)
        {
            var SB = new System.Text.StringBuilder();
            foreach (char C in inName)
            {
                int I = (int)C;
                if ((I >= 32 && I <= 126) || C == '\r' || C == '\n')
                    SB.Append(C);
                else
                    SB.AppendFormat("[0x{0:x}]", I);
            }
            return SB.ToString();
        }

        private void DumpClass(ObfuscatedClass classInfo)
        {
            string classCleartextName, classObfuscatedName;

            if (classInfo.Status == ObfuscationStatus.Renamed)
            {
                //writer.WriteLine("{0} -> {1}", classInfo.Name, classInfo.StatusText);
                //writer.WriteLine("{0}\t{1}\tRenamed", ObfText, CleanText);
                Entries.Add(new Entry(classInfo.Name, classInfo.StatusText, "", "", "class", "renamed"));
                classCleartextName = classInfo.Name;
                classObfuscatedName = classInfo.StatusText;
            }
            else if (classInfo.Status == ObfuscationStatus.Skipped)
            {
                Entries.Add(new Entry(classInfo.Name, classInfo.Name, "", "", "class", "skipped"));
                classCleartextName = classInfo.Name;
                classObfuscatedName = classInfo.Name;
            }
            else
                return;

            int numRenamed = 0;
            foreach (KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods)
            {
                //if (method.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpMethod(method.Key, method.Value);
                    numRenamed++;
                }
            }

            /*
            // add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Methods.Count)
               writer.WriteLine();
            foreach (KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods)
            {
                if (method.Value.Status == ObfuscationStatus.Skipped)
                    DumpMethod(method.Key, method.Value);
            }
            // add a blank line to separate methods from field...it's pretty.
            if (classInfo.Methods.Count > 0 && classInfo.Fields.Count > 0)
                writer.WriteLine();*/

            //numRenamed = 0;
            foreach (KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields)
            {
                //if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpField(field.Key, field.Value, classCleartextName, classObfuscatedName);
                    numRenamed++;
                }
            }

            /*// add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Fields.Count)
                writer.WriteLine();
            foreach (KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpField(writer, field.Key, field.Value);
            }

            // add a blank line to separate props...it's pretty.
            if (classInfo.Properties.Count > 0)
                writer.WriteLine();*/

            //numRenamed = 0;
            foreach (KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties)
            {
                //if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpProperty(field.Key, field.Value, classCleartextName, classObfuscatedName);
                    numRenamed++;
                }
            }

            /*// add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Properties.Count)
                writer.WriteLine();

            foreach (KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpProperty(writer, field.Key, field.Value);
            }

            // add a blank line to separate events...it's pretty.
            if (classInfo.Events.Count > 0)
                writer.WriteLine();*/

            //numRenamed = 0;
            foreach (KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events)
            {
                //if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpEvent(field.Key, field.Value, classCleartextName, classObfuscatedName);
                    numRenamed++;
                }
            }

            /*// add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Events.Count)
                writer.WriteLine();*/

            foreach (KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events)
            {
                //if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpEvent(field.Key, field.Value, classCleartextName, classObfuscatedName);
            }

            //writer.WriteLine("}");
        }

        private void DumpMethod(MethodKey key, ObfuscatedThing info)
        {
            /*writer.Write("{0}(", info.Name);
            for (int i = 0; i < key.Count; i++)
            {
                if (i > 0)
                    writer.Write(", ");
                else
                    writer.Write(" ");

                writer.Write(key.ParamTypes[i]);
            }

            if (info.Status == ObfuscationStatus.Renamed)
                writer.WriteLine(" ) -> {0}", info.StatusText);
            else
            {
                Debug.Assert(info.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");

                writer.WriteLine(" ) skipped:  {0}", info.StatusText);
            }*/

            Entry E;
            if (info.Status == ObfuscationStatus.Renamed)
                E = new Entry(info.Name, info.StatusText, "", "", "method", "renamed");
            else if (info.Status == ObfuscationStatus.Skipped)
                E = new Entry(info.Name, info.Name, "", "", "method", "skipped");
            else
                return;

            E.Method_Params = string.Join(", ", key.ParamTypes);
            Entries.Add(E);
        }

        private void DumpField(/*TextWriter writer, */FieldKey key, ObfuscatedThing info, string CleartextContext, string ObfuscatedContext)
        {
            Entry E;
            if (info.Status == ObfuscationStatus.Renamed)
                E = new Entry(info.Name, info.StatusText, CleartextContext, ObfuscatedContext, "field", "renamed");
            else if (info.Status == ObfuscationStatus.Skipped)
                E = new Entry(info.Name, info.Name, CleartextContext, ObfuscatedContext, "field", "skipped");
            else
                return;

            E.Field_Type = key.Type;
            Entries.Add(E);
        }

        private void DumpProperty(/*TextWriter writer, */PropertyKey key, ObfuscatedThing info, string CleartextContext, string ObfuscatedContext)
        {
            /*if (info.Status == ObfuscationStatus.Renamed)
                writer.WriteLine("\t{0} {1} -> {2}", key.Type, info.Name, info.StatusText);
            else
            {
                Debug.Assert(info.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");

                writer.WriteLine("\t{0} {1} skipped:  {2}", key.Type, info.Name, info.StatusText);
            }*/
            Entry E;
            if (info.Status == ObfuscationStatus.Renamed)
                E = new Entry(info.Name, info.StatusText, CleartextContext, ObfuscatedContext, "property", "renamed");
            else if (info.Status == ObfuscationStatus.Skipped)
                E = new Entry(info.Name, info.Name, CleartextContext, ObfuscatedContext, "property", "skipped");
            else
                return;
            E.Field_Type = key.Type;
            Entries.Add(E);
        }

        private void DumpEvent(/*TextWriter writer, */EventKey key, ObfuscatedThing info, string CleartextContext, string ObfuscatedContext)
        {
            Entry E;
            if (info.Status == ObfuscationStatus.Renamed)
                E = new Entry(info.Name, info.StatusText, CleartextContext, ObfuscatedContext, "event", "renamed");
            else if (info.Status == ObfuscationStatus.Skipped)
                E = new Entry(info.Name, info.Name, CleartextContext, ObfuscatedContext, "event", "skipped");
            else
                return;
            E.Field_Type = key.Type;
            Entries.Add(E);
        }

        public void Dispose()
        {
            writer1.Close();
        }
    }


    class TextMapWriter : IMapWriter, IDisposable
    {
        private readonly TextWriter writer;

        public TextMapWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteMap(ObfuscationMap map)
        {
            writer.WriteLine("Renamed Types:");

            foreach (ObfuscatedClass classInfo in map.ClassMap.Values)
            {
                // print the ones we didn't skip first
                if (classInfo.Status == ObfuscationStatus.Renamed)
                    DumpClass(classInfo);
            }

            writer.WriteLine();
            writer.WriteLine("Skipped Types:");

            foreach (ObfuscatedClass classInfo in map.ClassMap.Values)
            {
                // now print the stuff we skipped
                if (classInfo.Status == ObfuscationStatus.Skipped)
                    DumpClass(classInfo);
            }

            writer.WriteLine();
            writer.WriteLine("Renamed Resources:");
            writer.WriteLine();

            foreach (ObfuscatedThing info in map.Resources)
            {
                if (info.Status == ObfuscationStatus.Renamed)
                    writer.WriteLine("{0} -> {1}", info.Name, info.StatusText);
            }

            writer.WriteLine();
            writer.WriteLine("Skipped Resources:");
            writer.WriteLine();

            foreach (ObfuscatedThing info in map.Resources)
            {
                if (info.Status == ObfuscationStatus.Skipped)
                    writer.WriteLine("{0} ({1})", info.Name, info.StatusText);
            }
        }

        private void DumpClass(ObfuscatedClass classInfo)
        {
            writer.WriteLine();
            if (classInfo.Status == ObfuscationStatus.Renamed)
                writer.WriteLine("{0} -> {1}", classInfo.Name, classInfo.StatusText);
            else
            {
                Debug.Assert(classInfo.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");
                writer.WriteLine("{0} skipped:  {1}", classInfo.Name, classInfo.StatusText);
            }
            writer.WriteLine("{");

            int numRenamed = 0;
            foreach (KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods)
            {
                if (method.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpMethod(method.Key, method.Value);
                    numRenamed++;
                }
            }

            // add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Methods.Count)
                writer.WriteLine();

            foreach (KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods)
            {
                if (method.Value.Status == ObfuscationStatus.Skipped)
                    DumpMethod(method.Key, method.Value);
            }

            // add a blank line to separate methods from field...it's pretty.
            if (classInfo.Methods.Count > 0 && classInfo.Fields.Count > 0)
                writer.WriteLine();

            numRenamed = 0;
            foreach (KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields)
            {
                if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpField(writer, field.Key, field.Value);
                    numRenamed++;
                }
            }

            // add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Fields.Count)
                writer.WriteLine();

            foreach (KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpField(writer, field.Key, field.Value);
            }

            // add a blank line to separate props...it's pretty.
            if (classInfo.Properties.Count > 0)
                writer.WriteLine();

            numRenamed = 0;
            foreach (KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties)
            {
                if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpProperty(writer, field.Key, field.Value);
                    numRenamed++;
                }
            }

            // add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Properties.Count)
                writer.WriteLine();

            foreach (KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpProperty(writer, field.Key, field.Value);
            }

            // add a blank line to separate events...it's pretty.
            if (classInfo.Events.Count > 0)
                writer.WriteLine();

            numRenamed = 0;
            foreach (KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events)
            {
                if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpEvent(writer, field.Key, field.Value);
                    numRenamed++;
                }
            }

            // add a blank line to separate renamed from skipped...it's pretty.
            if (numRenamed < classInfo.Events.Count)
                writer.WriteLine();

            foreach (KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpEvent(writer, field.Key, field.Value);
            }

            writer.WriteLine("}");
        }

        private void DumpMethod(MethodKey key, ObfuscatedThing info)
        {
            writer.Write("\t{0}(", info.Name);
            for (int i = 0; i < key.Count; i++)
            {
                if (i > 0)
                    writer.Write(", ");
                else
                    writer.Write(" ");

                writer.Write(key.ParamTypes[i]);
            }

            if (info.Status == ObfuscationStatus.Renamed)
                writer.WriteLine(" ) -> {0}", info.StatusText);
            else
            {
                Debug.Assert(info.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");

                writer.WriteLine(" ) skipped:  {0}", info.StatusText);
            }
        }

        private void DumpField(TextWriter writer, FieldKey key, ObfuscatedThing info)
        {
            if (info.Status == ObfuscationStatus.Renamed)
                writer.WriteLine("\t{0} {1} -> {2}", key.Type, info.Name, info.StatusText);
            else
            {
                Debug.Assert(info.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");

                writer.WriteLine("\t{0} {1} skipped:  {2}", key.Type, info.Name, info.StatusText);
            }
        }

        private void DumpProperty(TextWriter writer, PropertyKey key, ObfuscatedThing info)
        {
            if (info.Status == ObfuscationStatus.Renamed)
                writer.WriteLine("\t{0} {1} -> {2}", key.Type, info.Name, info.StatusText);
            else
            {
                Debug.Assert(info.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");

                writer.WriteLine("\t{0} {1} skipped:  {2}", key.Type, info.Name, info.StatusText);
            }
        }

        private void DumpEvent(TextWriter writer, EventKey key, ObfuscatedThing info)
        {
            if (info.Status == ObfuscationStatus.Renamed)
                writer.WriteLine("\t{0} {1} -> {2}", key.Type, info.Name, info.StatusText);
            else
            {
                Debug.Assert(info.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");

                writer.WriteLine("\t{0} {1} skipped:  {2}", key.Type, info.Name, info.StatusText);
            }
        }

        public void Dispose()
        {
            writer.Close();
        }
    }


    class XmlMapWriter : IMapWriter, IDisposable
    {
        private readonly XmlWriter writer;

        public XmlMapWriter(TextWriter writer)
        {
            this.writer = new XmlTextWriter(writer);
        }

        public void WriteMap(ObfuscationMap map)
        {
            writer.WriteStartElement("mapping");
            writer.WriteStartElement("renamedTypes");

            foreach (ObfuscatedClass classInfo in map.ClassMap.Values)
            {
                // print the ones we didn't skip first
                if (classInfo.Status == ObfuscationStatus.Renamed)
                    DumpClass(classInfo);
            }
            writer.WriteEndElement();
            writer.WriteString("\r\n");

            writer.WriteStartElement("skippedTypes");

            foreach (ObfuscatedClass classInfo in map.ClassMap.Values)
            {
                // now print the stuff we skipped
                if (classInfo.Status == ObfuscationStatus.Skipped)
                    DumpClass(classInfo);
            }
            writer.WriteEndElement();
            writer.WriteString("\r\n");

            writer.WriteStartElement("renamedResources");

            foreach (ObfuscatedThing info in map.Resources)
            {
                if (info.Status == ObfuscationStatus.Renamed)
                {
                    writer.WriteStartElement("renamedResource");
                    writer.WriteAttributeString("oldName", info.Name);
                    writer.WriteAttributeString("newName", info.StatusText);
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            writer.WriteString("\r\n");

            writer.WriteStartElement("skippedResources");

            foreach (ObfuscatedThing info in map.Resources)
            {
                if (info.Status == ObfuscationStatus.Skipped)
                {
                    writer.WriteStartElement("skippedResource");
                    writer.WriteAttributeString("name", info.Name);
                    writer.WriteAttributeString("reason", info.StatusText);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
            writer.WriteString("\r\n");
            writer.WriteEndElement();
            writer.WriteString("\r\n");
        }

        private void DumpClass(ObfuscatedClass classInfo)
        {
            if (classInfo.Status != ObfuscationStatus.Renamed)
            {
                Debug.Assert(classInfo.Status == ObfuscationStatus.Skipped,
                    "Status is expected to be either Renamed or Skipped.");
                writer.WriteStartElement("skippedClass");
                writer.WriteAttributeString("name", classInfo.Name);
                writer.WriteAttributeString("reason", classInfo.StatusText);
            }
            else
            {
                writer.WriteStartElement("renamedClass");
                writer.WriteAttributeString("oldName", classInfo.Name);
                writer.WriteAttributeString("newName", classInfo.StatusText);
            }

            int numRenamed = 0;
            foreach (KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods)
            {
                if (method.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpMethod(method.Key, method.Value);
                    numRenamed++;
                }
            }


            foreach (KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods)
            {
                if (method.Value.Status == ObfuscationStatus.Skipped)
                    DumpMethod(method.Key, method.Value);
            }


            foreach (KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields)
            {
                if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpField(writer, field.Key, field.Value);
                }
            }

            //

            foreach (KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpField(writer, field.Key, field.Value);
            }


            foreach (KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties)
            {
                if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpProperty(writer, field.Key, field.Value);
                }
            }


            foreach (KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpProperty(writer, field.Key, field.Value);
            }


            foreach (KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events)
            {
                if (field.Value.Status == ObfuscationStatus.Renamed)
                {
                    DumpEvent(writer, field.Key, field.Value);
                }
            }


            foreach (KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events)
            {
                if (field.Value.Status == ObfuscationStatus.Skipped)
                    DumpEvent(writer, field.Key, field.Value);
            }

            writer.WriteEndElement();
            writer.WriteString("\r\n");
        }

        private void DumpMethod(MethodKey key, ObfuscatedThing info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}(", info.Name);
            for (int i = 0; i < key.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");

                sb.Append(key.ParamTypes[i]);
            }

            sb.Append(")");

            if (info.Status == ObfuscationStatus.Renamed)
            {
                writer.WriteStartElement("renamedMethod");
                writer.WriteAttributeString("oldName", sb.ToString());
                writer.WriteAttributeString("newName", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
            else
            {
                writer.WriteStartElement("skippedMethod");
                writer.WriteAttributeString("name", sb.ToString());
                writer.WriteAttributeString("reason", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
        }

        private void DumpField(XmlWriter writer, FieldKey key, ObfuscatedThing info)
        {
            if (info.Status == ObfuscationStatus.Renamed)
            {
                writer.WriteStartElement("renamedField");
                writer.WriteAttributeString("oldName", info.Name);
                writer.WriteAttributeString("newName", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
            else
            {
                writer.WriteStartElement("skippedField");
                writer.WriteAttributeString("name", info.Name);
                writer.WriteAttributeString("reason", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
        }

        private void DumpProperty(XmlWriter writer, PropertyKey key, ObfuscatedThing info)
        {
            if (info.Status == ObfuscationStatus.Renamed)
            {
                writer.WriteStartElement("renamedProperty");
                writer.WriteAttributeString("oldName", info.Name);
                writer.WriteAttributeString("newName", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
            else
            {
                writer.WriteStartElement("skippedProperty");
                writer.WriteAttributeString("name", info.Name);
                writer.WriteAttributeString("reason", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
        }

        private void DumpEvent(XmlWriter writer, EventKey key, ObfuscatedThing info)
        {
            if (info.Status == ObfuscationStatus.Renamed)
            {
                writer.WriteStartElement("renamedEvent");
                writer.WriteAttributeString("oldName", info.Name);
                writer.WriteAttributeString("newName", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
            else
            {
                writer.WriteStartElement("skippedEvent");
                writer.WriteAttributeString("name", info.Name);
                writer.WriteAttributeString("reason", info.StatusText);
                writer.WriteEndElement();
                writer.WriteString("\r\n");
            }
        }

        public void Dispose()
        {
            writer.Close();
        }
    }



}
