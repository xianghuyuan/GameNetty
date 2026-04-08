#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace GameNetty.EditorTools
{
    /// <summary>
    /// Zero-dependency xlsx reader/writer using System.IO.Compression + LINQ to XML.
    /// Handles Luban-format Excel files (##var/##type/##/data rows).
    /// </summary>
    internal static class LubanExcelRW
    {
        private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        #region Public API

        public static LubanTable Read(string filePath)
        {
            using var zip = ZipFile.Open(filePath, ZipArchiveMode.Read);

            string[] sharedStrings = ReadSharedStrings(zip);
            XElement sheet = ReadSheetXml(zip);

            var allRows = sheet.Element(Ns + "sheetData")?.Elements(Ns + "row").ToList();
            if (allRows == null || allRows.Count < 3)
            {
                UnityEngine.Debug.LogError($"Excel file format invalid: {filePath}");
                return null;
            }

            // Row 0 (1-based row 1): ##var + column names
            // Row 1 (1-based row 2): ##type + type definitions
            // Row 2 (1-based row 3): ## + descriptions
            // Row 3+ (1-based row 4+): data rows
            //
            // Column A = ##var/##type marker, actual data starts from column B (index 1)
            List<string> allColNames = ParseRowCells(allRows[0], sharedStrings);
            List<string> allColTypes = ParseRowCells(allRows[1], sharedStrings);

            // Schema: columns B onward (skip column A which is ##var/##type)
            var schema = new List<ColumnInfo>();
            for (int i = 1; i < allColNames.Count || i < allColTypes.Count; i++)
            {
                string name = i < allColNames.Count ? allColNames[i] : "";
                string typeStr = i < allColTypes.Count ? allColTypes[i] : "";
                string baseType = CleanType(typeStr);
                bool isId = schema.Count == 0; // First data column (B) is always the ID

                schema.Add(new ColumnInfo
                {
                    Name = name,
                    FullType = typeStr,
                    BaseType = baseType,
                    IsReadOnly = isId,
                });
            }

            // Parse data rows (skip first 3 header rows)
            var data = new List<Dictionary<string, string>>();
            for (int r = 3; r < allRows.Count; r++)
            {
                List<string> cells = ParseRowCells(allRows[r], sharedStrings);

                // Skip completely empty rows
                bool hasAnyData = false;
                for (int c = 1; c < cells.Count; c++)
                {
                    if (!string.IsNullOrEmpty(cells[c])) { hasAnyData = true; break; }
                }
                if (!hasAnyData) continue;

                var row = new Dictionary<string, string>();
                for (int c = 0; c < schema.Count; c++)
                {
                    // Schema column c maps to Excel column (c + 1), i.e. index (c + 1) in cells
                    int cellIdx = c + 1; // skip column A
                    string val = cellIdx < cells.Count ? cells[cellIdx] : "";
                    row[schema[c].Name] = val;
                }

                data.Add(row);
            }

            return new LubanTable
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Schema = schema,
                Data = data,
            };
        }

        public static void Write(string filePath, LubanTable table)
        {
            // Read into memory first, then write back with ZipArchiveMode.Update
            string ssContent = null;
            string sheetContent = null;

            using (var readZip = ZipFile.OpenRead(filePath))
            {
                ssContent = ReadEntryText(readZip, "xl/sharedStrings.xml");
                sheetContent = ReadEntryText(readZip, "xl/worksheets/sheet1.xml");
            }

            // Parse shared strings
            var ssDoc = XDocument.Parse(ssContent);
            var siElements = ssDoc.Root!.Elements(Ns + "si").ToList();
            var sharedStringList = new List<string>();
            foreach (var si in siElements)
            {
                var t = si.Element(Ns + "t");
                sharedStringList.Add(t?.Value ?? "");
            }

            // Parse sheet
            var sheetDoc = XDocument.Parse(sheetContent);
            var sheetData = sheetDoc.Root!.Element(Ns + "sheetData");
            var rows = sheetData!.Elements(Ns + "row").ToList();

            int existingDataRows = rows.Count - 3;

            // Update or add data rows
            for (int i = 0; i < table.Data.Count; i++)
            {
                var rowDict = table.Data[i];
                XElement row;

                if (i < existingDataRows)
                {
                    row = rows[i + 3];
                    row.Elements(Ns + "c").Remove();
                }
                else
                {
                    row = new XElement(Ns + "row");
                    row.SetAttributeValue("r", (i + 4).ToString());
                    sheetData.Add(row);
                }

                int col = 1; // Start from column B (A is empty for data)
                foreach (var colDef in table.Schema)
                {
                    string val = rowDict.TryGetValue(colDef.Name, out string v) ? v : "";
                    string colRef = ColumnIndexToLetter(col) + (i + 4);

                    var cell = new XElement(Ns + "c",
                        new XAttribute("r", colRef));

                    if (!string.IsNullOrEmpty(val))
                    {
                        bool isString = IsStringType(colDef.BaseType);
                        if (isString)
                        {
                            int ssIdx = GetOrAddSharedString(sharedStringList, val);
                            cell.Add(new XAttribute("t", "s"));
                            cell.Add(new XElement(Ns + "v", ssIdx.ToString()));
                        }
                        else
                        {
                            cell.Add(new XElement(Ns + "v", val));
                        }
                    }

                    row.Add(cell);
                    col++;
                }

                row.SetAttributeValue("r", (i + 4).ToString());
            }

            // Remove excess data rows
            for (int i = table.Data.Count; i < existingDataRows; i++)
            {
                rows[i + 3].Remove();
            }

            // Update shared strings XML
            ssDoc.Root!.Elements(Ns + "si").Remove();
            foreach (string s in sharedStringList)
            {
                ssDoc.Root.Add(new XElement(Ns + "si", new XElement(Ns + "t", s ?? "")));
            }
            ssDoc.Root.SetAttributeValue("count", sharedStringList.Count.ToString());
            ssDoc.Root.SetAttributeValue("uniqueCount", sharedStringList.Count.ToString());

            // Write back using ZipArchiveMode.Update
            using (var writeZip = ZipFile.Open(filePath, ZipArchiveMode.Update))
            {
                WriteEntry(writeZip, "xl/sharedStrings.xml", ssDoc.ToString());
                WriteEntry(writeZip, "xl/worksheets/sheet1.xml", sheetDoc.ToString());
            }
        }

        #endregion

        #region Internal

        private static string ReadEntryText(ZipArchive zip, string entryName)
        {
            var entry = zip.GetEntry(entryName);
            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static void WriteEntry(ZipArchive zip, string entryName, string content)
        {
            var entry = zip.GetEntry(entryName);
            using var stream = entry.Open();
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
            stream.SetLength(bytes.Length);
        }

        private static string[] ReadSharedStrings(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return Array.Empty<string>();

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);

            var siElements = doc.Root?.Elements(Ns + "si").ToList();
            if (siElements == null) return Array.Empty<string>();

            var strings = new string[siElements.Count];
            for (int i = 0; i < siElements.Count; i++)
            {
                var t = siElements[i].Element(Ns + "t");
                strings[i] = t?.Value ?? "";
            }
            return strings;
        }

        private static XElement ReadSheetXml(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml");
            using var stream = entry.Open();
            return XDocument.Load(stream).Root;
        }

        /// <summary>
        /// Parse a row's cells and return values indexed by column position (0-based).
        /// Handles sparse columns (e.g., missing D1 in header but D4 in data) by
        /// using the cell reference (A, B, C, ...) to determine the correct position.
        /// </summary>
        private static List<string> ParseRowCells(XElement row, string[] sharedStrings)
        {
            var cells = row.Elements(Ns + "c").ToList();

            // Find max column index to pre-allocate
            int maxCol = 0;
            foreach (var cell in cells)
            {
                string refStr = cell.Attribute("r")?.Value ?? "";
                string colStr = ColumnLettersFromRef(refStr);
                int colIdx = LetterToColumnIndex(colStr);
                if (colIdx > maxCol) maxCol = colIdx;
            }

            var result = new List<string>(maxCol + 1);
            for (int i = 0; i <= maxCol; i++)
                result.Add("");

            foreach (var cell in cells)
            {
                string refStr = cell.Attribute("r")?.Value ?? "";
                string colStr = ColumnLettersFromRef(refStr);
                int colIdx = LetterToColumnIndex(colStr);

                string type = cell.Attribute("t")?.Value ?? "";
                var v = cell.Element(Ns + "v");
                string val = v?.Value ?? "";

                if (type == "s" && int.TryParse(val, out int idx))
                {
                    val = idx < sharedStrings.Length ? sharedStrings[idx] : val;
                }

                if (colIdx >= 0 && colIdx < result.Count)
                    result[colIdx] = val;
            }

            return result;
        }

        /// <summary>
        /// Extract column letters from a cell reference (e.g., "B4" → "B", "AA12" → "AA").
        /// </summary>
        private static string ColumnLettersFromRef(string cellRef)
        {
            int i = 0;
            while (i < cellRef.Length && !char.IsDigit(cellRef[i]))
                i++;
            return cellRef.Substring(0, i);
        }

        /// <summary>
        /// Convert column letters to 0-based index (e.g., "A"→0, "B"→1, "Z"→25, "AA"→26).
        /// </summary>
        private static int LetterToColumnIndex(string letters)
        {
            int result = 0;
            foreach (char c in letters)
            {
                result = result * 26 + (c - 'A' + 1);
            }
            return result - 1;
        }

        private static int GetOrAddSharedString(List<string> sharedStrings, string value)
        {
            int idx = sharedStrings.IndexOf(value);
            if (idx >= 0) return idx;

            sharedStrings.Add(value);
            return sharedStrings.Count - 1;
        }

        private static string CleanType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr)) return "string";

            int ampIdx = typeStr.IndexOf('&');
            if (ampIdx > 0) typeStr = typeStr.Substring(0, ampIdx);

            int hashIdx = typeStr.IndexOf('#');
            if (hashIdx > 0) typeStr = typeStr.Substring(0, hashIdx);

            return typeStr.Trim();
        }

        private static bool IsStringType(string baseType)
        {
            if (string.IsNullOrEmpty(baseType)) return true;
            return baseType is "string" or "text";
        }

        private static string ColumnIndexToLetter(int col)
        {
            string result = "";
            while (col > 0)
            {
                col--;
                result = (char)('A' + (col % 26)) + result;
                col /= 26;
            }
            return result;
        }

        #endregion
    }

    #region Data Structures

    internal sealed class LubanTable
    {
        public string FilePath;
        public string FileName;
        public List<ColumnInfo> Schema;
        public List<Dictionary<string, string>> Data;
    }

    internal sealed class ColumnInfo
    {
        public string Name;
        public string FullType;
        public string BaseType;
        public bool IsReadOnly;
    }

    #endregion
}
#endif
