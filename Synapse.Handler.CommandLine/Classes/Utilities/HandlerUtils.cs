using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.Handlers.CommandLine
{
    public static class HandlerUtils
    {
        public static T Deserialize<T>(String str)
        {
            T obj;
            if (str.Trim().StartsWith("<"))
                try
                {
                    obj = XmlHelpers.Deserialize<T>(new StringReader(str));
                }
                catch (Exception e)
                {
                    // Check Edge Case Of Yaml Document Starting With A "<" Character
                    try
                    {
                        obj = YamlHelpers.Deserialize<T>(new StringReader(str));
                    }
                    catch (Exception)
                    {
                        throw e;
                    }
                }
            else
                obj = YamlHelpers.Deserialize<T>(new StringReader(str));

            return obj;
        }

        public static String Serialize<T>(object obj)
        {
            String str = String.Empty;

            if (obj.GetType() == typeof(XmlNode[]))
            {
                StringBuilder sb = new StringBuilder();
                String type = typeof(T).Name;
                sb.Append("<" + type + ">");
                XmlNode[] nodes = (XmlNode[])obj;
                foreach (XmlNode node in nodes)
                    sb.Append(node.OuterXml);
                sb.Append("</" + type + ">");
                str = sb.ToString();
            }
            else if (obj.GetType() == typeof(Dictionary<object, object>))
                str = YamlHelpers.Serialize(obj);
            else
                str = obj.ToString();

            return str;
        }

        public static double ElapsedSeconds(this Stopwatch stopwatch)
        {
            return TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;
        }

        /// <summary>
        /// Converts the numeric exit code from the running process into the specified StatusType.
        /// If no config ValidExitCode strings are specified, the default behaviour is to return "Complete" when the exit 
        /// code is zero, and "Failure" when the exit code is non-zero.
        /// 
        /// Exit Code String Format : "OPERATOR VALUE1 [VALUE2] [STATUS]"
        /// Default STATUS when not specified : "Complete"
        /// 
        /// Valid Operator Values :
        ///     EQ, EQU, or EqualTo                 : Returns STATUS when exitCode is equal to VALUE1.
        ///     NE, NEQ, or NotEqualTo              : Returns STATUS when exitCode is not equal to VALUE1.
        ///     LT, LSS, or LessThan                : Returns STATUS when exitCode is less than VALUE1.
        ///     LE, LEQ, or LessThanOrEqualTo       : Returns STATUS when exitCode is less than or equal to VALUE1.
        ///     GT, GTR, or GreaterThan             : Returns STATUS when exitCode is greater than VALUE1.
        ///     GE, GEQ, or GreaterThanOrEqualTo    : Returns STATUS when exitCode is greater than or equal to VALUE1.
        ///     BT, BTW, or Between                 : Returns STATUS when exitCode is between VALUE1 and VALUE2 (inclusive).
        ///     NB, NBT, or NotBetween              : Returns STATUS when exitCode is not between VALUE1 and VALUE2 (exclusive).
        /// </summary>
        /// <param name="exitCode">Numeric exit code returned from the command.</param>
        /// <param name="validExitCodes">A List of Valid Exit Codes in the format specified in the summary.</param>
        /// <returns></returns>
        //  
        //
        public static StatusType GetStatusType(Int32 exitCode, List<String> validExitCodes)
        {
            StatusType returnStatus = StatusType.None;
            bool validComparisionFound = false;

            if (validExitCodes != null)
            {
                foreach (String validExitCode in validExitCodes)
                {
                    if (returnStatus == StatusType.None && !(String.IsNullOrWhiteSpace(validExitCode)))
                    {
                        try
                        {
                            String op = null;
                            int v1 = 0;
                            int v2 = 0;
                            String sts = null;

                            // Expected Format : "Operator Value1 [Value2] [Status]"
                            MatchCollection matches = Regex.Matches(validExitCode, @"^\s*(\S*)\s*(-?\d*)\s*(-?\d*)\s*(\S*).*$", RegexOptions.IgnoreCase);

                            op = matches[0].Groups[1].Value;
                            v1 = int.Parse(matches[0].Groups[2].Value);
                            if (!String.IsNullOrWhiteSpace(matches[0].Groups[3].Value))
                                v2 = int.Parse(matches[0].Groups[3]?.Value);
                            sts = matches[0].Groups[4]?.Value;
                            StatusType status = StatusType.Complete;
                            if (!String.IsNullOrWhiteSpace(sts))
                                status = (StatusType)Enum.Parse(typeof(StatusType), sts);

                            if (!String.IsNullOrWhiteSpace(op))
                            {
                                switch (op.ToUpper())
                                {
                                    case "EQ":
                                    case "EQU":
                                    case "EQUALTO":
                                    case "":
                                        validComparisionFound = true;
                                        if (exitCode == v1)
                                            returnStatus = status;
                                        break;
                                    case "NE":
                                    case "NEQ":
                                    case "NOTEQUALTO":
                                        validComparisionFound = true;
                                        if (exitCode != v1)
                                            returnStatus = status;
                                        break;
                                    case "LT":
                                    case "LSS":
                                    case "LESSTHAN":
                                        validComparisionFound = true;
                                        if (exitCode < v1)
                                            returnStatus = status;
                                        break;
                                    case "LE":
                                    case "LEQ":
                                    case "LESSTHANOREQUALTO":
                                        validComparisionFound = true;
                                        if (exitCode <= v1)
                                            returnStatus = status;
                                        break;
                                    case "GT":
                                    case "GTR":
                                    case "GREATERTHAN":
                                        validComparisionFound = true;
                                        if (exitCode > v1)
                                            returnStatus = status;
                                        break;
                                    case "GE":
                                    case "GEQ":
                                    case "GREATERTHANOREQUALTO":
                                        validComparisionFound = true;
                                        if (exitCode >= v1)
                                            returnStatus = status;
                                        break;
                                    case "BT":
                                    case "BTW":
                                    case "BETWEEN":
                                        validComparisionFound = true;
                                        int bv1 = Math.Min(v1, v2);
                                        int bv2 = Math.Max(v1, v2);
                                        if (bv1 <= exitCode && exitCode <= bv2)
                                            returnStatus = status;
                                        break;
                                    case "NB":
                                    case "NBT":
                                    case "NOTBETWEEN":
                                        validComparisionFound = true;
                                        int nbv1 = Math.Min(v1, v2);
                                        int nbv2 = Math.Max(v1, v2);
                                        if (nbv1 > exitCode || nbv2 < exitCode)
                                            returnStatus = status;
                                        break;
                                    default:
                                        throw new Exception("Unknown Operator [" + op + "].");
                                }
                            }


                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Invalid ExitCode Format [" + validExitCode + "].  Ignoring.  " + e.Message);
                        }

                    }
                }
            }

            // If No Valid Comparisions Were Specified, Default Action Is 0 = Succuess, Non-Zero = Failure.
            if (validComparisionFound && returnStatus == StatusType.None)
                returnStatus = StatusType.Failed;
            else if (!validComparisionFound && exitCode == 0)
                returnStatus = StatusType.Complete;
            else if (!validComparisionFound && exitCode != 0)
                returnStatus = StatusType.Failed;

            return returnStatus;
        }



    }
}
