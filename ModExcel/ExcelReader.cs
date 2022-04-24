using Abraham.Office;
using ExcelDataReader;
using System;
using System.IO;

namespace Abraham.Office
{
	// read this:
	// https://github.com/ExcelDataReader/ExcelDataReader
	//
	// Important note on .NET Core
	// By default, ExcelDataReader throws a NotSupportedException "No data is available for encoding 1252." 
	// on .NET Core.
	// 
	// To fix, add a dependency to the package System.Text.Encoding.CodePages and then add code to register 
	// the code page provider during application initialization (f.ex in Startup.cs):
	// System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
	// 
	// This is required to parse strings in binary BIFF2-5 Excel documents encoded with DOS-era code pages. 
	// These encodings are registered by default in the full .NET Framework, but not on .NET Core.

	public class ExcelReader
	{
		public static void RegisterCodepageProvider()
		{
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		}

		public static string ReadCellValueFromExcelFile(string filename, string cellName)
	    {
            var converter    = new ExcelCellNamesConverter(cellName);
            var wantedRow    = converter.Row;
            var wantedColumn = converter.Column;
            string cellValue = "";
    	    System.Diagnostics.Debug.WriteLine($"ModExcel: reading cell '{cellName}' from file '{filename}' (row {wantedRow} column {wantedColumn})");

            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    //// --------------------- VARIANTE 1 ------------------------------
                    // 1. Use the reader methods
                    do
                    {
                        int currentRow = 0;
                        while (reader.Read())
                        {
                            currentRow++;
                            if (currentRow == wantedRow)
							{
                                var value = reader.GetValue((int)wantedColumn-1);
                                cellValue = Convert.ToString(value);
    						    System.Diagnostics.Debug.WriteLine($"cell value is '{cellValue}'");
                                return cellValue;
							}
                        }
                    } 
                    while (reader.NextResult());

                    //// --------------------- VARIANTE 2 ------------------------------
                    //// 2. Use the AsDataSet extension method
                    //var result = reader.AsDataSet();
                    //// The result of each spreadsheet is in result.Tables
                }
            }

            return cellValue;
	    }
	}
}
