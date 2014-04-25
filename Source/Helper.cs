﻿using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using woanware;

namespace autorunner
{
    /// <summary>
    /// Contains helper routines to keep the UI objects clean
    /// </summary>
    internal class Helper
    {
        /// <summary>
        /// Extracts information from the sigcheck output
        /// </summary>
        /// <param name="autoRunEntry"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static AutoRunEntry ParseSigCheckOutput(AutoRunEntry autoRunEntry, 
                                                       string output)
        {
            Regex regex = new Regex(@"\s+Verified:\s+?(.*)", RegexOptions.IgnoreCase);
            Match match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.Verified = match.Groups[1].Value.Trim();
            }

            regex = new Regex(@"\s+Publisher:\s+?(.*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.FilePublisher = match.Groups[1].Value.Trim();
            }

            regex = new Regex(@"\s+Description:\s+?(.*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.FileDescription = match.Groups[1].Value.Trim();
            }

            regex = new Regex(@"\s+Strong Name:\s+?(.*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.StrongName = match.Groups[1].Value.Trim();
            }

            regex = new Regex(@"\s+Version:\s+?(.*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.Version = match.Groups[1].Value.Trim();
            }

            regex = new Regex(@"\s+File version:\s+?(.*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.FileVersion = match.Groups[1].Value.Trim();
            }

            regex = new Regex(@"\s+File date:\s+?(.*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.FileDate = DateTime.Parse(match.Groups[1].Value.Trim());
            }

            regex = new Regex(@"\s+Signing date:\s+?(.*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success == true)
            {
                autoRunEntry.SigningDate = DateTime.Parse(match.Groups[1].Value.Trim());
            }

            return autoRunEntry;
        }

        /// <summary>
        /// Attempts to strip the binary (and full path) from the parameters. It also replaces 
        /// the path to the binary with the user selected one e.g. the image is mounted 
        /// at J:\ so C:\someprog.exe becomes J:\someprog.exe
        /// </summary>
        /// <param name="driveMappings"></param>
        /// <param name="autoRunEntry"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static AutoRunEntry GetFilePathWithNoParameters(List<DriveMapping> driveMappings, 
                                                               AutoRunEntry autoRunEntry,
                                                               string path)
        {
            if (path.Trim().Length == 0)
            {
                return null;
            }

            string parameters = string.Empty;
            if (path.StartsWith("\"") == true)
            {
                string temp = path.Substring(1);
                int index = temp.IndexOf('"');
                if (index > -1)
                {
                    autoRunEntry.Parameters = temp.Substring(index + 1, temp.Length - (index + 1));
                    autoRunEntry.FilePath = temp.Substring(0, index);
                }

                autoRunEntry.FilePath = Helper.NormalisePath(driveMappings, autoRunEntry.FilePath);
            }
            else
            {
                autoRunEntry = GetFilePathWithNoParametersEdgeCases(driveMappings, autoRunEntry, path);
                if (autoRunEntry.FilePath.Length > 0)
                {
                    return autoRunEntry;
                }

                string[] parts = path.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts[parts.Length - 1].IndexOf(" ") > -1)
                {
                    string tempFile = parts[parts.Length - 1].Substring(0, parts[parts.Length - 1].IndexOf(" "));
                    autoRunEntry.Parameters = parts[parts.Length - 1].Substring(parts[parts.Length - 1].IndexOf(" "));
                    path = string.Join("\\", parts.Slice(0, parts.Length - 1));
                    if (path.StartsWith("\"") == true)
                    {
                        path = path.Substring(1);
                    }

                    tempFile = woanware.Path.ReplaceIllegalPathChars(tempFile, string.Empty);
                    autoRunEntry.FilePath = System.IO.Path.Combine(path, tempFile);
                }
                else
                {
                    if (path.StartsWith("\"") == true)
                    {
                        path = path.Substring(1);
                    }

                    if (path.EndsWith("\"") == true)
                    {
                        path = path.Substring(0, path.Length - 1);
                    }

                    autoRunEntry.FilePath = path;
                }

                autoRunEntry.FilePath = Helper.NormalisePath(driveMappings, autoRunEntry.FilePath);
            }

            return autoRunEntry;
        }

        /// <summary>
        /// T
        /// </summary>
        /// <param name="driveMappings"></param>
        /// <param name="autoRunEntry"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static AutoRunEntry GetFilePathWithNoParametersEdgeCases(List<DriveMapping> driveMappings,
                                                                         AutoRunEntry autoRunEntry,
                                                                         string path)
        {
            int indexOf = path.IndexOf("%SystemRoot%\\system32\\regsvr32.exe", StringComparison.InvariantCultureIgnoreCase);
            if (indexOf == 0)
            {
                autoRunEntry.FilePath = "%SystemRoot%\\system32\\regsvr32.exe";
                autoRunEntry.Parameters = path.Substring("%SystemRoot%\\system32\\regsvr32.exe".Length);
                autoRunEntry.FilePath = Helper.NormalisePath(driveMappings, autoRunEntry.FilePath);
                return autoRunEntry;
            }

            indexOf = path.IndexOf("Windows\\system32\\Rundll32.exe", StringComparison.InvariantCultureIgnoreCase);
            if (indexOf > -1)
            {
                autoRunEntry.FilePath = path.Substring(0, indexOf + "Windows\\system32\\Rundll32.exe".Length);
                autoRunEntry.Parameters = path.Substring(autoRunEntry.FilePath.Length);
                autoRunEntry.FilePath = Helper.NormalisePath(driveMappings, autoRunEntry.FilePath);
                return autoRunEntry;
            }

            indexOf = path.IndexOf("Windows\\SysWOW64\\Rundll32.exe", StringComparison.InvariantCultureIgnoreCase);
            if (indexOf > -1)
            {
                autoRunEntry.FilePath = path.Substring(0, indexOf + "Windows\\SysWOW64\\Rundll32.exe".Length);
                autoRunEntry.Parameters = path.Substring(autoRunEntry.FilePath.Length);
                autoRunEntry.FilePath = Helper.NormalisePath(driveMappings, autoRunEntry.FilePath);
                return autoRunEntry;
            }

            indexOf = path.IndexOf("regsvr32.exe", StringComparison.InvariantCultureIgnoreCase);
            if (indexOf == 0)
            {
                autoRunEntry.FilePath = "%SystemRoot%\\system32\\regsvr32.exe";
                autoRunEntry.Parameters = path.Substring("regsvr32.exe".Length);
                autoRunEntry.FilePath = Helper.NormalisePath(driveMappings, autoRunEntry.FilePath);
                return autoRunEntry;
            }

            indexOf = path.IndexOf("bin/pg_ctl.exe", StringComparison.InvariantCultureIgnoreCase);
            if (indexOf > 0)
            {
                autoRunEntry.FilePath = path.Substring(0, indexOf + "bin/pg_ctl.exe".Length);
                autoRunEntry.Parameters = path.Substring(indexOf + "bin/pg_ctl.exe".Length);
                autoRunEntry.FilePath = Helper.NormalisePath(driveMappings, autoRunEntry.FilePath);
                return autoRunEntry;
            }

            return autoRunEntry;
        }

        /// <summary>
        /// Attempts to replace all the different ways that binary paths are 
        /// stored in the reg with ones that can be used to access the binary
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalisePath(List<DriveMapping> driveMappings, string path)
        {
            //\??\C:\Windows\system32\Drivers\DgiVecp.sys
            //%SystemRoot%\system32\svchost.exe -k defragsvc
            //"C:\Program Files (x86)\GNU\GnuPG2\dirmngr.exe" --service
            //\SystemRoot\system32\drivers\dmvsc.sys
            //system32\drivers\drmkaud.sys
            //C:\Windows\SysWOW64\wex4962\EMCliSrv.exe
            //%windir%\system32\svchost.exe -k ftpsvc
            //%PROGRAMFILES%\Windows Media Player\wmpnetwk.exe
            //"C:\\Program Files\\MySQL\\MySQL Server 5.5\\bin\\mysqld\" --defaults-file=\"C:\\ProgramData\\MySQL\\MySQL Server 5.5"

            Regex regex = new Regex(@"^\w:\\", RegexOptions.IgnoreCase);
            Match match = regex.Match(path);
            if (match.Success == true)
            {
                string tempDrive = System.IO.Path.GetPathRoot(path);

                var drive = (from d in driveMappings where d.OriginalDrive.ToLower() == tempDrive.ToLower() select d).SingleOrDefault();
                if (drive == null)
                {
                    return string.Empty;
                }

                path = path.ToLower().Replace(tempDrive.ToLower(), drive.MappedDrive);
            }

            // C:/Program Files/PostgreSQL/9.1/bin/pg_ctl.exe 
            regex = new Regex(@"^\w:/", RegexOptions.IgnoreCase);
            match = regex.Match(path);
            if (match.Success == true)
            {
                string tempDrive = System.IO.Path.GetPathRoot(path);

                var drive = (from d in driveMappings where d.OriginalDrive.ToLower() == tempDrive.ToLower() select d).SingleOrDefault();
                if (drive == null)
                {
                    return string.Empty;
                }

                path = path.ToLower().Replace(tempDrive.ToLower(), drive.MappedDrive);
            }

            var windowsDrive = (from d in driveMappings where d.IsWindowsDrive == true select d).SingleOrDefault();

            if (path.ToLower().IndexOf("%systemroot%", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                path = path.ToLower().Replace("%systemroot%", System.IO.Path.Combine(windowsDrive.MappedDrive, "Windows"));
            }

            if (path.ToLower().IndexOf(@"\systemroot", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                path = path.ToLower().Replace(@"\systemroot", System.IO.Path.Combine(windowsDrive.MappedDrive, "Windows"));
            }

            if (path.ToLower().IndexOf(@"system32", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                path = System.IO.Path.Combine(windowsDrive.MappedDrive, "Windows", path);
            }

            if (path.ToLower().IndexOf(@"%programfiles%", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                path = path.ToLower().Replace(@"%programfiles%", System.IO.Path.Combine(windowsDrive.MappedDrive, "Program Files"));
            }

            if (path.ToLower().IndexOf(@"%programfiles(x86)%", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                path = path.ToLower().Replace(@"%programfiles(x86)%", System.IO.Path.Combine(windowsDrive.MappedDrive, "Program Files (x86)"));
            }

            if (path.ToLower().IndexOf(@"%windir%", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                path = path.ToLower().Replace(@"%windir%", System.IO.Path.Combine(windowsDrive.MappedDrive, "Windows"));
            }

            if (path.ToLower().IndexOf(@"\??\", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                path = path.Substring(4, path.Length - 4);
                string tempDrive = System.IO.Path.GetPathRoot(path);
                var drive = (from d in driveMappings where d.OriginalDrive.ToLower() == tempDrive.ToLower() select d).SingleOrDefault();
                if (drive == null)
                {
                    return string.Empty;
                }

                path = path.ToLower().Replace(tempDrive.ToLower(), drive.MappedDrive);
            }

            return path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectListView"></param>
        public static void AutoResizeListColumns(ObjectListView objectListView)
        {
            if (objectListView.Items.Count == 0)
            {
                foreach (OLVColumn column in objectListView.Columns)
                {
                    column.AutoResize(System.Windows.Forms.ColumnHeaderAutoResizeStyle.HeaderSize);
                }
            }
            else
            {
                foreach (OLVColumn column in objectListView.Columns)
                {
                    column.AutoResize(System.Windows.Forms.ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="column"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static string CopyToClipboard(Global.Columns column, AutoRunEntry entry)
        {
            switch (column)
            {
                case Global.Columns.Description:
                    Clipboard.SetText(entry.FileDescription);
                    return "Copied \"Description\" to clipboard";

                case Global.Columns.FileDate:
                    Clipboard.SetText(entry.FileDateText);
                    return "Copied \"File Date\" to clipboard";

                case Global.Columns.FileName:
                    Clipboard.SetText(entry.FileName);
                    return "Copied \"File Name\" to clipboard";

                case Global.Columns.FilePath:
                    Clipboard.SetText(entry.FilePath);
                    return "Copied \"File Path\" to clipboard";

                case Global.Columns.Info:
                    Clipboard.SetText(entry.Info);
                    return "Copied \"Info\" to clipboard";

                case Global.Columns.Md5:
                    Clipboard.SetText(entry.Md5);
                    return "Copied \"MD5\" to clipboard";

                case Global.Columns.Parameters:
                    Clipboard.SetText(entry.Parameters);
                    return "Copied \"Parameters\" to clipboard";

                case Global.Columns.Path:

                    Clipboard.SetText(entry.Path);
                    return "Copied \"Path\" to clipboard";

                case Global.Columns.Publisher:
                    Clipboard.SetText(entry.FilePublisher);
                    return "Copied \"Publisher\" to clipboard";

                case Global.Columns.SigningDate:
                    Clipboard.SetText(entry.SigningDateText);
                    return "Copied \"SigningDate\" to clipboard";

                case Global.Columns.Type:
                    Clipboard.SetText(entry.Type);
                    return "Copied \"Type\" to clipboard";

                case Global.Columns.Version:
                    Clipboard.SetText(entry.Version);
                    return "Copied \"Version\" to clipboard";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="file"></param>
        /// <param name="data"></param>
        public static void WriteErrorToLog(string message, 
                                           string file, 
                                           string data)
        {
            if (System.IO.Directory.Exists(Misc.GetUserDataDirectory()) == false)
            {
                System.IO.Directory.CreateDirectory(Misc.GetUserDataDirectory());
            }

            IO.WriteTextToFile(string.Format("{0}: File: {1}", DateTime.Now.ToString("s"), file), System.IO.Path.Combine(Misc.GetUserDataDirectory(), "Errors.txt"), true);
            IO.WriteTextToFile(string.Format("{0}: Data: {1}", DateTime.Now.ToString("s"), data), System.IO.Path.Combine(Misc.GetUserDataDirectory(), "Errors.txt"), true);
            IO.WriteTextToFile(string.Format("{0}: Error: {1}", DateTime.Now.ToString("s"), message), System.IO.Path.Combine(Misc.GetUserDataDirectory(), "Errors.txt"), true);
        }
    }
}
