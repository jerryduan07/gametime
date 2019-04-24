/*************************************************************************************************
 * Configuration                                                                                 *
 * ============================================================================================= *
 * This file contains the class that maintains all of the constants that will be used in         *
 * various places in the GameTime code. This file is essentially a repository of constants,      *
 * made for ease of maintenance.                                                                 *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.IO;
using System.Xml;


namespace Utilities
{
    /// <summary>
    /// This class maintains all of the constants that will be used in various places in
    /// the GameTime code. This file is essentially a repository of constants, made for
    /// ease of maintenance.
    /// </summary>
    public class Configuration
    {
        #region Constructor

        /// <summary>
        /// Constructor for a Configuration object.
        /// </summary>
        public Configuration()
        {
            WEBSITE_URL = "";
            VERSION = "";
            LATEST_VERSION_INFO_URL = "";

            configFile = "";
            configDir = "";

            WORD_BITSIZE = 32;
            WORD_BYTESIZE = 4;
            ENDIANNESS = Endianness.LITTLE;

            ANNOTATE_ASSUME = "";
            ANNOTATE_SIMULATE = "";

            IDENT_AGGREGATE = "";
            IDENT_CONSTRAINT = "";
            IDENT_EFC = "";
            IDENT_FIELD = "";
            IDENT_TEMPINDEX = "";
            IDENT_TEMPPTR = "";
            IDENT_TEMPVAR = "";

            TEMP_PROJECT_CONFIG = "";
            TEMP_MERGED = "";
            TEMP_LOOP_CONFIG = "";

            TEMP_SUFFIX = "";

            TEMP_SUFFIX_MERGED = "";
            TEMP_SUFFIX_UNROLLED = "";
            TEMP_SUFFIX_INLINED = "";
            TEMP_SUFFIX_LINE_NUMS = "";

            TEMP_PHX_CREATE_DAG = "";
            TEMP_DAG = "";
            TEMP_DAG_ID_MAP = "";

            TEMP_PHX_IR = "";
            TEMP_PHX_FIND_CONDITIONS = "";

            TEMP_PATH_ILP_PROBLEM = "";
            TEMP_PATH_NODES = "";
            TEMP_PATH_LINE_NUMBERS = "";
            TEMP_PATH_CONDITIONS = "";
            TEMP_PATH_CONDITION_EDGES = "";
            TEMP_PATH_CONDITION_TRUTHS = "";
            TEMP_PATH_ARRAY_ACCESSES = "";
            TEMP_PATH_AGG_INDEX_EXPRS = "";
            TEMP_PATH_PREDICTED_VALUE = "";
            TEMP_PATH_MEASURED_VALUE = "";
            TEMP_PATH_ALL = "";

            TEMP_PATH_QUERY = "";
            TEMP_SMT_MODEL = "";
            TEMP_PATH_QUERY_ALL = "";

            TEMP_CASE = "";

            TEMP_BASIS_MATRIX = "";
            TEMP_MEASUREMENT = "";
            TEMP_BASIS_VALUES = "";
            TEMP_DAG_WEIGHTS = "";
            TEMP_DISTRIBUTION = "";

            TOOL_PHOENIX = "";
            TOOL_CIL = "";

            SIMULATOR_TOOL_GNU_ARM = "";
            SIMULATOR_PTARM = "";
        }

        #endregion

        #region GameTime Information

        /// <summary>
        /// URL of the website for GameTime.
        /// </summary>
        public string WEBSITE_URL;

        /// <summary>
        /// Current version number of GameTime.
        /// </summary>
        public string VERSION;

        /// <summary>
        /// URL that provides information about the latest version of GameTime.
        /// </summary>
        public string LATEST_VERSION_INFO_URL;

        #endregion

        #region File Information

        /// <summary>
        /// Full location of the GameTime configuration file.
        /// </summary>
        public string configFile;

        /// <summary>
        /// Directory that contains the GameTime configuration file.
        /// </summary>
        public string configDir;

        #endregion

        #region Memory Layout Information

        /// <summary>
        /// Word size on the machine that GameTime is being run on (in bits). This value
        /// should be changed if GameTime will be run on a non-32-bit machine.
        /// </summary>
        public uint WORD_BITSIZE;

        /// <summary>
        /// Word size on the machine that GameTime is being run on (in bytes). This value
        /// should be changed if GameTime will be run on a non-32-bit machine.
        /// </summary>
        public uint WORD_BYTESIZE;

        /// <summary>
        /// Enumeration that lists the possible types of endianness of the target machine.
        /// </summary>
        public enum Endianness
        {
            /// <summary>
            /// Big-endian machine.
            /// </summary>
            BIG,

            /// <summary>
            /// Little-endian machine.
            /// </summary>
            LITTLE
        };

        /// <summary>
        /// Endianness of the target machine.
        /// </summary>
        public Endianness ENDIANNESS;

        #endregion

        #region Annotations

        /// <summary>
        /// Annotation that is used when additional conditions need to be provided to GameTime.
        /// </summary>
        public string ANNOTATE_ASSUME;

        public string ANNOTATE_SIMULATE;

        #endregion

        #region Special Identifiers

        /* The special identifiers are described in the default GameTime
         * configuration XML file provided in the source directory. */

        public string IDENT_AGGREGATE;
        public string IDENT_CONSTRAINT;
        public string IDENT_EFC;
        public string IDENT_FIELD;
        public string IDENT_TEMPINDEX;
        public string IDENT_TEMPPTR;
        public string IDENT_TEMPVAR;

        #endregion

        #region Temporary Files and Folders

        /* The names and prefixes of temporary files and folders are described in the default
         * GameTime configuration XML file provided in the source directory (config.xml). */

        public string TEMP_PROJECT_CONFIG;
        public string TEMP_MERGED;
        public string TEMP_LOOP_CONFIG;

        public string TEMP_SUFFIX;

        public string TEMP_SUFFIX_MERGED;
        public string TEMP_SUFFIX_UNROLLED;
        public string TEMP_SUFFIX_INLINED;
        public string TEMP_SUFFIX_LINE_NUMS;

        public string TEMP_PHX_CREATE_DAG;
        public string TEMP_DAG;
        public string TEMP_DAG_ID_MAP;

        public string TEMP_PHX_IR;
        public string TEMP_PHX_FIND_CONDITIONS;

        public string TEMP_PATH_ILP_PROBLEM;
        public string TEMP_PATH_NODES;
        public string TEMP_PATH_LINE_NUMBERS;
        public string TEMP_PATH_CONDITIONS;
        public string TEMP_PATH_CONDITION_EDGES;
        public string TEMP_PATH_CONDITION_TRUTHS;
        public string TEMP_PATH_ARRAY_ACCESSES;
        public string TEMP_PATH_AGG_INDEX_EXPRS;
        public string TEMP_PATH_PREDICTED_VALUE;
        public string TEMP_PATH_MEASURED_VALUE;
        public string TEMP_PATH_ALL;

        public string TEMP_PATH_QUERY;
        public string TEMP_SMT_MODEL;
        public string TEMP_PATH_QUERY_ALL;

        public string TEMP_CASE;

        public string TEMP_BASIS_MATRIX;
        public string TEMP_MEASUREMENT;
        public string TEMP_BASIS_VALUES;
        public string TEMP_DAG_WEIGHTS;
        public string TEMP_DISTRIBUTION;

        #endregion

        #region Tools

        /// <summary>
        /// Absolute location of the Phoenix DLL.
        /// </summary>
        public string TOOL_PHOENIX;

        /// <summary>
        /// Absolute location of the directory that contains the CIL source code.
        /// </summary>
        public string TOOL_CIL;

        #endregion

        #region SMT Solvers

        /// <summary>
        /// Absolute location of the Boolector executable.
        /// </summary>
        public string SOLVER_BOOLECTOR;

        /// <summary>
        /// Absolute location of the Python frontend of Z3, the SMT solver from Microsoft.
        /// </summary>
        public string SOLVER_Z3;

        #endregion

        #region Simulators

        /// <summary>
        /// Absolute location of the directory that contains the GNU ARM toolchain.
        /// </summary>
        public string SIMULATOR_TOOL_GNU_ARM;

        /// <summary>
        /// Absolute location of the directory that contains the PTARM simulator.
        /// </summary>
        public string SIMULATOR_PTARM;

        #endregion

        #region I/O Functions

        /// <summary>
        /// Obtains the text from the XML node provided.
        /// </summary>
        ///
        /// <param name="node">Node to obtain the text content from.</param>
        /// <returns>The text content of the node provided.</returns>
        public static string GetXmlText(XmlNode node)
        {
            return node.InnerText.Trim();
        }

        /// <summary>
        /// Reads the GameTime configuration values from the input XML file provided.
        /// </summary>
        ///
        /// <param name="configXmlFile">Path to file that contains GameTime
        /// configuration information.</param>
        /// <returns>Configuration object that contains information from
        /// the file provided.</returns>
        public static Configuration ReadConfigFile(string configXmlFile) {
            if (!File.Exists(configXmlFile))
            {
                Console.Out.WriteLine("PHOENIX: Cannot find configuration file: {0}",
                    configXmlFile);
                Environment.Exit(2);
                return null;
            }
            else
            {
                Configuration config = new Configuration();

                XmlDocument configXmlDoc = new XmlDocument();
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader configXmlReader = XmlReader.Create(configXmlFile, readerSettings);
                configXmlDoc.Load(configXmlReader);

                /* Get the absolute path of the file and the directory that contains the file. */
                config.configFile = Path.GetFullPath(configXmlFile);
                config.configDir = Path.GetDirectoryName(config.configFile);

                XmlElement configDocElt = configXmlDoc.DocumentElement;

                /* Process GameTime information. */
                XmlNodeList gameTimeInfo = configDocElt.GetElementsByTagName("gametime");
                XmlNode gameTimeInfoNode = gameTimeInfo[0];

                foreach (XmlNode node in gameTimeInfoNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        switch (nodeName)
                        {
                            case "website-url":
                                config.WEBSITE_URL = nodeText;
                                break;
                            case "version":
                                config.VERSION = nodeText;
                                break;
                            case "latest-version-info-url":
                                config.LATEST_VERSION_INFO_URL = nodeText;
                                break;
                            default:
                                Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", nodeName);
                                Environment.Exit(2);
                                return null;
                        }
                    }
                }

                /* Process the memory layout information. */
                XmlNodeList memoryInfo = configDocElt.GetElementsByTagName("memory");
                XmlNode memoryInfoNode = memoryInfo[0];

                foreach (XmlNode node in memoryInfoNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        switch (nodeName)
                        {
                            case "bitsize":
                                config.WORD_BITSIZE = Convert.ToUInt32(nodeText);
                                config.WORD_BYTESIZE = config.WORD_BITSIZE / 8;
                                break;
                            case "endianness":
                                nodeText = nodeText.ToLower();
                                config.ENDIANNESS =
                                    (nodeText.Equals("big")) ? Endianness.BIG : Endianness.LITTLE;
                                break;
                            default:
                                Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", nodeName);
                                Environment.Exit(2);
                                return null;
                        }
                    }
                }

                /* Process the annotations that can be added to the code under analysis. */
                XmlNodeList annotations = configDocElt.GetElementsByTagName("annotations");
                XmlNode annotationsNode = annotations[0];

                foreach (XmlNode node in annotationsNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        switch (nodeName)
                        {
                            case "assume":
                                config.ANNOTATE_ASSUME = nodeText;
                                break;
                            case "simulate":
                                config.ANNOTATE_SIMULATE = nodeText;
                                break;
                            default:
                                Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", nodeName);
                                Environment.Exit(2);
                                return null;
                        }
                    }
                }

                /* Process the special identifiers. */
                XmlNodeList idents = configDocElt.GetElementsByTagName("identifiers");
                XmlNode identsNode = idents[0];

                foreach (XmlNode node in identsNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        switch (nodeName)
                        {
                            case "aggregate":
                                config.IDENT_AGGREGATE = nodeText;
                                break;
                            case "constraint":
                                config.IDENT_CONSTRAINT = nodeText;
                                break;
                            case "efc":
                                config.IDENT_EFC = nodeText;
                                break;
                            case "field":
                                config.IDENT_FIELD = nodeText;
                                break;
                            case "tempindex":
                                config.IDENT_TEMPINDEX = nodeText;
                                break;
                            case "tempptr":
                                config.IDENT_TEMPPTR = nodeText;
                                break;
                            case "tempvar":
                                config.IDENT_TEMPVAR = nodeText;
                                break;
                            default:
                                Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", nodeName);
                                Environment.Exit(2);
                                return null;
                        }
                    }
                }

                /* Process the names for temporary files and folders that
                 * are generated during the GameTime toolflow.. */
                XmlNodeList temps = configDocElt.GetElementsByTagName("temps");
                XmlNode tempsNode = temps[0];

                foreach (XmlNode node in tempsNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        switch (nodeName)
                        {
                            case "project-config":
                                config.TEMP_PROJECT_CONFIG = nodeText;
                                break;
                            case "merged":
                                config.TEMP_MERGED = nodeText;
                                break;
                            case "loop-config":
                                config.TEMP_LOOP_CONFIG = nodeText;
                                break;

                            case "suffix":
                                config.TEMP_SUFFIX = nodeText;
                                break;

                            case "suffix-merged":
                                config.TEMP_SUFFIX_MERGED = nodeText;
                                break;
                            case "suffix-unrolled":
                                config.TEMP_SUFFIX_UNROLLED = nodeText;
                                break;
                            case "suffix-inlined":
                                config.TEMP_SUFFIX_INLINED = nodeText;
                                break;
                            case "suffix-line-nums":
                                config.TEMP_SUFFIX_LINE_NUMS = nodeText;
                                break;

                            case "phx-create-dag":
                                config.TEMP_PHX_CREATE_DAG = nodeText;
                                break;
                            case "dag":
                                config.TEMP_DAG = nodeText;
                                break;
                            case "dag-id-map":
                                config.TEMP_DAG_ID_MAP = nodeText;
                                break;

                            case "phx-ir":
                                config.TEMP_PHX_IR = nodeText;
                                break;
                            case "phx-find-conditions":
                                config.TEMP_PHX_FIND_CONDITIONS = nodeText;
                                break;

                            case "path-ilp-problem":
                                config.TEMP_PATH_ILP_PROBLEM = nodeText;
                                break;
                            case "path-nodes":
                                config.TEMP_PATH_NODES = nodeText;
                                break;
                            case "path-line-numbers":
                                config.TEMP_PATH_LINE_NUMBERS = nodeText;
                                break;
                            case "path-conditions":
                                config.TEMP_PATH_CONDITIONS = nodeText;
                                break;
                            case "path-condition-edges":
                                config.TEMP_PATH_CONDITION_EDGES = nodeText;
                                break;
                            case "path-condition-truths":
                                config.TEMP_PATH_CONDITION_TRUTHS = nodeText;
                                break;
                            case "path-array-accesses":
                                config.TEMP_PATH_ARRAY_ACCESSES = nodeText;
                                break;
                            case "path-agg-index-exprs":
                                config.TEMP_PATH_AGG_INDEX_EXPRS = nodeText;
                                break;
                            case "path-predicted-value":
                                config.TEMP_PATH_PREDICTED_VALUE = nodeText;
                                break;
                            case "path-measured-value":
                                config.TEMP_PATH_MEASURED_VALUE = nodeText;
                                break;
                            case "path-all":
                                config.TEMP_PATH_ALL = nodeText;
                                break;

                            case "path-query":
                                config.TEMP_PATH_QUERY = nodeText;
                                break;
                            case "smt-model":
                                config.TEMP_SMT_MODEL = nodeText;
                                break;
                            case "path-query-all":
                                config.TEMP_PATH_QUERY_ALL = nodeText;
                                break;

                            case "case":
                                config.TEMP_CASE = nodeText;
                                break;

                            case "basis-matrix":
                                config.TEMP_BASIS_MATRIX = nodeText;
                                break;
                            case "measurement":
                                config.TEMP_MEASUREMENT = nodeText;
                                break;
                            case "basis-values":
                                config.TEMP_BASIS_VALUES = nodeText;
                                break;
                            case "dag-weights":
                                config.TEMP_DAG_WEIGHTS = nodeText;
                                break;
                            case "distribution":
                                config.TEMP_DISTRIBUTION = nodeText;
                                break;

                            default:
                                Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", nodeName);
                                Environment.Exit(2);
                                return null;
                        }
                    }
                }

                /* Process the locations of useful tools. */
                XmlNodeList tools = configDocElt.GetElementsByTagName("tools");
                XmlNode toolsNode = tools[0];

                foreach (XmlNode node in toolsNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        string location = Path.Combine(config.configDir, nodeText);
                        if (!File.Exists(location) && !Directory.Exists(location))
                        {
                            Console.Out.WriteLine("PHOENIX: Invalid location: {0}", location);
                            Environment.Exit(3);
                            return null;
                        }
                        if (node.Name == "phoenix") { config.TOOL_PHOENIX = location; }
                        else if (node.Name == "cil") { config.TOOL_CIL = location; }
                        else
                        {
                            Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", node.Name);
                            Environment.Exit(2);
                            return null;
                        }
                    }
                }

                /* Process the locations of SMT solvers. */
                XmlNodeList solvers = configDocElt.GetElementsByTagName("smt-solvers");
                XmlNode solversNode = solvers[0];

                foreach (XmlNode node in solversNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        string location = Path.Combine(config.configDir, nodeText);
                        if (node.Name == "boolector") { config.SOLVER_BOOLECTOR = location; }
                        else if (node.Name == "z3") { config.SOLVER_Z3 = location; }
                        else
                        {
                            Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", node.Name);
                            Environment.Exit(2);
                            return null;
                        }
                    }
                }

                /* Process the locations of simulators and useful auxiliary tools. */
                XmlNodeList simulators = configDocElt.GetElementsByTagName("simulators");
                XmlNode simulatorsNode = simulators[0];

                foreach (XmlNode node in simulatorsNode.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        string nodeText = Configuration.GetXmlText(node);
                        string nodeName = node.Name;

                        string location = Path.Combine(config.configDir, nodeText);
                        if (node.Name == "gnu-arm")
                        {
                            config.SIMULATOR_TOOL_GNU_ARM = location;
                        }
                        else if (node.Name == "ptarm")
                        {
                            config.SIMULATOR_PTARM = location;
                        }
                        else
                        {
                            Console.Out.WriteLine("PHOENIX: Unrecognized tag: {0}", node.Name);
                            Environment.Exit(2);
                            return null;
                        }
                    }
                }

                configXmlReader.Close();
                return config;
            }
        }

        #endregion
    }
}
