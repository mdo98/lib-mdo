using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace System.IO
{
    public static class CrcCheck
    {
        #region Constants

        public const string CrcCheckedFileNamePattern_CrcGroup = "CRC";
        public const string CrcCheckedFileNamePattern_FileNameGroup = "Base";
        public const string CrcCheckedFileNamePattern_FileExtensionGroup = "Extension";

        public static readonly Regex CrcCheckedFileNamePatternCurrent = new Regex(
            @"((?# BOS)\A)" +
            @"((?# Filename)\s*(?<Base>[\s\S]*\S)\s*)" +
            @"((?# CRC preceded by a dot)\.\s*(?<CRC>[0-9a-fA-F]{8})\s*)" +
            @"((?# Optional extension)(?<Extension>(\.[\s\S]+)*))" +
            @"((?# EOS)\z)",
            RegexOptions.Compiled);
        public static readonly Regex CrcCheckedFileNamePatternLegacy1 = new Regex(
            @"((?# BOS)\A)" +
            @"((?# Filename)\s*(?<Base>[\s\S]*\S)\s*)" +
            @"((?# CRC square-bracketed)\s*\[\s*(?<CRC>[0-9a-fA-F]{8})\s*\]\s*)" +
            @"((?# Optional extension)(?<Extension>(\.[\s\S]+)*))" +
            @"((?# EOS)\z)",
            RegexOptions.Compiled);
        public static readonly Regex CrcCheckedFileNamePatternLegacy2 = new Regex(
            @"((?# BOS)\A)" +
            @"((?# CRC square-bracketed)\s*\[\s*(?<CRC>[0-9a-fA-F]{8})\s*\]\s*)" +
            @"((?# Filename)\s*(?<Base>[\s\S]*?\S)\s*)" +
            @"((?# Optional extension)(?<Extension>(\.\S+)*)\s*)" +
            @"((?# EOS)\z)",
            RegexOptions.Compiled);

        public static readonly Regex[] CrcCheckedFileNamePatterns =
        {
            CrcCheckedFileNamePatternCurrent,
            CrcCheckedFileNamePatternLegacy1,
            CrcCheckedFileNamePatternLegacy2,
        };

        #endregion Constants


        #region Public Methods

        public static bool CheckCrc(string filePath, bool rename, out uint? expectedCrcValue, out uint crcValue, out string crcCheckedFilePath, out bool? renamed)
        {
            string fileName = Path.GetFileName(filePath);
            string fileBaseName = null, fileExtension = null;
            expectedCrcValue = null;
            foreach (Regex crcCheckedFileNamePattern in CrcCheckedFileNamePatterns)
            {
                Match crcMatch = crcCheckedFileNamePattern.Match(fileName);
                if (crcMatch.Success)
                {
                    fileBaseName = crcMatch.Groups[CrcCheckedFileNamePattern_FileNameGroup].Value;
                    expectedCrcValue = Convert.ToUInt32(crcMatch.Groups[CrcCheckedFileNamePattern_CrcGroup].Value, 16);
                    fileExtension = crcMatch.Groups[CrcCheckedFileNamePattern_FileExtensionGroup].Value;
                    break;
                }
            }
            crcValue = CrcCalc.CalculateFromFile(filePath);
            crcCheckedFilePath = filePath;
            renamed = null;
            if (rename)
            {
                string crcCheckedFileName = string.Format(
                    "{0}.{1}{2}",
                    fileBaseName ?? Path.GetFileNameWithoutExtension(filePath),
                    crcValue.ToString("X8"),
                    fileExtension ?? Path.GetExtension(filePath));
                if (crcCheckedFileName != fileName)  // should rename
                {
                    crcCheckedFilePath = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        crcCheckedFileName);
                    try
                    {
                        File.Move(filePath, crcCheckedFilePath);
                        renamed = true;
                    }
                    catch
                    {
                        renamed = false;
                    }
                }
            }
            return expectedCrcValue.HasValue ? (crcValue == expectedCrcValue.Value) : true;
        }
        
        #endregion Public Methods
    }
}
